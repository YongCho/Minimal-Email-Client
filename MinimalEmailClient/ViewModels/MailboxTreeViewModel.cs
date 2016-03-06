using Prism.Mvvm;
using System.Collections.Generic;
using System.Diagnostics;
using MinimalEmailClient.Models;
using MinimalEmailClient.Events;
using System.Collections.ObjectModel;
using Prism.Events;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using System;
using System.Windows;

namespace MinimalEmailClient.ViewModels
{
    public class MailboxTreeViewModel : BindableBase
    {
        public ObservableCollection<Account> Accounts { get; set; }
        private IEventAggregator eventAggregator;
        public ICommand DeleteAccountCommand { get; set; }

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
            this.eventAggregator = GlobalEventAggregator.Instance().EventAggregator;
            this.eventAggregator.GetEvent<NewAccountAddedEvent>().Subscribe(HandleNewAccountAddedEvent);
            this.eventAggregator.GetEvent<AccountDeletedEvent>().Subscribe(HandleAccountDeletedEvent);
            DeleteAccountCommand = new DelegateCommand<Account>(DeleteAccount);

            LoadAccounts();
        }

        private void DeleteAccount(Account ac)
        {
            Task.Run(() => { AccountManager.Instance().DeleteAccount(ac); });
        }

        private void HandleNewAccountAddedEvent(Account newAccount)
        {
            Accounts.Add(newAccount);
        }

        private void HandleAccountDeletedEvent(string accountName)
        {
            foreach (Account ac in Accounts)
            {
                if (ac.AccountName == accountName)
                {
                    Application.Current.Dispatcher.Invoke(() => { Accounts.Remove(ac); });
                }
            }
        }

        public async void LoadAccounts()
        {
            if (Accounts == null)
            {
                Accounts = new ObservableCollection<Account>();
            }
            else
            {
                Accounts.Clear();
            }

            List<Account> accounts = await Task.Run<List<Account>>(() => {
                return AccountManager.Instance().Accounts;
            });
            foreach (Account acc in accounts)
            {
                Accounts.Add(acc);
                AccountManager.Instance().BeginSyncMailboxList(acc);
            }
        }
    }
}
