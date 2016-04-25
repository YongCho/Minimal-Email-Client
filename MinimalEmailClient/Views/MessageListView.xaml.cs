using Hardcodet.Wpf.TaskbarNotification;
using MinimalEmailClient.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MinimalEmailClient.Views
{
    public partial class MessageListView : UserControl
    {
        MessageListViewModel viewModel;
        public MessageListView()
        {
            InitializeComponent();
            viewModel = (MessageListViewModel)this.DataContext;
            viewModel.NewMessageArrived += OnNewMessageArrived;
        }

        private void OnNewMessageArrived(object sender, MessageHeaderViewModel newMessageHeaderViewModel)
        {
            Dispatcher.Invoke(() => {
                var balloon = new NewMessageBalloon(newMessageHeaderViewModel);
                TaskbarIcon tb = Application.Current.Resources["TbIcon"] as TaskbarIcon;
                if (tb != null)
                {
                    tb.ShowCustomBalloon(balloon, PopupAnimation.Fade, 10000);
                }
            });
        }

        private void messageListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            viewModel.SelectedMessageHeaderViewModels.Clear();
            foreach (MessageHeaderViewModel msgHeaderVm in messageListView.SelectedItems)
            {
                viewModel.SelectedMessageHeaderViewModels.Add(msgHeaderVm);
            }
        }
    }
}
