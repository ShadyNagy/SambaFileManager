namespace SambaFileManager.Models;

public class SambaSettings
{
  public string Server { get; set; } = string.Empty;
  public string Share { get; set; } = string.Empty;
  public string Username { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string Domain { get; set; } = string.Empty;
}
