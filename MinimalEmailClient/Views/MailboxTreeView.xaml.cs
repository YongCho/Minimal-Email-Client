using System.Windows;
using System.Windows.Controls;
using MinimalEmailClient.ViewModels;
using MinimalEmailClient.Models;

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
    }
}
