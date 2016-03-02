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

namespace MinimalEmailClient.ViewModels
{
    public class MessageListViewModel : BindableBase
    {
        public ObservableCollection<Message> Messages { get; set; }
        private Message selectedMessage;  // This could be null
        public Message SelectedMessage
        {
            get { return this.selectedMessage; }
            set { SetProperty(ref this.selectedMessage, value); }
        }
        private IEventAggregator eventAggregator;
        public InteractionRequest<SelectedMessageNotification> OpenSelectedMessagePopupRequest { get; set; }
        public ICommand OpenSelectedMessageCommand { get; set; }

        public MessageListViewModel()
        {
            Messages = new ObservableCollection<Message>();

            CollectionView cv = (CollectionView)CollectionViewSource.GetDefaultView(Messages);
            cv.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));

            OpenSelectedMessagePopupRequest = new InteractionRequest<SelectedMessageNotification>();
            OpenSelectedMessageCommand = new DelegateCommand(RaiseOpenSelectedMessagePopupRequest);

            this.eventAggregator = GlobalEventAggregator.Instance().EventAggregator;
            this.eventAggregator.GetEvent<MailboxSelectionEvent>().Subscribe(HandleMailboxSelection);
        }

        private void RaiseOpenSelectedMessagePopupRequest()
        {
            if (SelectedMessage != null)
            {
                SelectedMessageNotification notification = new SelectedMessageNotification(SelectedMessage);
                notification.Title = SelectedMessage.Subject;
                OpenSelectedMessagePopupRequest.Raise(notification);
            }
        }

        private void HandleMailboxSelection(Mailbox selectedMailbox)
        {
            Account selectedAccount = AccountManager.Instance().GetAccountByName(selectedMailbox.AccountName);
            UpdateListView(selectedAccount, selectedMailbox.DirectoryPath);
        }

        public async void UpdateListView(Account account, string mailboxPath)
        {
            ImapClient imapClient = new ImapClient(account);
            if (!imapClient.Connect())
            {
                return;
            }
            Messages.Clear();

            MailboxStatus status;
            if (imapClient.ExamineMailbox(mailboxPath, out status))
            {
                int messagesCount = status.Exists;

                if (messagesCount < 1)
                {
                    return;
                }

                int downloadChunk = 10;
                int startSeq = messagesCount - downloadChunk + 1;
                int endSeq = messagesCount;
                while (endSeq > 0)
                {
                    List<Message> msgs = await Task.Run<List<Message>>(() =>
                    {
                        // Message sequence number starts from 1.
                        if (startSeq < 1)
                        {
                            startSeq = 1;
                        }
                        return imapClient.FetchHeaders(startSeq, endSeq - startSeq + 1);
                    });
                    if (msgs.Count > 0)
                    {
                        foreach (Message m in msgs)
                        {
                            Messages.Add(m);
                        }
                    }
                    endSeq = startSeq - 1;
                    startSeq = endSeq - downloadChunk + 1;
                }
            }

            imapClient.Disconnect();
        }

    }
}
