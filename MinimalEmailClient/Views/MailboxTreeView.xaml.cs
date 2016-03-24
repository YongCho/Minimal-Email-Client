using System.Windows;
using System.Windows.Controls;
using MinimalEmailClient.ViewModels;
using MinimalEmailClient.Models;
using System.Diagnostics;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for MailboxTreeView.xaml
    /// </summary>
    public partial class MailboxTreeView : UserControl
    {
        public MailboxTreeView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            MailboxTreeViewModel viewModel = (MailboxTreeViewModel)this.DataContext;
            viewModel.SelectedTreeViewItem = e.NewValue;
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
                    if (textBlock.DataContext is Account)
                    {
                        Account ac = textBlock.DataContext as Account;
                        var viewModel = this.DataContext as MailboxTreeViewModel;
                        if (viewModel.DeleteAccountCommand.CanExecute(ac))
                        {
                            viewModel.DeleteAccountCommand.Execute(ac);
                        }
                    }
                }
            }
        }
    }
}
