namespace SambaFileManager.Interfaces;
/// <summary>
/// Provides methods for interacting with files and directories on an SMB share.
/// </summary>
public interface ISambaFileService
{
  /// <summary>
  /// Reads a file from the SMB share and returns its content as a byte array.
  /// </summary>
  /// <param name="filePath">The full path of the file to read.</param>
  /// <returns>The file content as a byte array.</returns>
  byte[] ReadFile(string filePath);

  /// <summary>
  /// Reads a file from the SMB share and returns its content as a UTF-8 encoded string.
  /// </summary>
  /// <param name="filePath">The full path of the file to read.</param>
  /// <returns>The file content as a string.</returns>
  string ReadStringFile(string filePath);

  /// <summary>
  /// Writes a string to a file on the SMB share, creating or overwriting the file.
  /// </summary>
  /// <param name="filePath">The full path of the file to write.</param>
  /// <param name="content">The content to write to the file.</param>
  void WriteFile(string filePath, string content);

  /// <summary>
  /// Writes a byte array to a file on the SMB share, creating or overwriting the file.
  /// </summary>
  /// <param name="filePath">The full path of the file to write.</param>
  /// <param name="content">The byte array content to write to the file.</param>
  void WriteFile(string filePath, byte[] content);

  /// <summary>
  /// Deletes a file from the SMB share.
  /// </summary>
  /// <param name="filePath">The full path of the file to delete.</param>
  void DeleteFile(string filePath);

  /// <summary>
  /// Renames a file on the SMB share if the new name does not already exist.
  /// </summary>
  /// <param name="oldPath">The current full path of the file.</param>
  /// <param name="newPath">The new full path of the file.</param>
  void RenameFile(string oldPath, string newPath);

  /// <summary>
  /// Creates a directory and all necessary parent directories on the SMB share.
  /// </summary>
  /// <param name="filePath">The full path of the directory to create.</param>
  void CreateDirectoriesRecursively(string filePath);

  /// <summary>
  /// Deletes an empty folder from the SMB share.
  /// </summary>
  /// <param name="folderPath">The full path of the folder to delete.</param>
  void DeleteFolder(string folderPath);

  /// <summary>
  /// Deletes a folder and all its subdirectories and files from the SMB share.
  /// </summary>
  /// <param name="folderPath">The full path of the folder to delete.</param>
  void DeleteFolderRecursive(string folderPath);

  /// <summary>
  /// Renames a folder on the SMB share if the new name does not already exist.
  /// </summary>
  /// <param name="oldPath">The current full path of the folder.</param>
  /// <param name="newPath">The new full path of the folder.</param>
  void RenameFolder(string oldPath, string newPath);
}
