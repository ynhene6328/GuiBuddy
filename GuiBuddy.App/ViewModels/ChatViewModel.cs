using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
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

    private readonly IChatService _chatService;
    private readonly IWindowService _windowService;
    private readonly IOverlayService _overlayService;
    private readonly IUIMapService _uiMapService;

    // Window Selection
    public ObservableCollection<WindowInfo> AvailableWindows { get; } = new();
    
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

    public ChatViewModel(IChatService chatService, IWindowService windowService, IOverlayService overlayService, IUIMapService uiMapService)
    {
        _chatService = chatService;
        _windowService = windowService;
        _overlayService = overlayService;
        _uiMapService = uiMapService;

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
                    // ConfirmWindowはSelectionChangedで呼ばれるが、ここでも明示的に呼ぶか、あるいはSelectionChangedに任せる
                    // ReactivePropertyの変更通知でUI側のイベントが発火し、ConfirmWindowが走るはず
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
                // エラーログは出すが、送信は続行（古いコンテキストかコンテキストなしで）
                Messages.Add(new ChatMessage("GuiBuddy-System", $"Warning: Context refresh failed: {ex.Message}"));
            }
        }

        AIResponse response = await _chatService.SendMessageAsync(userText, CurrentContext);
        
        Messages.Add(new ChatMessage("GuiBuddy-AI", response.ResponseText));

        // ハイライトとスクロール処理
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
            // CurrentContextが最新（または元のまま）
             if (CurrentContext != null)
            {
                _overlayService.Show(CurrentContext, showAll: false);
                foreach (var id in targetIds)
                {
                    // 再取得した場合、IDが変わっている可能性がある
                    // UIMapServiceの実装上、IDは生成順なので構造が同じなら同じになる可能性が高いが
                    // 厳密にはAutomationId等で再検索してIDを特定しなおすべき。
                    // しかし複雑になるため、今回は「座標更新後も構成が変わらなければIDズレは許容範囲」とするか、
                    // あるいは「スクロール後はIDが変わる」ことを前提に再検索するロジックを入れるか。
                    // UIMapService._nodeIdCounter = 1 でリセットされるため、構造が変わらなければIDは同じになるはず。
                    
                    // ただし動的なリスト読み込み等で構造が変わる場合はIDがずれる。
                    // ここでは簡易的に、元のIDでハイライトを試みる。
                    _overlayService.Highlight(id);
                }
            }
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

