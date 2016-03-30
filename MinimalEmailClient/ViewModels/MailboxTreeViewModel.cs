using Prism.Mvvm;
using System.Collections.Generic;
using System.Diagnostics;
using MinimalEmailClient.Models;
using MinimalEmailClient.Events;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using System.Windows;
using Prism.Events;
using MinimalEmailClient.Services;

namespace MinimalEmailClient.ViewModels
{
    public class MailboxTreeViewModel : BindableBase
    {
        public ObservableCollection<Account> Accounts { get; set; }
        public ICommand DeleteAccountCommand { get; set; }
        private Mailbox currentMailbox;
        public Mailbox CurrentMailbox
        {
            get { return this.currentMailbox; }
            private set
            {
                SetProperty(ref this.currentMailbox, value);
                GlobalEventAggregator.Instance.GetEvent<MailboxSelectionEvent>().Publish(this.currentMailbox);
            }
        }
        private Account currentAccount;
        public Account CurrentAccount
        {
            get { return this.currentAccount; }
            private set
            {
                SetProperty(ref this.currentAccount, value);
                GlobalEventAggregator.Instance.GetEvent<AccountSelectionEvent>().Publish(this.currentAccount);
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

                if (value is Mailbox)
                {
                    Mailbox selectedMailbox = value as Mailbox;
                    if (selectedMailbox != CurrentMailbox)
                    {
                        CurrentMailbox = selectedMailbox;
                    }

                    Account selectedMailboxAccount = AccountManager.Instance.GetAccountByName(selectedMailbox.AccountName);
                    if (selectedMailboxAccount != CurrentAccount)
                    {
                        CurrentAccount = selectedMailboxAccount;
                    }
                }
                else if (value is Account)
                {
                    Account selectedAccount = value as Account;
                    if (selectedAccount != CurrentAccount)
                    {
                        CurrentAccount = selectedAccount;
                        CurrentMailbox = null;
                    }
                }
                else if (value == null)
                {
                    CurrentAccount = null;
                    CurrentMailbox = null;
                }
            }
        }

        public MailboxTreeViewModel()
        {
            GlobalEventAggregator.Instance.GetEvent<NewAccountAddedEvent>().Subscribe(HandleNewAccountAddedEvent, ThreadOption.UIThread);
            GlobalEventAggregator.Instance.GetEvent<AccountDeletedEvent>().Subscribe(HandleAccountDeletedEvent, ThreadOption.UIThread);
            DeleteAccountCommand = new DelegateCommand<Account>(DeleteAccount);

            LoadAccounts();
        }

        private void DeleteAccount(Account ac)
        {
            Task.Run(() => { AccountManager.Instance.DeleteAccount(ac); });
        }

        private void HandleNewAccountAddedEvent(Account newAccount)
        {
            Accounts.Add(newAccount);
            SelectedTreeViewItem = newAccount;
        }

        private void HandleAccountDeletedEvent(string accountName)
        {
            foreach (Account ac in Accounts)
            {
                if (ac.AccountName == accountName)
                {
                    Application.Current.Dispatcher.Invoke(() => { Accounts.Remove(ac); });
                    break;
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
                return AccountManager.Instance.Accounts;
            });
            foreach (Account acc in accounts)
            {
                Accounts.Add(acc);
                AccountManager.Instance.BeginSyncMailboxList(acc);
            }
        }
    }
}
