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

        private IEventAggregator eventAggregator;
        public InteractionRequest<SelectedMessageNotification> OpenSelectedMessagePopupRequest { get; set; }
        public ICommand OpenSelectedMessageCommand { get; set; }
        public ICommand DeleteMessageCommand { get; set; }

        public MessageListViewModel()
        {
            Messages = new ObservableCollection<Message>();
            OpenSelectedMessagePopupRequest = new InteractionRequest<SelectedMessageNotification>();
            OpenSelectedMessageCommand = new DelegateCommand(RaiseOpenSelectedMessagePopupRequest);
            DeleteMessageCommand = new DelegateCommand(RaiseDeleteMessagesEvent);
            this.messagesCv = (CollectionView)CollectionViewSource.GetDefaultView(Messages);
            this.messagesCv.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));

            HandleMailboxSelectionChange(null);
            LoadMessagesFromDb();

            this.eventAggregator = GlobalEventAggregator.Instance().EventAggregator;
            this.eventAggregator.GetEvent<MailboxSelectionEvent>().Subscribe(HandleMailboxSelectionChange);
            this.eventAggregator.GetEvent<MailboxListSyncFinishedEvent>().Subscribe(HandleMailboxListSyncFinished);
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
            BeginSyncMessages(account);
        }

        private void HandleMailboxSelectionChange(Mailbox selectedMailbox)
        {
            this.selectedMailbox = selectedMailbox;
            if (this.selectedMailbox != null)
            {
                this.selectedAccount = AccountManager.Instance().GetAccountByName(selectedMailbox.AccountName);
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

        private void LoadMessagesFromDb()
        {
            List<Message> localMessages = DatabaseManager.GetMessages();
            Messages.Clear();
            Messages.AddRange(localMessages);
        }

        private void BeginSyncMessages(Account account)
        {
            List<Mailbox> mailboxes = DatabaseManager.GetMailboxes(account.AccountName);
            foreach (Mailbox mailbox in mailboxes)
            {
                if (mailbox.DisplayName.ToLower() == "inbox")
                {
                    Task.Run(() => { SyncMessage(account, mailbox); });
                    mailboxes.Remove(mailbox);
                    break;
                }
            }

            SyncMessage(account, mailboxes);
        }

        private async void SyncMessage(Account account, List<Mailbox> mailboxes)
        {
            foreach (Mailbox mailbox in mailboxes)
            {
                await Task.Run(() => { SyncMessage(account, mailbox); });
            }
        }

        private void SyncMessage(Account account, Mailbox mailbox)
        {
            if (mailbox.Flags.Contains(@"\Noselect"))
            {
                return;
            }

            ImapClient imapClient = new ImapClient(account);
            if (!imapClient.Connect())
            {
                Trace.WriteLine(imapClient.Error);
                return;
            }

            ExamineResult status;
            bool readOnly = true;
            if (!imapClient.SelectMailbox(mailbox.DirectoryPath, readOnly, out status))
            {
                Trace.WriteLine(imapClient.Error);
                imapClient.Disconnect();
                return;
            }

            // Download recent messages.
            int localLargestUid = DatabaseManager.GetMaxUid(account.AccountName, mailbox.DirectoryPath);
            int serverLargestUid = status.UidNext - 1;
            if (serverLargestUid > localLargestUid)
            {
                SyncHelper(account, mailbox, imapClient, localLargestUid + 1, serverLargestUid);
            }

            // Download old messages. (Maybe the program shut down previously in the middle of downloading?)
            int localSmallestUid = DatabaseManager.GetMinUid(account.AccountName, mailbox.DirectoryPath);
            if (localSmallestUid > 1)
            {
                SyncHelper(account, mailbox, imapClient, 1, localSmallestUid - 1);
            }

            imapClient.Disconnect();
        }

        private void SyncHelper(Account account, Mailbox mailbox, ImapClient imapClient, int firstUid, int lastUid)
        {
            int messagesCount = lastUid - firstUid + 1;
            int downloadChunk = 30;
            int startUid = lastUid - downloadChunk + 1;
            int endUid = lastUid;

            while (endUid >= firstUid)
            {
                if (startUid < firstUid)
                {
                    startUid = firstUid;
                }
                List<Message> msgs = imapClient.FetchHeaders(startUid, endUid - startUid + 1, true);
                if (msgs.Count > 0)
                {
                    foreach (Message m in msgs)
                    {
                        m.AccountName = account.AccountName;
                        m.MailboxPath = mailbox.DirectoryPath;
                        Application.Current.Dispatcher.Invoke(() => { Messages.Add(m); } );
                    }
                    DatabaseManager.StoreMessages(msgs);
                }

                endUid = startUid - 1;
                startUid = endUid - downloadChunk + 1;
            }
        }

        private void RaiseDeleteMessagesEvent()
        {
            this.eventAggregator.GetEvent<DeleteMessagesEvent>().Publish("Dummy Payload");
        }

        public void DeleteMessages(List<Message> messages)
        {
            // Delete from view.
            foreach (Message msg in messages)
            {
                Messages.Remove(msg);
            }

            // Delete from database.
            DatabaseManager.DeleteMessages(messages);

            // Delete from server.
            Task.Run(() => {
                ImapClient imapClient = new ImapClient(this.selectedAccount);
                if (imapClient.Connect())
                {
                    imapClient.DeleteMessages(messages);
                    imapClient.Disconnect();
                }
            });
        }

    }
}
