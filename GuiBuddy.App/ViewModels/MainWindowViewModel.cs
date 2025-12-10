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

    private readonly IWindowService _windowService;

    public ReactiveProperty<string> StatusMessage { get; } = new("Ready");
    public ObservableCollection<WindowInfo> Windows { get; } = new();
    public ReactiveCommand RefreshCommand { get; }

    public MainWindowViewModel(IWindowService windowService)
    {
        _windowService = windowService;

        RefreshCommand = new ReactiveCommand();
        RefreshCommand.Subscribe(_ => RefreshWindows());
    }

    private void RefreshWindows()
    {
        StatusMessage.Value = "Refreshing...";
        Windows.Clear();
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
}
