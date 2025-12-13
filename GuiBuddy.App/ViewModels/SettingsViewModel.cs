using System;
using System.Reactive.Linq;
using GuiBuddy.Core.Services;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace GuiBuddy.App.ViewModels;

public class SettingsViewModel : IDisposable
{
    private readonly ISettingsService _settingsService;
    private const string ProviderName = "Gemini";

    public ReactiveProperty<string> ApiKey { get; }
    public ReactiveCommand SaveCommand { get; }
    public ReactiveCommand CloseCommand { get; } = new();

    public event Action? RequestClose;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // Load existing key
        string currentKey = _settingsService.GetApiKey(ProviderName) ?? "";
        ApiKey = new ReactiveProperty<string>(currentKey);

        SaveCommand = ApiKey
            .Select(k => !string.IsNullOrEmpty(k)) // Simple validation
            .ToReactiveCommand();

        SaveCommand.Subscribe(_ => Save());
        CloseCommand.Subscribe(_ => RequestClose?.Invoke());
    }

    private void Save()
    {
        _settingsService.SaveApiKey(ProviderName, ApiKey.Value);
        RequestClose?.Invoke();
    }

    public void Dispose()
    {
        ApiKey.Dispose();
        SaveCommand.Dispose();
        CloseCommand.Dispose();
    }
}
