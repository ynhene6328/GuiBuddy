using Reactive.Bindings;
using System;

namespace GuiBuddy.App.ViewModels;

public class MainWindowViewModel
{
    public ReactiveCommand ToggleDebugCommand { get; }
    public DebugViewModel DebugViewModel { get; }
    public ChatViewModel ChatViewModel { get; }

    public MainWindowViewModel(DebugViewModel debugViewModel, ChatViewModel chatViewModel)
    {
        DebugViewModel = debugViewModel ?? throw new ArgumentNullException(nameof(debugViewModel));
        ChatViewModel = chatViewModel ?? throw new ArgumentNullException(nameof(chatViewModel));

        ToggleDebugCommand = new ReactiveCommand();
        ToggleDebugCommand.Subscribe(() => DebugViewModel.IsDebugVisible.Value = !DebugViewModel.IsDebugVisible.Value);
    }
}