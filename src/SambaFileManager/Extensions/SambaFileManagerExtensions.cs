using Microsoft.Extensions.DependencyInjection;
using SambaFileManager.Interfaces;
using SambaFileManager.Models;
using SambaFileManager.Services;

namespace SambaFileManager.Extensions;

public static class SambaFileManagerExtensions
{
  public static IServiceCollection AddSambaFileManagerServices(this IServiceCollection services, SambaSettings sambaSettings)
  {
    return services
      .AddSingleton(sambaSettings)
      .AddScoped<ISambaFileService, SambaFileService>();
  }

  public static IServiceCollection AddSambaFileManagerSingletonServices(this IServiceCollection services, SambaSettings sambaSettings)
  {
    return services
      .AddSingleton(sambaSettings)
      .AddSingleton<ISambaFileService, SambaFileService>();
  }
}
