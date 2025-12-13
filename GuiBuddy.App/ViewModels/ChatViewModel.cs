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
                // CanExecuteを更新するためにイベントを発行したいが、
                // ReactiveCommandでObserveしている場合、Propertyの変更通知があればOKか？
                // ReactivePropertyからReactiveCommandを作る場合と違うので、
                // ここは手動でCanExecuteChangedを呼ぶか、ReactiveCommandをSubjectから作る等の工夫が必要。
                // 簡易的にInputTextSubjectを使ってReactiveCommandを生成する。
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

    public ChatViewModel(IChatService chatService, IWindowService windowService, IOverlayService overlayService)
    {
        _chatService = chatService;
        _windowService = windowService;
        _overlayService = overlayService;

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
            Messages.Add(new ChatMessage("System", $"Error loading windows: {ex.Message}"));
        }
    }

    private void ConfirmWindow()
    {
        if (SelectedTargetWindow.Value != null)
        {
            WindowConfirmed?.Invoke(SelectedTargetWindow.Value);
            Messages.Add(new ChatMessage("System", $"Target set to: {SelectedTargetWindow.Value.Title}"));
        }
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        string userText = InputText;
        InputText = ""; // Clear input, this updates the view but usually safe for cursor if done after send

        Messages.Add(new ChatMessage("User", userText));

        // Add a temporary "Typing..." or similar if desired, but for Mock is fast.
        
        string response = await _chatService.SendMessageAsync(userText, CurrentContext);
        
        Messages.Add(new ChatMessage("GuiBuddy", response));
    }
}
