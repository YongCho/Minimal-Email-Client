using MinimalEmailClient.ViewModels;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    public partial class MessageListView : UserControl
    {
        MessageListViewModel viewModel;
        public MessageListView()
        {
            InitializeComponent();
            viewModel = (MessageListViewModel)this.DataContext;
        }

        private void messageListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (MessageHeaderViewModel msgHeaderVm in messageListView.SelectedItems)
            {
                viewModel.SelectedMessageHeaderViewModels.Add(msgHeaderVm);
            }
        }
    }
}
