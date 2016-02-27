using Prism.Mvvm;
using System.Collections.Generic;
using System.Diagnostics;
using MinimalEmailClient.Models;
using MinimalEmailClient.Events;
using System.Collections.ObjectModel;
using Prism.Events;

namespace MinimalEmailClient.ViewModels
{
    public class MailboxTreeViewModel : BindableBase
    {
        public ObservableCollection<Account> Accounts { get; set; }
        private IEventAggregator eventAggregator;

        private Mailbox selectedMailbox;
        public Mailbox SelectedMailbox
        {
            get { return this.selectedMailbox; }
            set
            {
                SetProperty(ref this.selectedMailbox, value);
                this.eventAggregator.GetEvent<MailboxSelectionEvent>().Publish(this.selectedMailbox);
            }
        }

        public MailboxTreeViewModel(IEventAggregator eventAggregator)
        {
            AccountManager am = new AccountManager();
            Accounts = am.Accounts;

            this.eventAggregator = eventAggregator;
        }
    }
}
