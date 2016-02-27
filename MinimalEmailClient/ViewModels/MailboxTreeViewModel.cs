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
                if (!value.Attributes.Contains(@"\Noselect"))
                {
                    this.eventAggregator.GetEvent<MailboxSelectionEvent>().Publish(this.selectedMailbox);
                }
            }
        }

        // This could be a Mailbox or an Account.
        private object selectedTreeViewItem;
        public object SelectedTreeViewItem
        {
            get { return this.selectedTreeViewItem; }
            set
            {
                SetProperty(ref this.selectedTreeViewItem, value);
                var selectedObject = value as Mailbox;
                if (selectedObject != null)
                {
                    SelectedMailbox = selectedObject;
                }
            }
        }

        public MailboxTreeViewModel()
        {
            Accounts = AccountManager.Instance().Accounts;
            this.eventAggregator = GlobalEventAggregator.Instance().EventAggregator;
            this.eventAggregator.GetEvent<NewAccountAddedEvent>().Subscribe(HandleNewAccountAddedEvent);
        }

        private void HandleNewAccountAddedEvent(Account newAccount)
        {
            Accounts = AccountManager.Instance().Accounts;
        }
    }
}
