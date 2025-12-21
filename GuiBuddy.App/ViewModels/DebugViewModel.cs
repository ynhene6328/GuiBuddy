using System.Collections.ObjectModel;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.ComponentModel;

namespace GuiBuddy.App.ViewModels;

public class DebugViewModel
{
    public ReactiveProperty<string> StatusMessage { get; } = new("Ready");
    public ObservableCollection<WindowInfo> Windows { get; } = new();
    public ReactiveCommand RefreshCommand { get; }
    public ReactiveProperty<WindowInfo?> SelectedWindow { get; } = new();
    public ObservableCollection<UIElementInfo> Structure { get; } = new();
    public ReactiveCommand GetStructureCommand { get; }
    public ReactiveProperty<string> UIMapJson { get; } = new("");
    public ReactiveProperty<string> AiMapJson { get; } = new("");
    public ReactiveCommand GenerateMapCommand { get; }
    public ReactiveCommand ShowOverlayCommand { get; }
    public ReactiveCommand CloseOverlayCommand { get; }
    
    // Debug Scroll
    public ReactiveProperty<UIElementInfo?> SelectedNode { get; } = new();
    public ReactiveCommand DebugScrollCommand { get; }

    // Debug View Toggle
    public ReactiveProperty<bool> IsDebugVisible { get; } = new(false);
    public ReactiveCommand ToggleDebugCommand { get; }

    private readonly IWindowService _windowService;
    private readonly IUIMapService _uiMapService;
    private readonly IOverlayService _overlayService;
    private readonly ISettingsService _settingsService;
    private UIMap? _currentMap;

    public ChatViewModel ChatViewModel { get; }

    public DebugViewModel(IWindowService windowService, IUIMapService uiMapService, IOverlayService overlayService, ISettingsService settingsService, ChatViewModel chatViewModel)
    {
        _windowService = windowService;
        _uiMapService = uiMapService;
        _overlayService = overlayService;
        _settingsService = settingsService;
        ChatViewModel = chatViewModel;

        RefreshCommand = new ReactiveCommand();
        RefreshCommand.Subscribe(_ => RefreshWindows());

        GetStructureCommand = SelectedWindow.Select(w => w != null).ToReactiveCommand();
        GetStructureCommand.Subscribe(_ => GetStructure());

        GenerateMapCommand = SelectedWindow.Select(w => w != null).ToReactiveCommand();
        GenerateMapCommand.Subscribe(_ => GenerateMap());

        // ユーザーの要望: ウィンドウ選択時に自動的にマップ生成を行う
        SelectedWindow.Where(w => w != null).Subscribe(_ => GenerateMap());

        ShowOverlayCommand = GenerateMapCommand.Select(_ => true).ToReactiveCommand(); // Simplified check for now, ideally tied to _currentMap availability
        ShowOverlayCommand.Subscribe(_ => ShowOverlay());

        CloseOverlayCommand = new ReactiveCommand();
        CloseOverlayCommand.Subscribe(_ => CloseOverlay());

        ToggleDebugCommand = new ReactiveCommand();
        ToggleDebugCommand.Subscribe(_ => IsDebugVisible.Value = !IsDebugVisible.Value);

        DebugScrollCommand = SelectedNode
            .Select(n => n != null)
            .ToReactiveCommand();
        DebugScrollCommand.Subscribe(_ => DebugScroll());

        // ChatViewModelからのウィンドウ確定通知を受け取る
        ChatViewModel.WindowConfirmed += OnChatWindowConfirmed;
        ChatViewModel.OpenSettingsRequested += ShowSettings;
    }

    private void DebugScroll()
    {
        if (SelectedWindow.Value == null || SelectedNode.Value == null) return;

        UIElementInfo? targetNode = SelectedNode.Value;
        string? targetKey = targetNode.AutomationId;
        bool useRuntimeId = false;

        // AutomationIdがない場合、親を遡って有効なIDを持つ要素を探す
        // StructureはObservableCollection<UIElementInfo>だが、通常ルートは1つだけ入っている
        if (string.IsNullOrEmpty(targetKey))
        {
             // まず自分自身のRuntimeId
            if (!string.IsNullOrEmpty(targetNode.RuntimeId))
            {
                targetKey = targetNode.RuntimeId;
                useRuntimeId = true;
                StatusMessage.Value = $"Target has no AutomationId. Using RuntimeId: {targetNode.RuntimeId}";
            }
            else if (Structure.Count > 0)
            {
                var root = Structure[0];
                var path = new List<UIElementInfo>();
                if (FindPath(root, targetNode, path))
                {
                    // パスは Root -> Parent -> Target の順
                    // 逆順（Target -> Parent -> Root）に探索
                    path.Reverse();
                    foreach (var node in path)
                    {
                        if (!string.IsNullOrEmpty(node.AutomationId))
                        {
                            targetNode = node;
                            targetKey = node.AutomationId;
                            useRuntimeId = false;
                            StatusMessage.Value = $"Target has no ID. Creating path and using ancestor: {node.Name} ({node.AutomationId})";
                            break;
                        }
                         if (!string.IsNullOrEmpty(node.RuntimeId))
                        {
                            targetNode = node;
                            targetKey = node.RuntimeId;
                            useRuntimeId = true;
                            StatusMessage.Value = $"Target has no ID. Creating path and using ancestor: {node.Name} (RuntimeId: {node.RuntimeId})";
                            break;
                        }
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(targetKey))
        {
            StatusMessage.Value = "Scroll Error: No AutomationId or RuntimeId found in node or any ancestor.";
            return;
        }

        StatusMessage.Value = $"Scrolling into view: {targetNode?.Name} ({(useRuntimeId ? "RuntimeId" : "AutomationId")}: {targetKey})...";
        bool success = _windowService.ScrollToElement(SelectedWindow.Value.Handle, targetKey, useRuntimeId);
        
        if (success)
        {
            StatusMessage.Value = "Scroll Success. Refreshing structure...";
            // スクロール後は位置が変わっているため再取得
            GetStructure();
        }
        else
        {
            StatusMessage.Value = "Scroll Failed (Not scrollable or not found).";
        }
    }

    private bool FindPath(UIElementInfo current, UIElementInfo target, List<UIElementInfo> path)
    {
        path.Add(current);
        if (current == target) return true;

        foreach (var child in current.Children)
        {
            if (FindPath(child, target, path)) return true;
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }

    private void OnChatWindowConfirmed(WindowInfo window)
    {
        // 既存のSelectedWindowを更新 -> Subscribe済みのGenerateMapが自動的に走る
        SelectedWindow.Value = window;
    }

    private void RefreshWindows()
    {
        StatusMessage.Value = "Refreshing...";
        Windows.Clear();
        Structure.Clear();
        try
        {
            var windows = _windowService.GetWindows();
            foreach (var window in windows)
            {
                Windows.Add(window);
            }
            StatusMessage.Value = $"Found {Windows.Count} windows.";
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"Error: {ex.Message}";
        }
    }

    private void GetStructure()
    {
        if (SelectedWindow.Value == null) return;

        StatusMessage.Value = $"Getting structure for {SelectedWindow.Value.Title}...";
        Structure.Clear();
        try
        {
            var root = _windowService.GetWindowStructure(SelectedWindow.Value.Handle);
            if (root != null)
            {
                Structure.Add(root);
                StatusMessage.Value = "Structure retrieved.";
            }
            else
            {
                StatusMessage.Value = "Failed to retrieve structure.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"Error: {ex.Message}";
        }
    }

    private void GenerateMap()
    {
        if (SelectedWindow.Value == null) return;

        StatusMessage.Value = $"Generating UI Map for {SelectedWindow.Value.Title}...";
        UIMapJson.Value = "Generating...";
        AiMapJson.Value = "Generating...";
        try
        {
            // まず構造を取得
            var root = _windowService.GetWindowStructure(SelectedWindow.Value.Handle);
            if (root != null)
            {
                // マップ生成 (Internal Full Map)
                var map = _uiMapService.GenerateMap(root);
                
                var options = new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                // Internal Map Serialize
                UIMapJson.Value = System.Text.Json.JsonSerializer.Serialize(map, options);
                _currentMap = map;

                // Update Chat Context
                if (map.Root != null)
                {
                    ChatViewModel.CurrentContext = map.Root;
                }

                // AI Map Conversion
                if (map.Root != null)
                {
                    var aiRoot = _uiMapService.ConvertToAiNode(map.Root);
                    var aiMap = new { WindowName = map.WindowName, Root = aiRoot };
                    AiMapJson.Value = System.Text.Json.JsonSerializer.Serialize(aiMap, options);
                }
                else
                {
                    AiMapJson.Value = "No Root Element";
                }
                
                StatusMessage.Value = "UI Map generated.";
            }
            else
            {
                StatusMessage.Value = "Failed to retrieve structure for map generation.";
                UIMapJson.Value = "Error: Could not retrieve window structure.";
                AiMapJson.Value = "Error";
                _currentMap = null;
            }
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"Error: {ex.Message}";
            UIMapJson.Value = $"Error: {ex.Message}";
            AiMapJson.Value = $"Error: {ex.Message}";
            _currentMap = null;
        }
    }

    private void ShowOverlay()
    {
        if (_currentMap?.Root != null)
        {
            _overlayService.Show(_currentMap.Root);
            StatusMessage.Value = "Overlay shown (Press Esc to close is not implemented yet, but Click-through is active)";
        }
    }

    private void CloseOverlay()
    {
        _overlayService.Hide();
        StatusMessage.Value = "Overlay closed.";
    }

    private void ShowSettings()
    {
        // 設定画面を開く
        // ViewModelはMainWindowと同じUIスレッドで作成される想定
        var settingsVm = new SettingsViewModel(_settingsService);
        var settingsWin = new Views.SettingsWindow(settingsVm);
        settingsWin.Owner = System.Windows.Application.Current.MainWindow; // モーダル親設定
        settingsWin.ShowDialog();
        
        // Windowが閉じられたらDispose
        settingsVm.Dispose();
    }
}
