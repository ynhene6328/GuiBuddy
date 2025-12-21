using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GuiBuddy.App.ViewModels;

namespace GuiBuddy.App.Views;

/// <summary>
/// Interaction logic for DebugView.xaml
/// </summary>
public partial class DebugView : UserControl
{
    public DebugView()
    {
        InitializeComponent();
    }

    public DebugView(DebugViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is DebugViewModel vm)
        {
            vm.SelectedNode.Value = e.NewValue as GuiBuddy.Core.Models.UIElementInfo;
        }
    }
}