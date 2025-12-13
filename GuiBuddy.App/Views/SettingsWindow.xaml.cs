using System.Windows;
using GuiBuddy.App.ViewModels;

namespace GuiBuddy.App.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += () => this.Close();
        }
    }
}
