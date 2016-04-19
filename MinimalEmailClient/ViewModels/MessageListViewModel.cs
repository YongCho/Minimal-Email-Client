using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using MinimalEmailClient.Notifications;
using MinimalEmailClient.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MinimalEmailClient.ViewModels
{
    public class MessageListViewModel : BindableBase
    {
        public ObservableCollection<MessageHeaderViewModel> MessageHeaderViewModels { get; set; }
        private CollectionView messagesCv;
        private MessageHeaderViewModel selectedMessageHeaderViewModel;  // This could be null.
        public MessageHeaderViewModel SelectedMessageHeaderViewModel
        {
            get { return this.selectedMessageHeaderViewModel; }
            set
            {
                SetProperty(ref this.selectedMessageHeaderViewModel, value);
                if (this.selectedMessageHeaderViewModel == null)
                {
                    GlobalEventAggregator.Instance.GetEvent<MessageSelectionEvent>().Publish(null);
                }
                else
                {
                    GlobalEventAggregator.Instance.GetEvent<MessageSelectionEvent>().Publish(this.selectedMessageHeaderViewModel.Message);
                }
            }
        }
        public List<MessageHeaderViewModel> SelectedMessageHeaderViewModels { get; set; }  // For multiple selection. Let it set by view on selection change events.
        private Mailbox currentMailbox;
        public Mailbox CurrentMailbox
        {
            get { return this.currentMailbox; }
            set { SetProperty(ref this.currentMailbox, value); }
        }
        private MessageManager messageManager = MessageManager.Instance;

        public InteractionRequest<MessageContentViewNotification> MessageContentViewPopupRequest { get; set; }
        public ICommand OpenMessageContentViewCommand { get; set; }
        public ICommand DeleteMessageCommand { get; set; }

        public MessageListViewModel()
        {
            LoadMessages();
            messageManager.MessageAdded += OnMessageAdded;
            messageManager.MessageRemoved += OnMessageRemoved;

            MessageContentViewPopupRequest = new InteractionRequest<MessageContentViewNotification>();
            OpenMessageContentViewCommand = new DelegateCommand(RaiseMessageContentViewPopupRequest);
            DeleteMessageCommand = new DelegateCommand(RaiseDeleteMessagesEvent);
            this.messagesCv = (CollectionView)CollectionViewSource.GetDefaultView(MessageHeaderViewModels);
            this.messagesCv.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));
            SelectedMessageHeaderViewModels = new List<MessageHeaderViewModel>();

            HandleMailboxSelectionChange(null);

            GlobalEventAggregator.Instance.GetEvent<MailboxSelectionEvent>().Subscribe(HandleMailboxSelectionChange, ThreadOption.UIThread);
            GlobalEventAggregator.Instance.GetEvent<DeleteMessagesEvent>().Subscribe(HandleDeleteMessagesEvent, ThreadOption.UIThread);
        }

        public void OnMessageAdded(object sender, Message newMessage)
        {
            Application.Current.Dispatcher.Invoke(() => { MessageHeaderViewModels.Add(new MessageHeaderViewModel(newMessage)); });
        }

        public void OnMessageRemoved(object sender, Message removedMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (MessageHeaderViewModel messageVm in MessageHeaderViewModels)
                {
                    if (messageVm.Message == removedMessage)
                    {
                        MessageHeaderViewModels.Remove(messageVm);
                        break;
                    }
                }
            });
        }

        private void LoadMessages()
        {
            if (MessageHeaderViewModels == null)
            {
                MessageHeaderViewModels = new ObservableCollection<MessageHeaderViewModel>();
            }
            else
            {
                MessageHeaderViewModels.Clear();
            }

            List<Message> messages = messageManager.MessagesDico.Values.ToList();
            foreach (Message msg in messages)
            {
                MessageHeaderViewModels.Add(new MessageHeaderViewModel(msg));
            }
        }

        private void RaiseMessageContentViewPopupRequest()
        {
            if (SelectedMessageHeaderViewModel != null)
            {
                Account currentMailboxAccount = AccountManager.Instance.GetAccountByName(CurrentMailbox.AccountName);
                MessageContentViewNotification notification = new MessageContentViewNotification(currentMailboxAccount, CurrentMailbox, SelectedMessageHeaderViewModel.Message);
                notification.Title = SelectedMessageHeaderViewModel.Message.Subject;
                MessageContentViewPopupRequest.Raise(notification);
            }
        }

        private void HandleMailboxSelectionChange(Mailbox selectedMailbox)
        {
            // Mailbox with \Noselect tag has no message to display. Ignore that mailbox.
            if (selectedMailbox == null || !selectedMailbox.Flags.Contains(@"\Noselect"))
            {
                CurrentMailbox = selectedMailbox;
            }

            this.messagesCv.Filter = new Predicate<object>(MessageFilter);
        }

        private void HandleDeleteMessagesEvent(string ignoredEventPayload)
        {
            List<Message> selectedMessages = new List<Message>();
            foreach (MessageHeaderViewModel msgHeaderVm in SelectedMessageHeaderViewModels)
            {
                selectedMessages.Add(msgHeaderVm.Message);
            }
            this.messageManager.DeleteMessages(selectedMessages);
        }

        private bool MessageFilter(object item)
        {
            // Do not display any messages when no mailbox is selected.
            if (CurrentMailbox == null)
            {
                return false;
            }

            MessageHeaderViewModel messageVm = item as MessageHeaderViewModel;
            bool showMsg = false;

            if (messageVm.AccountName == CurrentMailbox.AccountName &&
                messageVm.MailboxPath == CurrentMailbox.DirectoryPath)
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
