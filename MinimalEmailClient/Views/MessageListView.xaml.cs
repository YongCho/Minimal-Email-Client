using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using MinimalEmailClient.ViewModels;
using Prism.Events;
using System.Collections.Generic;
using System.Windows.Controls;
using MinimalEmailClient.Services;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for MessageListView.xaml
    /// </summary>
    public partial class MessageListView : UserControl
    {
        private MessageListViewModel viewModel;
        private MessageManager messageManager = MessageManager.Instance;

        public MessageListView()
        {
            InitializeComponent();

            viewModel = (MessageListViewModel)this.DataContext;
            GlobalEventAggregator.Instance.GetEvent<DeleteMessagesEvent>().Subscribe(HandleDeleteMessagesEvent, ThreadOption.UIThread);
        }

        private void HandleDeleteMessagesEvent(string ignoredEventPayload)
        {
            List<Message> selectedMessages = new List<Message>();

            foreach (Message message in messageListView.SelectedItems)
            {
                selectedMessages.Add(message);
            }

            messageManager.DeleteMessages(selectedMessages);
        }
    }
}
