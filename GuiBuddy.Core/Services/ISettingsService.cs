namespace GuiBuddy.Core.Services;

public interface ISettingsService
{
    // APIキー管理
    string? GetApiKey(string provider);
    void SaveApiKey(string provider, string key);

    // プロバイダー/モデル選択
    string GetSelectedProvider();
    void SetSelectedProvider(string provider);
    string GetSelectedModel();
    void SetSelectedModel(string model);
}

