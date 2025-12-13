namespace GuiBuddy.Core.Services;

public interface ISettingsService
{
    string? GetApiKey(string provider);
    void SaveApiKey(string provider, string key);
}
