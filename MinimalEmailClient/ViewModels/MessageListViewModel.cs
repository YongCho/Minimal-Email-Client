using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MinimalEmailClient.ViewModels
{
    public class MessageListViewModel : BindableBase
    {
        public ObservableCollection<Message> Messages { get; set; }
        public Message SelectedMessage { get; set; }
        private IEventAggregator eventAggregator;

        public MessageListViewModel()
        {
            Messages = new ObservableCollection<Message>();
            this.eventAggregator = GlobalEventAggregator.Instance().EventAggregator;
            this.eventAggregator.GetEvent<MailboxSelectionEvent>().Subscribe(HandleMailboxSelection);
        }

        private void HandleMailboxSelection(Mailbox selectedMailbox)
        {
            Account selectedAccount = AccountManager.Instance().GetAccountByName(selectedMailbox.AccountName);
            Sync(selectedAccount, selectedMailbox.FullPath);
        }

        public async void Sync(Account account, string mailboxPath)
        {
            List<Message> msgs = await Task.Run<List<Message>>(() =>
            {
                Downloader downloader = new Downloader(account);
                downloader.Connect();
                return downloader.GetMsgHeaders(mailboxPath, 1, 15);
            });

            // TODO: Check for download error.
            Messages.Clear();
            foreach (Message m in msgs)
            {
                Messages.Add(m);
            }
        }

    }
}
