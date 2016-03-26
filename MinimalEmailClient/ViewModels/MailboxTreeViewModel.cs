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
using System.Windows;

namespace MinimalEmailClient.ViewModels
{
    public class MailboxTreeViewModel : BindableBase
    {
        public ObservableCollection<Account> Accounts { get; set; }
        public ICommand DeleteAccountCommand { get; set; }

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
                    if (!selectedMailbox.Flags.Contains(@"\Noselect"))
                    {
                        GlobalEventAggregator.Instance.GetEvent<MailboxSelectionEvent>().Publish(selectedMailbox);
                    }
                }
                else if (value is Account)
                {
                    Account selectedAccount = value as Account;
                    GlobalEventAggregator.Instance.GetEvent<AccountSelectionEvent>().Publish(selectedAccount);
                }
                else if (value == null)
                {
                    if (Accounts.Count == 0)
                    {
                        GlobalEventAggregator.Instance.GetEvent<AccountSelectionEvent>().Publish(null);
                        GlobalEventAggregator.Instance.GetEvent<MailboxSelectionEvent>().Publish(null);
                    }
                }
            }
        }

        public MailboxTreeViewModel()
        {
            GlobalEventAggregator.Instance.GetEvent<NewAccountAddedEvent>().Subscribe(HandleNewAccountAddedEvent);
            GlobalEventAggregator.Instance.GetEvent<AccountDeletedEvent>().Subscribe(HandleAccountDeletedEvent);
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
