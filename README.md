[![publish to nuget](https://github.com/ShadyNagy/SambaFileManager/actions/workflows/nugt-publish.yml/badge.svg)](https://github.com/ShadyNagy/SambaFileManager/actions/workflows/nugt-publish.yml)
[![SambaFileManager on NuGet](https://img.shields.io/nuget/v/SambaFileManager?label=SambaFileManager)](https://www.nuget.org/packages/SambaFileManager/)
[![NuGet](https://img.shields.io/nuget/dt/SambaFileManager)](https://www.nuget.org/packages/SambaFileManager)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/ShadyNagy/SambaFileManager/blob/main/LICENSE)
[![Sponsor](https://img.shields.io/badge/Sponsor-ShadyNagy-brightgreen?logo=github-sponsors)](https://github.com/sponsors/ShadyNagy)

# 📁 SambaFileManager

🔹 **A .NET 8 library for managing files on a Samba (SMB) server with Dependency Injection support.**

---

## 📌 Introduction
SambaFileManager is a lightweight .NET 8 library that simplifies **reading, writing, and deleting files** on Samba (SMB) servers. It provides an **easy-to-use API** that integrates seamlessly with **Dependency Injection (DI)**.

### 📌 Key Features:  
✔️ Supports **reading, writing, and deleting** files over an SMB network.  
✔️ **Integrates with .NET Dependency Injection** for easy use in ASP.NET and Console Apps.  
✔️ Provides **robust error handling** and exception management.  
✔️ Uses **SMBLibrary** under the hood for stable and secure SMB communication.  

---

## 📥 Installation
Install the package via NuGet:
```sh
dotnet add package SambaFileManager
```

Or manually add it to your `.csproj`:
```xml
<ItemGroup>
    <PackageReference Include="SambaFileManager" Version="1.0.0" />
</ItemGroup>
```

---

## 🚀 Quick Start: Using in a Console Application
### 1️⃣ Setup Dependency Injection
```csharp
using Microsoft.Extensions.DependencyInjection;
using System;
using SambaFileManager.Interfaces;
using SambaFileManager.Models;
using SambaFileManager.Extensions;

class Program
{
    static void Main()
    {
        // Setup DI container
         var sambaSettings = new SambaSettingsBuilder()
            .SetServer("192.168.1.100")
            .SetShare("storage")
            .SetUsername("smbuser")
            .SetPassword("smbpassword")
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddSambaFileManagerServices(sambaSettings)
            .BuildServiceProvider();

        // Resolve the Samba file service
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
            string content = sambaFileService.ReadFile(filePath);
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
```

### 2️⃣ Expected Output
```sh
Writing file...
File written successfully.
Reading file...
File content: Hello, Samba File System!
Deleting file...
File deleted successfully.
```

---

## 🔧 Using in an ASP.NET Core Application
### 1️⃣ Register the Service in `Program.cs`
```csharp
var sambaSettings = builder.Configuration.GetSection("Samba").Get<SambaSettings>();
builder.Services.AddSambaFileManagerServices(sambaSettings);
```

### 2️⃣ Inject and Use in a Controller
```csharp
[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly ISambaFileService _sambaFileService;

    public FileController(ISambaFileService sambaFileService)
    {
        _sambaFileService = sambaFileService;
    }

    [HttpGet("read")]
    public IActionResult ReadFile(string filePath)
    {
        var content = _sambaFileService.ReadFile(filePath);
        return Ok(content);
    }
}
```

---

## 🛠 Configuration
You can configure your SMB settings in **`appsettings.json`**:
```json
{
  "Samba": {
    "Server": "192.168.1.100",
    "Share": "SharedFolder",
    "Username": "smbuser",
    "Password": "smbpassword",
    "Domain": ""
  }
}
```

---

## 🔗 License
This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

---

## 🙌 Contributing
🎯 Found a bug or have an idea for improvement?  
Feel free to **open an issue** or **submit a pull request**!  
🔗 [GitHub Issues](https://github.com/ShadyNagy/SambaFileManager/issues)

---

## ⭐ Support the Project
If you find this package useful, **give it a star ⭐ on GitHub** and **share it with others!** 🚀
