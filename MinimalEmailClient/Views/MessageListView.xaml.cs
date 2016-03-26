using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using MinimalEmailClient.ViewModels;
using Prism.Events;
using System.Collections.Generic;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for MessageListView.xaml
    /// </summary>
    public partial class MessageListView : UserControl
    {
        private MessageListViewModel viewModel;
        private MessageManager messageManager = MessageManager.Instance;
        private IEventAggregator eventAggregator;

        public MessageListView()
        {
            InitializeComponent();

            viewModel = (MessageListViewModel)this.DataContext;
            this.eventAggregator = GlobalEventAggregator.Instance().EventAggregator;
            this.eventAggregator.GetEvent<DeleteMessagesEvent>().Subscribe(HandleDeleteMessagesEvent);
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
