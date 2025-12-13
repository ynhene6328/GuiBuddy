using System.Collections.ObjectModel;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.ComponentModel;

namespace GuiBuddy.App.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;



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

    // Debug View Toggle
    public ReactiveProperty<bool> IsDebugVisible { get; } = new(false);
    public ReactiveCommand ToggleDebugCommand { get; }

    private readonly IWindowService _windowService;
    private readonly IUIMapService _uiMapService;
    private readonly IOverlayService _overlayService;
    private UIMap? _currentMap;

    public ChatViewModel ChatViewModel { get; }

    public MainWindowViewModel(IWindowService windowService, IUIMapService uiMapService, IOverlayService overlayService, ChatViewModel chatViewModel)
    {
        _windowService = windowService;
        _uiMapService = uiMapService;
        _overlayService = overlayService;
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

        // ChatViewModelからのウィンドウ確定通知を受け取る
        ChatViewModel.WindowConfirmed += OnChatWindowConfirmed;
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
}
