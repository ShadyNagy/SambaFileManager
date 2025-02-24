using System;
using System.IO;
using System.Linq;
using System.Text;
using SambaFileManager.Interfaces;
using SambaFileManager.Models;
using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace SambaFileManager.Services;

public class SambaFileService : ISambaFileService, IDisposable
{
  private readonly string _server;
  private readonly string _share;
  private readonly string _username;
  private readonly string _password;
  private readonly string? _domain;
  private SMB2Client? _client;
  private ISMBFileStore? _tree;

  public SambaFileService(SambaSettings sambaSettings)
  {
    if (sambaSettings == null)
      throw new ArgumentNullException(nameof(sambaSettings));

    _server = sambaSettings.Server;
    _share = sambaSettings.Share;
    _username = sambaSettings.Username;
    _password = sambaSettings.Password;
    _domain = sambaSettings.Domain;

    _client = new SMB2Client();
  }

  private void Connect()
  {
    if (_client!.IsConnected)
      return;

    if (!_client.Connect(_server, SMBTransportType.DirectTCPTransport))
      throw new IOException("Failed to connect to SMB server.");

    var status = _client.Login(_domain, _username, _password);
    if (status != NTStatus.STATUS_SUCCESS)
      throw new IOException($"Login failed: {status}");

    _tree = _client.TreeConnect(_share, out status);

    if (status != NTStatus.STATUS_SUCCESS || _tree == null)
      throw new IOException($"Failed to connect to share: {status}");
  }

  public byte[] ReadFile(string filePath)
  {
    Connect();
    try
    {
      if (_tree == null)
        throw new InvalidOperationException("SMB connection is not initialized.");

      var status = _tree.CreateFile(
        out var fileHandle,
        out _,
        filePath,
        AccessMask.GENERIC_READ,
        FileAttributes.Normal,
        ShareAccess.Read,
        CreateDisposition.FILE_OPEN,
        CreateOptions.FILE_NON_DIRECTORY_FILE,
        null);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to open file: {status}");

      status = _tree.GetFileInformation(out var fileInfo, fileHandle, FileInformationClass.FileStandardInformation);
      if (status != NTStatus.STATUS_SUCCESS)
      {
        _tree.CloseFile(fileHandle);
        throw new IOException($"Failed to retrieve file information: {status}");
      }

      byte[] data = new byte[fileInfo.Length];

      status = _tree.ReadFile(out data, fileHandle, 0, data.Length);
      _tree.CloseFile(fileHandle);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to read file: {status}");

      return data;
    }
    finally
    {
      Disconnect();
    }
  }

  public string ReadStringFile(string filePath)
  {
    var data = ReadFile(filePath);
    return Encoding.UTF8.GetString(data);
  }


  public void WriteFile(string filePath, byte[] content)
  {
    Connect();
    try
    {
      if (_tree == null)
        throw new InvalidOperationException("SMB connection is not initialized.");

      EnsureDirectoriesExist(filePath);

      var status = _tree.CreateFile(
        out var fileHandle,
        out _,
        filePath,
        AccessMask.GENERIC_WRITE,
        FileAttributes.Normal,
        ShareAccess.None,
        CreateDisposition.FILE_OVERWRITE_IF,
        CreateOptions.FILE_NON_DIRECTORY_FILE,
        null);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to create file: {status}");

      const int CHUNK_SIZE = 64 * 1024;
      long offset = 0;
      int bytesWritten = 0;

      while (offset < content.Length)
      {
        int lengthToWrite = (int)Math.Min(CHUNK_SIZE, content.Length - offset);
        byte[] buffer = new byte[lengthToWrite];
        Array.Copy(content, offset, buffer, 0, lengthToWrite);

        status = this._tree.WriteFile(out int numberOfBytesWritten, fileHandle, offset, buffer);
        if (status != NTStatus.STATUS_SUCCESS)
        {
          throw new IOException($"Failed to write chunk at offset {offset}, NTStatus={status}");
        }

        offset += numberOfBytesWritten;
        bytesWritten += numberOfBytesWritten;
      }

      if (bytesWritten != content.Length)
      {
        throw new IOException("Not all bytes were written!");
      }

      _tree.CloseFile(fileHandle);
    }
    finally
    {
      Disconnect();
    }
  }

  public void WriteFile(string filePath, string content)
  {
    byte[] data = Encoding.UTF8.GetBytes(content);
    WriteFile(filePath, data);
  }

  public void DeleteFile(string filePath)
  {
    Connect();
    try
    {
      if (_tree == null)
        throw new InvalidOperationException("SMB connection is not initialized.");

      var status = _tree.CreateFile(
          out var fileHandle,
          out _,
          filePath,
          AccessMask.DELETE,
          FileAttributes.Normal,
          ShareAccess.Read,
          CreateDisposition.FILE_OPEN,
          CreateOptions.FILE_NON_DIRECTORY_FILE,
          null);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to open file: {status}");

      FileDispositionInformation dispositionInfo = new() { DeletePending = true };
      status = _tree.SetFileInformation(fileHandle, dispositionInfo);
      _tree.CloseFile(fileHandle);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to delete file: {status}");
    }
    finally
    {
      Disconnect();
    }
  }

  public void CreateDirectoriesRecursively(string filePath)
  {
    EnsureDirectoriesExist(filePath);
  }

  public void DeleteFolder(string folderPath)
  {
    Connect();
    try
    {
      if (_tree == null)
        throw new InvalidOperationException("SMB connection is not initialized.");

      var status = _tree.CreateFile(
          out var dirHandle,
          out _,
          folderPath,
          AccessMask.DELETE,
          FileAttributes.Directory,
          ShareAccess.Read,
          CreateDisposition.FILE_OPEN,
          CreateOptions.FILE_DIRECTORY_FILE,
          null);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to open directory: {status}");

      FileDispositionInformation dispositionInfo = new() { DeletePending = true };
      status = _tree.SetFileInformation(dirHandle, dispositionInfo);
      _tree.CloseFile(dirHandle);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to delete directory: {status}");
    }
    finally
    {
      Disconnect();
    }
  }

  public void DeleteFolderRecursive(string folderPath)
  {
    Connect();
    try
    {
      DeleteFolderRecursiveInternal(folderPath);
    }
    finally
    {
      Disconnect();
    }
  }

  public void RenameFile(string oldPath, string newPath)
  {
    Connect();
    try
    {
      if (_tree == null)
        throw new InvalidOperationException("SMB connection is not initialized.");

      var status = _tree.CreateFile(
          out var fileHandle,
          out _,
          oldPath,
          AccessMask.GENERIC_READ | AccessMask.GENERIC_WRITE | AccessMask.DELETE,
          FileAttributes.Normal,
          ShareAccess.Read,
          CreateDisposition.FILE_OPEN,
          CreateOptions.FILE_NON_DIRECTORY_FILE,
          null);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to open file {oldPath}: {status}");

      var renameInfo = new FileRenameInformationType2
      {
        ReplaceIfExists = false,
        FileName = newPath
      };

      status = _tree.SetFileInformation(fileHandle, renameInfo);
      _tree.CloseFile(fileHandle);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to rename file from {oldPath} to {newPath}: {status}");
    }
    finally
    {
      Disconnect();
    }
  }

  public void RenameFolder(string oldPath, string newPath)
  {
    Connect();
    try
    {
      if (_tree == null)
        throw new InvalidOperationException("SMB connection is not initialized.");

      var status = _tree.CreateFile(
          out var dirHandle,
          out _,
          oldPath,
          AccessMask.GENERIC_READ | AccessMask.GENERIC_WRITE | AccessMask.DELETE,
          FileAttributes.Directory,
          ShareAccess.Read,
          CreateDisposition.FILE_OPEN,
          CreateOptions.FILE_DIRECTORY_FILE,
          null);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to open directory {oldPath}: {status}");

      var renameInfo = new FileRenameInformationType2
      {
        ReplaceIfExists = false,
        FileName = newPath
      };

      status = _tree.SetFileInformation(dirHandle, renameInfo);
      _tree.CloseFile(dirHandle);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to rename directory from {oldPath} to {newPath}: {status}");
    }
    finally
    {
      Disconnect();
    }
  }

  public bool FileExists(string filePath)
  {
    Connect();
    try
    {
      if (_tree == null)
        throw new InvalidOperationException("SMB connection is not initialized.");

      var status = _tree.CreateFile(
        out var fileHandle,
        out var fileStatus,
        filePath,
        AccessMask.GENERIC_READ, 
        FileAttributes.Normal,
        ShareAccess.Read,
        CreateDisposition.FILE_OPEN,    
        CreateOptions.FILE_NON_DIRECTORY_FILE,
        null);

      if (status == NTStatus.STATUS_SUCCESS)
      {
        _tree.CloseFile(fileHandle);
        return true;
      }
      else if (status == NTStatus.STATUS_OBJECT_NAME_NOT_FOUND ||
               status == NTStatus.STATUS_NO_SUCH_FILE ||
               status == NTStatus.STATUS_OBJECT_PATH_NOT_FOUND)
      {
        // The file does not exist
        return false;
      }
      else
      {
        // Some other error occurred (e.g. permission issues, server error, etc.)
        throw new IOException($"Error checking file existence: {status}");
      }
    }
    finally
    {
      Disconnect();
    }
  }

  private void DeleteFolderRecursiveInternal(string folderPath)
  {
    if (_tree == null)
      throw new InvalidOperationException("SMB connection is not initialized.");

    var status = _tree.CreateFile(
        out var dirHandle,
        out _,
        folderPath,
        AccessMask.GENERIC_READ | AccessMask.DELETE,
        FileAttributes.Directory,
        ShareAccess.Read,
        CreateDisposition.FILE_OPEN,
        CreateOptions.FILE_DIRECTORY_FILE,
        null);

    if (status != NTStatus.STATUS_SUCCESS)
      throw new IOException($"Failed to open directory: {status}");

    status = _tree.QueryDirectory(out var files, dirHandle, "*", FileInformationClass.FileDirectoryInformation);
    if (status != NTStatus.STATUS_SUCCESS)
    {
      _tree.CloseFile(dirHandle);
      throw new IOException($"Failed to list directory contents: {status}");
    }

    foreach (var fileInfo in files.Cast<FileDirectoryInformation>())
    {
      string name = fileInfo.FileName;
      if (name == "." || name == "..")
        continue;

      string fullPath = Path.Combine(folderPath, name);

      if ((fileInfo.FileAttributes & FileAttributes.Directory) != 0)
      {
        DeleteFolderRecursiveInternal(fullPath);
      }
      else
      {
        status = _tree.CreateFile(
            out var fileHandle,
            out _,
            fullPath,
            AccessMask.DELETE,
            FileAttributes.Normal,
            ShareAccess.Read,
            CreateDisposition.FILE_OPEN,
            CreateOptions.FILE_NON_DIRECTORY_FILE,
            null);

        if (status != NTStatus.STATUS_SUCCESS)
          throw new IOException($"Failed to open file {fullPath}: {status}");

        FileDispositionInformation disposition = new() { DeletePending = true };
        status = _tree.SetFileInformation(fileHandle, disposition);
        _tree.CloseFile(fileHandle);

        if (status != NTStatus.STATUS_SUCCESS)
          throw new IOException($"Failed to delete file {fullPath}: {status}");
      }
    }

    _tree.CloseFile(dirHandle);

    status = _tree.CreateFile(
        out var dirHandleDelete,
        out _,
        folderPath,
        AccessMask.DELETE,
        FileAttributes.Directory,
        ShareAccess.Read,
        CreateDisposition.FILE_OPEN,
        CreateOptions.FILE_DIRECTORY_FILE,
        null);

    if (status != NTStatus.STATUS_SUCCESS)
      throw new IOException($"Failed to open directory for deletion: {status}");

    FileDispositionInformation dirDisposition = new() { DeletePending = true };
    status = _tree.SetFileInformation(dirHandleDelete, dirDisposition);
    _tree.CloseFile(dirHandleDelete);

    if (status != NTStatus.STATUS_SUCCESS)
      throw new IOException($"Failed to delete directory: {status}");
  }

  private void EnsureDirectoriesExist(string filePath)
  {
    if (string.IsNullOrWhiteSpace(filePath))
      throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

    string[] pathSegments = filePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
    if (pathSegments.Length == 0)
      return;

    var currentPath = new StringBuilder();

    for (int i = 0; i < pathSegments.Length - 1; i++)
    {
      if (currentPath.Length > 0)
        currentPath.Append('/');

      currentPath.Append(pathSegments[i]);

      CreateDirectory(currentPath.ToString());
    }
  }

  private void CreateDirectory(string directoryPath)
  {
    if (_tree == null)
      throw new InvalidOperationException("SMB connection is not initialized.");

    var status = _tree.CreateFile(
        out var dirHandle,
        out var fileStatus,
        directoryPath,
        AccessMask.GENERIC_ALL,
        FileAttributes.Directory,
        ShareAccess.Read,
        CreateDisposition.FILE_OPEN_IF,
        CreateOptions.FILE_DIRECTORY_FILE,
        null);

    if (status != NTStatus.STATUS_SUCCESS)
      throw new IOException($"Failed to create directory: {status}");

    _tree.CloseFile(dirHandle);
  }

  private void Disconnect()
  {
    try
    {
      _tree?.Disconnect();
    }
    catch(Exception exception)
    {
      throw new IOException($"Disconnect Error: {exception.Message}");
    }

    try
    {
      _client?.Disconnect();
    }
    catch (Exception exception)
    {
      throw new IOException($"Disconnect Error: {exception.Message}");
    }
  }

  public void Dispose()
  {
    Disconnect();
    _tree = null;
    _client = null;
  }
}


