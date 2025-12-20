using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace GuiBuddy.App.ViewModels;

public class SettingsViewModel : IDisposable
{
    private readonly ISettingsService _settingsService;

    // プロバイダー一覧
    public IReadOnlyList<string> AvailableProviders { get; }
    
    // 選択中のプロバイダー
    public ReactiveProperty<string> SelectedProvider { get; }
    
    // 現在のプロバイダーで利用可能なモデル一覧
    public ReactiveProperty<IReadOnlyList<string>> AvailableModels { get; }
    
    // 選択中のモデル
    public ReactiveProperty<string> SelectedModel { get; }
    
    // APIキー
    public ReactiveProperty<string> ApiKey { get; }
    
    // コマンド
    public ReactiveCommand SaveCommand { get; }
    public ReactiveCommand CloseCommand { get; } = new();

    public event Action? RequestClose;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // プロバイダー一覧の初期化
        AvailableProviders = AIProviderInfo.AvailableProviders.Select(p => p.Name).ToList();

        // 現在の設定を読み込み
        string currentProvider = _settingsService.GetSelectedProvider();
        string currentModel = _settingsService.GetSelectedModel();

        // プロバイダー選択
        SelectedProvider = new ReactiveProperty<string>(currentProvider);

        // モデル一覧（プロバイダーに応じて更新）
        AvailableModels = SelectedProvider
            .Select(provider => GetModelsForProvider(provider))
            .ToReactiveProperty(GetModelsForProvider(currentProvider));

        // モデル選択
        SelectedModel = new ReactiveProperty<string>(currentModel);

        // プロバイダー変更時にモデルをリセット（最初のモデルを選択）
        SelectedProvider
            .Skip(1) // 初期値はスキップ
            .Subscribe(provider =>
            {
                var models = GetModelsForProvider(provider);
                if (models.Count > 0 && !models.Contains(SelectedModel.Value))
                {
                    SelectedModel.Value = models[0];
                }
            });

        // APIキー（選択中プロバイダー用）
        ApiKey = new ReactiveProperty<string>(_settingsService.GetApiKey(currentProvider) ?? "");

        // プロバイダー変更時にAPIキーを切り替え
        SelectedProvider
            .Skip(1)
            .Subscribe(provider =>
            {
                ApiKey.Value = _settingsService.GetApiKey(provider) ?? "";
            });

        // 保存コマンド
        SaveCommand = new ReactiveCommand();
        SaveCommand.Subscribe(_ => Save());
        CloseCommand.Subscribe(_ => RequestClose?.Invoke());
    }

    private IReadOnlyList<string> GetModelsForProvider(string provider)
    {
        var providerInfo = AIProviderInfo.AvailableProviders.FirstOrDefault(p => p.Name == provider);
        return providerInfo?.Models ?? Array.Empty<string>();
    }

    private void Save()
    {
        // プロバイダーとモデルを保存
        _settingsService.SetSelectedProvider(SelectedProvider.Value);
        _settingsService.SetSelectedModel(SelectedModel.Value);
        
        // APIキーを保存（現在選択中のプロバイダー用）
        _settingsService.SaveApiKey(SelectedProvider.Value, ApiKey.Value);
        
        RequestClose?.Invoke();
    }

    public void Dispose()
    {
        SelectedProvider.Dispose();
        AvailableModels.Dispose();
        SelectedModel.Dispose();
        ApiKey.Dispose();
        SaveCommand.Dispose();
        CloseCommand.Dispose();
    }
}
