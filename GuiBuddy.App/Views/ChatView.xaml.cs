using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Input;
using GuiBuddy.App.ViewModels;

namespace GuiBuddy.App.Views
{
    public partial class ChatView : UserControl
    {
        public ChatView()
        {
            InitializeComponent();
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (DataContext is ChatViewModel vm)
                {
                    if (vm.SendCommand.CanExecute())
                    {
                        vm.SendCommand.Execute();
                        e.Handled = true; // Prevent newline
                    }
                }
            }
        }

        private void ChatListBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ChatListBox.ItemsSource is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += (s, args) =>
                {
                    if (ChatListBox.Items.Count > 0)
                    {
                        ChatListBox.ScrollIntoView(ChatListBox.Items[ChatListBox.Items.Count - 1]);
                    }
                };
            }
        }
        void TargetWindow_OnDropDownOpened(object sender, EventArgs e)
        {
            if (DataContext is ChatViewModel vm)
            {
                vm.RefreshWindowsCommand.Execute();
            }
        }
        private void TargetWindow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ComboBox選択時に自動的にウィンドウ確定
            if (DataContext is ChatViewModel vm && e.AddedItems.Count > 0)
            {
                if (vm.ConfirmTargetCommand.CanExecute())
                {
                    vm.ConfirmTargetCommand.Execute();
                }
            }
        }
    }
}
