namespace SambaFileManager.Interfaces;
public interface ISambaFileService
{
  byte[] ReadFile(string filePath);
  string ReadStringFile(string filePath);
  void WriteFile(string filePath, string content);
  void WriteFile(string filePath, byte[] content);
  void DeleteFile(string filePath);
}
