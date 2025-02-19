namespace SambaFileManager.Models;

public class SambaSettingsBuilder
{
  private SambaSettings? _sambaSettings;
  public SambaSettingsBuilder SetServer(string server)
  {
    InitSettings();
    _sambaSettings!.Server = server;

    return this;
  }
  public SambaSettingsBuilder SetShare(string share)
  {
    InitSettings();
    _sambaSettings!.Share = share;

    return this;
  }
  public SambaSettingsBuilder SetUsername(string username)
  {
    InitSettings();
    _sambaSettings!.Username = username;

    return this;
  }
  public SambaSettingsBuilder SetPassword(string password)
  {
    InitSettings();
    _sambaSettings!.Password = password;

    return this;
  }
  public SambaSettingsBuilder SetDomain(string domain)
  {
    InitSettings();
    _sambaSettings!.Domain = domain;

    return this;
  }

  public SambaSettings Build()
  {
    return _sambaSettings!;
  }

  private void InitSettings()
  {
    if (_sambaSettings == null)
    {
      _sambaSettings = new SambaSettings();
    }
  }
}
