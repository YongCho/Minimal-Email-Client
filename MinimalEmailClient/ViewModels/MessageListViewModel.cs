using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Data;
using System.ComponentModel;
using Prism.Interactivity.InteractionRequest;
using System.Windows.Input;
using Prism.Commands;
using System;
using System.Windows;

namespace MinimalEmailClient.ViewModels
{
    public class MessageListViewModel : BindableBase
    {
        public ObservableCollection<Message> Messages { get; set; }
        private CollectionView messagesCv;
        private Message selectedMessage;  // This could be null
        public Message SelectedMessage
        {
            get { return this.selectedMessage; }
            set { SetProperty(ref this.selectedMessage, value); }
        }
        private Account selectedAccount;
        private Mailbox selectedMailbox;
        private MessageManager messageManager = MessageManager.Instance;

        public InteractionRequest<SelectedMessageNotification> OpenSelectedMessagePopupRequest { get; set; }
        public ICommand OpenSelectedMessageCommand { get; set; }
        public ICommand DeleteMessageCommand { get; set; }

        public MessageListViewModel()
        {
            LoadMessages();
            messageManager.MessageAdded += OnMessageAdded;
            messageManager.MessageRemoved += OnMessageRemoved;

            OpenSelectedMessagePopupRequest = new InteractionRequest<SelectedMessageNotification>();
            OpenSelectedMessageCommand = new DelegateCommand(RaiseOpenSelectedMessagePopupRequest);
            DeleteMessageCommand = new DelegateCommand(RaiseDeleteMessagesEvent);
            this.messagesCv = (CollectionView)CollectionViewSource.GetDefaultView(Messages);
            this.messagesCv.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));

            HandleMailboxSelectionChange(null);

            GlobalEventAggregator.Instance.GetEvent<MailboxSelectionEvent>().Subscribe(HandleMailboxSelectionChange);
            GlobalEventAggregator.Instance.GetEvent<MailboxListSyncFinishedEvent>().Subscribe(HandleMailboxListSyncFinished);
        }

        public void OnMessageAdded(object sender, Message newMessage)
        {
            Application.Current.Dispatcher.Invoke(() => { Messages.Add(newMessage); });
        }

        public void OnMessageRemoved(object sender, Message removedMessage)
        {
            Application.Current.Dispatcher.Invoke(() => { Messages.Remove(removedMessage); });
        }

        public async void LoadMessages()
        {
            if (Messages == null)
            {
                Messages = new ObservableCollection<Message>();
            }
            else
            {
                Messages.Clear();
            }

            List<Message> messages = await Task.Run<List<Message>>(() => {
                return messageManager.Messages;
            });
            Messages.AddRange(messages);
        }

        private void RaiseOpenSelectedMessagePopupRequest()
        {
            if (SelectedMessage != null)
            {
                SelectedMessageNotification notification = new SelectedMessageNotification(this.selectedAccount, this.selectedMailbox, SelectedMessage);
                notification.Title = SelectedMessage.Subject;
                OpenSelectedMessagePopupRequest.Raise(notification);
            }
        }

        private void HandleMailboxListSyncFinished(Account account)
        {
            messageManager.BeginSyncMessages(account);
        }

        private void HandleMailboxSelectionChange(Mailbox selectedMailbox)
        {
            this.selectedMailbox = selectedMailbox;
            if (this.selectedMailbox != null)
            {
                this.selectedAccount = AccountManager.Instance.GetAccountByName(selectedMailbox.AccountName);
            }
            this.messagesCv.Filter = new Predicate<object>(MessageFilter);
        }

        private bool MessageFilter(object item)
        {
            // Do not display any messages when no mailbox is selected.
            if (this.selectedMailbox == null)
            {
                return false;
            }

            Message message = item as Message;
            bool showMsg = false;

            if (message.AccountName == this.selectedAccount.AccountName &&
                message.MailboxPath == this.selectedMailbox.DirectoryPath)
            {
                showMsg = true;
            }

            return showMsg;
        }

        private void RaiseDeleteMessagesEvent()
        {
            GlobalEventAggregator.Instance.GetEvent<DeleteMessagesEvent>().Publish("Dummy Payload");
        }
    }
}
