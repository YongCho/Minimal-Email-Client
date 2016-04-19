using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalEmailClient.Services
{
    public class AccountManager
    {
        public List<Account> Accounts;
        public string Error = string.Empty;
        public readonly int MaxAccountNameLength = 30;

        private static readonly AccountManager instance = new AccountManager();
        public static AccountManager Instance
        {
            get { return instance; }
        }

        protected AccountManager()
        {
            LoadAccounts();
        }

        // Loads all accounts from database.
        public void LoadAccounts()
        {
            Accounts = DatabaseManager.GetAccounts();
            foreach (Account account in Accounts)
            {
                var mailboxes = DatabaseManager.GetMailboxes(account.AccountName);
                account.Mailboxes.AddRange(mailboxes);
                BeginSyncMailboxList(account);
            }
        }

        // Updates the mailbox tree in the specified account with ones from the server.
        // Also updates the database to reflect the change.
        private void BeginSyncMailboxList(Account account)
        {
            Task.Factory.StartNew(() => {
                List<Mailbox> localMailboxes = DatabaseManager.GetMailboxes(account.AccountName);

                ImapClient imapClient = new ImapClient(account);
                if (imapClient.Connect())
                {
                    List<Mailbox> serverMailboxes = imapClient.ListMailboxes();

                    var comparer = new CompareMailbox();
                    var localNotServer = localMailboxes.Except(serverMailboxes, comparer).ToList();
                    var serverNotLocal = serverMailboxes.Except(localMailboxes, comparer).ToList();

                    if (localNotServer.Count > 0 || serverNotLocal.Count > 0)
                    {
                        account.Mailboxes.Clear();
                        account.Mailboxes.AddRange(serverMailboxes);

                        if (localNotServer.Count > 0)
                        {
                            DatabaseManager.DeleteMailboxes(localNotServer);
                        }
                        if (serverNotLocal.Count > 0)
                        {
                            DatabaseManager.InsertMailboxes(serverNotLocal);
                        }
                    }

                    imapClient.Disconnect();
                }

                while (!Common.Globals.BootStrapperLoaded)
                {
                    // We don't want to fire this event before the subscribers are ready.
                    // There must be a better way to handle these sort of things.
                    Thread.Sleep(50);
                }
                GlobalEventAggregator.Instance.GetEvent<MailboxListSyncFinishedEvent>().Publish(account);
            });
        }

        // Returns true if successfully added the account. False, otherwise.
        public bool AddAccount(Account account)
        {
            string error;
            bool success = DatabaseManager.InsertAccount(account, out error);
            if (success)
            {
                Accounts.Add(account);
                BeginSyncMailboxList(account);
                GlobalEventAggregator.Instance.GetEvent<NewAccountAddedEvent>().Publish(account);
            }
            else
            {
                Error = error;
            }

            return success;
        }

        public bool DeleteAccount(Account account)
        {
            string error;
            bool success = DatabaseManager.DeleteAccount(account, out error);
            if (!success)
            {
                Error = error;
                return false;
            }

            Accounts.Remove(account);
            GlobalEventAggregator.Instance.GetEvent<AccountDeletedEvent>().Publish(account.AccountName);

            return true;
        }

        public Account GetAccountByName(string accountName)
        {
            foreach (Account account in Accounts)
            {
                if (account.AccountName == accountName)
                {
                    return account;
                }
            }

            return null;
        }
    }
}
