using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private const string SettingsFileName = "settings.json";
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        // AppData/Local/GuiBuddy に保存
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appDir = Path.Combine(localAppData, "GuiBuddy");
        Directory.CreateDirectory(appDir);
        _settingsFilePath = Path.Combine(appDir, SettingsFileName);
    }

    public void SaveApiKey(string provider, string key)
    {
        var settings = LoadSettings();
        
        if (string.IsNullOrEmpty(key))
        {
            if (settings.ApiKeys.ContainsKey(provider))
            {
                settings.ApiKeys.Remove(provider);
            }
        }
        else
        {
            // Windows DPAPIで暗号化 (CurrentUserスコープ)
            byte[] entropy = Encoding.UTF8.GetBytes("GuiBuddy-Salt"); // 追加のエントロピー
            byte[] encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(key),
                entropy,
                DataProtectionScope.CurrentUser);

            settings.ApiKeys[provider] = Convert.ToBase64String(encryptedData);
        }

        SaveSettings(settings);
    }

    public string? GetApiKey(string provider)
    {
        var settings = LoadSettings();
        if (settings.ApiKeys.TryGetValue(provider, out var encryptedKey))
        {
            try
            {
                byte[] entropy = Encoding.UTF8.GetBytes("GuiBuddy-Salt");
                byte[] encryptedData = Convert.FromBase64String(encryptedKey);
                byte[] decryptedData = ProtectedData.Unprotect(
                    encryptedData,
                    entropy,
                    DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(decryptedData);
            }
            catch
            {
                // 復号化失敗時はnullを返す (データ破損や他ユーザーコンテキストなど)
                return null;
            }
        }
        return null;
    }

    private AppSettings LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
        return new AppSettings();
    }

    private void SaveSettings(AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }

    // 内部設定モデル
    private class AppSettings
    {
        public System.Collections.Generic.Dictionary<string, string> ApiKeys { get; set; } = new();
    }
}
