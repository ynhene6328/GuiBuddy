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
    public ReactiveProperty<WindowInfo?> SelectedWindow { get; } = new();
    public ObservableCollection<UIElementInfo> Structure { get; } = new();
    public ReactiveCommand GetStructureCommand { get; }

    public MainWindowViewModel(IWindowService windowService)
    {
        _windowService = windowService;

        RefreshCommand = new ReactiveCommand();
        RefreshCommand.Subscribe(_ => RefreshWindows());

        GetStructureCommand = SelectedWindow.Select(w => w != null).ToReactiveCommand();
        GetStructureCommand.Subscribe(_ => GetStructure());
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
}
