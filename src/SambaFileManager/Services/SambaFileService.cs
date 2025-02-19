using System;
using System.IO;
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

    _server = sambaSettings.Server ?? throw new ArgumentNullException(nameof(sambaSettings.Server));
    _share = sambaSettings.Share ?? throw new ArgumentNullException(nameof(sambaSettings.Share));
    _username = sambaSettings.Username ?? throw new ArgumentNullException(nameof(sambaSettings.Username));
    _password = sambaSettings.Password ?? throw new ArgumentNullException(nameof(sambaSettings.Password));
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
        out var fileStatus,
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
        out var fileStatus,
        filePath,
        AccessMask.GENERIC_WRITE,
        FileAttributes.Normal,
        ShareAccess.None,
        CreateDisposition.FILE_OVERWRITE_IF,
        CreateOptions.FILE_NON_DIRECTORY_FILE,
        null);

      if (status != NTStatus.STATUS_SUCCESS)
        throw new IOException($"Failed to create file: {status}");

      status = _tree.WriteFile(out var bytesWritten, fileHandle, 0, content);
      _tree.CloseFile(fileHandle);

      if (status != NTStatus.STATUS_SUCCESS || bytesWritten != content.Length)
        throw new IOException($"Failed to write file: {status}");
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
          out var fileStatus,
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

  private void EnsureDirectoriesExist(string filePath)
  {
    string[] pathSegments = filePath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
    string currentPath = string.Empty;

    for (int i = 0; i < pathSegments.Length - 1; i++)
    {
      currentPath += (currentPath == string.Empty ? "" : "/") + pathSegments[i];
      CreateDirectory(currentPath);
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


