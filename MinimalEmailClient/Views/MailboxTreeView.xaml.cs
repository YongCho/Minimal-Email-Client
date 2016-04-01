using System.Windows;
using System.Windows.Controls;
using MinimalEmailClient.ViewModels;
using System.Diagnostics;

namespace MinimalEmailClient.Views
{
    public partial class MailboxTreeView : UserControl
    {
        public MailboxTreeView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            MailboxTreeViewModel viewModel = this.DataContext as MailboxTreeViewModel;
            if (viewModel != null)
            {
                viewModel.SelectedTreeViewItem = e.NewValue;
            }
        }

        private void DeleteAccountMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                ContextMenu cm = mi.CommandParameter as ContextMenu;
                if (cm != null)
                {
                    TextBlock textBlock = cm.PlacementTarget as TextBlock;
                    if (textBlock.DataContext is AccountViewModel)
                    {
                        AccountViewModel accountVm = textBlock.DataContext as AccountViewModel;
                        var viewModel = this.DataContext as MailboxTreeViewModel;
                        if (viewModel != null && viewModel.DeleteAccountCommand.CanExecute(accountVm))
                        {
                            viewModel.DeleteAccountCommand.Execute(accountVm);
                        }
                    }
                }
            }
        }
    }
}
