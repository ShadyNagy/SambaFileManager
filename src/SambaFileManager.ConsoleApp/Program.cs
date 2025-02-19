using Microsoft.Extensions.DependencyInjection;
using System;
using SambaFileManager.Interfaces;
using SambaFileManager.Models;
using SambaFileManager.Extensions;

class Program
{
  static void Main()
  {
    // Setup Dependency Injection
    var sambaSettings = new SambaSettingsBuilder()
      .SetServer("192.168.1.5")
      .SetShare("storage")
      .SetUsername("smbuser")
      .SetPassword("password")
      .Build();

    var serviceProvider = new ServiceCollection()
      .AddSambaFileManagerServices(sambaSettings)
      .BuildServiceProvider();

    // Resolve the service
    var sambaFileService = serviceProvider.GetRequiredService<ISambaFileService>();

    // Define file path
    string filePath = "test.txt";
    string fileContent = "Hello, Samba File System!";

    try
    {
      // Write a file
      Console.WriteLine("Writing file...");
      sambaFileService.WriteFile(filePath, fileContent);
      Console.WriteLine("File written successfully.");

      // Read the file
      Console.WriteLine("Reading file...");
      string content = sambaFileService.ReadStringFile(filePath);
      Console.WriteLine($"File content: {content}");

      // Delete the file
      Console.WriteLine("Deleting file...");
      sambaFileService.DeleteFile(filePath);
      Console.WriteLine("File deleted successfully.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
    }
  }
}
