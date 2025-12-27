using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows;
using System.Threading.Tasks;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GuiBuddy.App.ViewModels;

public class ChatViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    private const bool IsMonitoringFeatureEnabled = true;

    private readonly IChatService _chatService;
    private readonly IWindowService _windowService;
    private readonly IOverlayService _overlayService;
    private readonly IUIMapService _uiMapService;
    private readonly IUserActivityMonitor _userActivityMonitor;

    // Window Selection
    public ObservableCollection<WindowInfo> AvailableWindows { get; } = new();
    public ReactivePropertySlim<bool> IsExpanded { get; } = new(false);
    
    // Use ReactiveProperty for selection to handle notifications and logic easily
    public ReactiveProperty<WindowInfo?> SelectedTargetWindow { get; } = new();

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    private string _inputText = "";
    public string InputText 
    {
        get => _inputText;
        set
        {
            if (_inputText != value)
            {
                _inputText = value;
                OnPropertyChanged();
                _inputTextSubject.OnNext(value);
            }
        }
    }
    private readonly System.Reactive.Subjects.Subject<string> _inputTextSubject = new();

    public ReactiveCommand SendCommand { get; }
    public ReactiveCommand RefreshWindowsCommand { get; }
    public ReactiveCommand ConfirmTargetCommand { get; }
    public ReactiveCommand OpenSettingsCommand { get; }

    // Event to notify parent view model
    public event Action<WindowInfo>? WindowConfirmed;
    public event Action? OpenSettingsRequested;

    public UiNode? CurrentContext { get; set; } // Set by parent ViewModel

    public ChatViewModel(IChatService chatService, IWindowService windowService, IOverlayService overlayService, IUIMapService uiMapService, IUserActivityMonitor userActivityMonitor)
    {
        _chatService = chatService;
        _windowService = windowService;
        _overlayService = overlayService;
        _uiMapService = uiMapService;
        _userActivityMonitor = userActivityMonitor;

        // Window Highlight Logic
        SelectedTargetWindow
            .Where(w => w != null)
            .Subscribe(w => _overlayService.HighlightWindow(w!));

        SendCommand = _inputTextSubject
            .Select(x => !string.IsNullOrWhiteSpace(x))
            .ToReactiveCommand();
        SendCommand.Subscribe(async _ => await SendMessage());

        RefreshWindowsCommand = new ReactiveCommand();
        RefreshWindowsCommand.Subscribe(_ => LoadWindows());

        ConfirmTargetCommand = SelectedTargetWindow
            .Select(w => w != null)
            .ToReactiveCommand();
        ConfirmTargetCommand.Subscribe(_ => ConfirmWindow());

        OpenSettingsCommand = new ReactiveCommand();
        OpenSettingsCommand.Subscribe(_ => OpenSettingsRequested?.Invoke());

        // Initial Load
        LoadWindows();
        
        // Initial Subject Value
        _inputTextSubject.OnNext("");

        // Monitor activity
        _userActivityMonitor.InputDetected += OnUserActivityDetected;
        
        // ウィンドウ切り替えに応じて監視を切り替える
        SelectedTargetWindow
            .Subscribe(w => 
            {
                if (w != null)
                {
                    // 監視を停止（手動制御が前提なら、ここでは停止するだけでいいかもしれないが、
                    // 前回の実装ではSelectされたらポーリング開始していたため、それに倣うか、
                    // あるいは「UserGoalが出たら開始」という仕様を遵守するか。
                    // ユーザーの手動実装は「Selectされたらポーリング開始」だった。
                    // しかし過剰反応を防ぐには「AI指示後のみ」が良い。
                    // ここでは一旦停止し、StartUIMonitoringで再開するフローにする。
                    StopUIMonitoring();
                }
                else
                {
                    StopUIMonitoring();
                }
            });

        IsExpanded.Where(expanded => expanded == true)
            .Subscribe(_ => LoadWindows());
    }

    private void LoadWindows()
    {
        AvailableWindows.Clear();
        try
        {
            var windows = _windowService.GetWindows();
            foreach (var w in windows)
            {
                AvailableWindows.Add(w);
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage("GuiBuddy-System", $"Error loading windows: {ex.Message}"));
        }
    }

    private void ConfirmWindow()
    {
        if (SelectedTargetWindow.Value != null)
        {
            WindowConfirmed?.Invoke(SelectedTargetWindow.Value);
            Messages.Add(new ChatMessage("GuiBuddy-System", $"対象ウィンドウを設定しました: {SelectedTargetWindow.Value.Title}"));
        }
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        // 1. ウィンドウ追従機能 (Window Tracking)
        // ユーザーが現在操作しているフォアグラウンドウィンドウを確認
        var foregroundWindow = _windowService.GetForegroundWindow();
        if (foregroundWindow != null && SelectedTargetWindow.Value != null)
        {
            // 現在の対象ウィンドウと異なるプロセスIDだが、同一プロセス内の別ウィンドウの場合
            // または、プロセスIDが一致するがハンドルが異なる場合（こちらのほうが確実）
            bool isSameProcess = foregroundWindow.ProcessId == SelectedTargetWindow.Value.ProcessId;
            bool isDifferentHandle = foregroundWindow.Handle != SelectedTargetWindow.Value.Handle;

            if (isSameProcess && isDifferentHandle)
            {
                // リストにあるか確認し、あればそれに切り替える
                // なければリストを更新してから切り替える必要があるが、まずは既存リストから検索
                var existingWrapper = AvailableWindows.FirstOrDefault(w => w.Handle == foregroundWindow.Handle);
                
                if (existingWrapper != null)
                {
                    SelectedTargetWindow.Value = existingWrapper;
                    Messages.Add(new ChatMessage("GuiBuddy-System", $"対象ウィンドウを自動切り替えしました: {existingWrapper.Title}"));
                }
                else
                {
                    // リストにないのでリロードして再検索
                    LoadWindows();
                    existingWrapper = AvailableWindows.FirstOrDefault(w => w.Handle == foregroundWindow.Handle);
                    if (existingWrapper != null)
                    {
                        SelectedTargetWindow.Value = existingWrapper;
                        Messages.Add(new ChatMessage("GuiBuddy-System", $"対象ウィンドウを自動切り替えしました: {existingWrapper.Title}"));
                    }
                }
            }
        }

        string userText = InputText;
        InputText = ""; // Clear input

        Messages.Add(new ChatMessage("User", userText));

        // 送信前に現在のターゲットウィンドウの情報を再取得してコンテキストを更新
        if (SelectedTargetWindow.Value != null)
        {
            try
            {
                // UI構造の再取得
                var root = _windowService.GetWindowStructure(SelectedTargetWindow.Value.Handle);
                if (root != null)
                {
                    // ID付与ロジックはGenerateMapで走るため、Map生成を通してからContextに設定
                    var map = _uiMapService.GenerateMap(root);
                    CurrentContext = map.Root;
                }
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage("GuiBuddy-System", $"Warning: Context refresh failed: {ex.Message}"));
            }
        }

        AIResponse response = await _chatService.SendMessageAsync(userText, CurrentContext);
        
        Messages.Add(new ChatMessage("GuiBuddy-AI", response.ResponseText + "\n\n---------------\n\n" + response.ContextSummary));

        // UserGoalが返ってきたら監視を開始
        if (!string.IsNullOrWhiteSpace(response.UserGoal))
        {
            StartUIMonitoring();
            Messages.Add(new ChatMessage("GuiBuddy-System", "[監視開始] 画面変化の自動検知を開始しました。"));
        }

        // ハイライトとスクロール処理
        ProcessHighlightAndScroll(response);
    }
    
    private void ProcessHighlightAndScroll(AIResponse response){
        if (response.TargetElementIds != null && response.TargetElementIds.Count > 0 && CurrentContext != null)
        {
            var targetIds = response.TargetElementIds;
            bool needRefresh = false;

            // 画面外要素のチェックとスクロール
            foreach (var id in targetIds)
            {
                var node = FindNodeById(CurrentContext, id);
                if (node != null && node.IsOffscreen)
                {
                    string? targetKey = node.AutomationId;
                    bool useRuntimeId = false;

                    // AutomationIdがない場合、親を遡る
                    if (string.IsNullOrEmpty(targetKey))
                    {
                        // まず自分自身のRuntimeIdをチェック (あれば親遡りは不要かもしれないが念のため)
                        if (!string.IsNullOrEmpty(node.RuntimeId))
                        {
                            targetKey = node.RuntimeId;
                            useRuntimeId = true;
                        }
                        else
                        {
                            var path = new List<UiNode>();
                            if (FindNodePath(CurrentContext, node.Id, path))
                            {
                                path.Reverse(); // Target -> Parent -> Root
                                foreach(var ancestor in path)
                                {
                                    if (!string.IsNullOrEmpty(ancestor.AutomationId))
                                    {
                                        targetKey = ancestor.AutomationId;
                                        useRuntimeId = false;
                                        break;
                                    }
                                    // 祖先のRuntimeIdもチェック
                                    if (!string.IsNullOrEmpty(ancestor.RuntimeId))
                                    {
                                        targetKey = ancestor.RuntimeId;
                                        useRuntimeId = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(targetKey))
                    {
                        // スクロール試行
                        bool scrollSuccess = _windowService.ScrollToElement(SelectedTargetWindow.Value!.Handle, targetKey, useRuntimeId);
                        if (scrollSuccess)
                        {
                            needRefresh = true;
                        }
                        else
                        {
                            Messages.Add(new ChatMessage("GuiBuddy-System", $"注意: 対象要素の一つが画面外ですが、スクロールできませんでした。(ID: {id})"));
                        }
                    }
                }
            }

            if (needRefresh)
            {
                // スクロールにより座標が変わった可能性があるため再取得
                try
                {
                    var root = _windowService.GetWindowStructure(SelectedTargetWindow.Value!.Handle);
                    if (root != null)
                    {
                        var map = _uiMapService.GenerateMap(root);
                        CurrentContext = map.Root;
                        // マップ更新時はオーバーレイも更新
                        _overlayService.Update(CurrentContext);
                    }
                }
                catch (Exception ex)
                {
                    Messages.Add(new ChatMessage("GuiBuddy-System", $"Warning: Post-scroll refresh failed: {ex.Message}"));
                }
            }

            // オーバーレイ表示とハイライト
             if (CurrentContext != null)
            {
                _overlayService.Show(CurrentContext, showAll: false);
                foreach (var id in targetIds)
                {
                    _overlayService.Highlight(id);
                }
            }
        }
    }

    // --- UI監視制御 ---

    /// <summary>
    /// UI監視を開始します。
    /// </summary>
    private void StartUIMonitoring()
    {
        if (!IsMonitoringFeatureEnabled) return;
        if (SelectedTargetWindow.Value == null) return;
        _userActivityMonitor.StartMonitoring(SelectedTargetWindow.Value.Handle);
    }

    /// <summary>
    /// UI監視を停止します。
    /// </summary>
    private void StopUIMonitoring()
    {
        if (!IsMonitoringFeatureEnabled) return;
        _userActivityMonitor.StopMonitoring();
    }

    private async void OnUserActivityDetected(object? sender, EventArgs e)
    {
        // UIスレッドで実行
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
             await HandleInputAsync();
        });
    }

    private async Task HandleInputAsync()
    {
        if (!IsMonitoringFeatureEnabled) return;

        var target = SelectedTargetWindow.Value;
        if (target == null) return;

        try
        {
            var root = _windowService.GetWindowStructure(target.Handle);
            if (root == null) return;

            var map = _uiMapService.GenerateMap(root);

            CurrentContext = map.Root;
            _overlayService.ClearHighlight();
            _overlayService.Update(CurrentContext);
            Messages.Add(new ChatMessage("GuiBuddy-System", $"UI変化を検出しました: {target.Title}"));

            // Notify AI about the change
            try
            {
                // 監視を一旦停止（連続反応を防ぐため、またはAIが次の指示を出すまで待機）
                // StopUIMonitoring(); // ※必要に応じて

                var aiResponse = await _chatService.SendMessageAsync($"ユーザーの操作によってUI要素が変化しました、最終目的が達成されているか確認し、達成されていなければ次の操作を教えてください", CurrentContext);
                if (aiResponse != null)
                {
                    Messages.Add(new ChatMessage("GuiBuddy-AI", aiResponse.ResponseText + "\n\n---------------\n\n" + aiResponse.ContextSummary));
                    ProcessHighlightAndScroll(aiResponse);
                    
                    // AIから追加の指示があれば監視継続、なければ（ゴールなら）停止等のロジックも検討可能だが、
                    // 現状は「UI変化検知 -> AI確認」のループ
                }
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage("GuiBuddy-System", $"Warning: AI notify failed: {ex.Message}"));
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage("GuiBuddy-System", $"Warning: Input handling failed: {ex.Message}"));
        }
    }

    private UiNode? FindNodeById(UiNode root, int id)
    {
        if (root.Id == id) return root;
        foreach (var child in root.Children)
        {
            var found = FindNodeById(child, id);
            if (found != null) return found;
        }
        return null;
    }

    private bool FindNodePath(UiNode current, int targetId, List<UiNode> path)
    {
        path.Add(current);
        if (current.Id == targetId) return true;

        foreach (var child in current.Children)
        {
            if (FindNodePath(child, targetId, path)) return true;
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }
}

