using Prism.Events;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MinimalEmailClient.Events;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;

namespace MinimalEmailClient.Models
{
    public class AccountManager
    {
        public List<Account> Accounts;
        public string Error = string.Empty;
        public readonly int MaxAccountNameLength = 30;
        private IEventAggregator eventAggregator;

        private static AccountManager instance;
        protected AccountManager()
        {
            this.eventAggregator = GlobalEventAggregator.Instance().EventAggregator;
            LoadAccounts();
        }
        public static AccountManager Instance()
        {
            if (instance == null)
            {
                instance = new AccountManager();
            }
            return instance;
        }

        // Loads all accounts from database.
        public void LoadAccounts()
        {
            DatabaseManager databaseManager = new DatabaseManager();
            Accounts = databaseManager.GetAccounts();
            foreach (Account account in Accounts)
            {
                LoadMailboxListFromDb(account);
            }
        }

        // Populates the mailbox tree of the specified account.
        public void LoadMailboxListFromDb(Account account)
        {
            DatabaseManager dbManager = new DatabaseManager();
            List<Mailbox> localMailboxes = dbManager.GetMailboxes(account.AccountName);
            ConstructMailboxTree(account, localMailboxes);
        }

        // Updates the mailbox tree in the specified account with ones from the server.
        // Also updates the database to reflect the change.
        public void BeginSyncMailboxList(Account account)
        {
            Task.Factory.StartNew(() => {
                DatabaseManager dbManager = new DatabaseManager();
                List<Mailbox> localMailboxes = dbManager.GetMailboxes(account.AccountName);

                ImapClient imapClient = new ImapClient(account);
                if (imapClient.Connect())
                {
                    List<Mailbox> serverMailboxes = imapClient.ListMailboxes();

                    var comparer = new CompareMailbox();
                    var localNotServer = localMailboxes.Except(serverMailboxes, comparer).ToList();
                    var serverNotLocal = serverMailboxes.Except(localMailboxes, comparer).ToList();

                    if (localNotServer.Count > 0 || serverNotLocal.Count > 0)
                    {
                        ConstructMailboxTree(account, serverMailboxes);
                        dbManager.UpdateMailboxes(account.AccountName, serverMailboxes);
                    }

                    imapClient.Disconnect();
                }
            });
        }

        private void ConstructMailboxTree(Account account, List<Mailbox> rawMailboxes)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => { account.Mailboxes.Clear(); }));

            foreach (Mailbox mailbox in rawMailboxes)
            {
                // DisplayName is the directory name without its path string.
                string pattern = "[^" + mailbox.PathSeparator + "]+$";
                Regex regx = new Regex(pattern);
                Match match = regx.Match(mailbox.DirectoryPath);
                mailbox.MailboxName = match.Value.ToString().Replace("\"", "");

                // Check if the mailbox a child of another mailbox.
                // If so, add it to the parent mailbox's Subdirectories.
                // Otherwise, treat it as a root mailbox and add it directly to the account's mailbox list.
                // Note: This method of searching the parent mailbox relies on that the
                // parent mailbox comes before the children mailbox within the 'mailboxes' list.
                // This is in turn under the assumption that the server will return the parent
                // mailbox name before its children on the LIST command. If for some reason the server
                // returns a child mailbox before the parent, the child mailbox will show up in the
                // root of the tree instead of under the parent.
                if (mailbox.DirectoryPath.Contains(mailbox.PathSeparator))
                {
                    // Matches "pp/qq/rr" from "pp/qq/rr/ss".
                    string parentPathPattern = "^(.*)" + mailbox.PathSeparator + "[^" + mailbox.PathSeparator + "]+$";
                    Regex regex = new Regex(parentPathPattern);
                    Match m = regex.Match(mailbox.DirectoryPath);
                    string parentPath = m.Groups[1].ToString();

                    // Find the parent mailbox.
                    Mailbox parent = FindMailboxRecursive(parentPath, mailbox.PathSeparator, account.Mailboxes);
                    if (parent != null)
                    {
                        parent.Subdirectories.Add(mailbox);
                    }
                    else
                    {
                        // We shouldn't get here unless the server returned the child mailbox before any of its parents.
                        // For now, we are treating this mailbox as one of the root mailboxes.
                        Application.Current.Dispatcher.Invoke(new Action(() => { account.Mailboxes.Add(mailbox); }));
                    }
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(new Action(() => { account.Mailboxes.Add(mailbox); }));
                }
            }
        }

        private Mailbox FindMailboxRecursive(string path, string separator, ObservableCollection<Mailbox> mailboxes)
        {
            bool hasChild = path.Contains(separator);
            string root = string.Empty;
            string theRest = string.Empty;

            if (hasChild)
            {
                string rootPattern = "^([^" + separator + "]+)" + separator + "(.+)$";
                Regex regex = new Regex(rootPattern);
                Match m = regex.Match(path);
                root = m.Groups[1].ToString();
                theRest = m.Groups[2].ToString();
            }
            else
            {
                root = path;
            }

            // Check if root directory is in the mailboxes.
            foreach (Mailbox m in mailboxes)
            {
                // ToLower() is needed because the server sometimes returns the same mailbox name
                // with different capitalization. For example, "INBOX/test1" vs "Inbox/test1/test2".
                if (m.MailboxName.ToLower() == root.ToLower().Replace("\"", ""))
                {
                    if (hasChild)
                    {
                        return FindMailboxRecursive(theRest, separator, m.Subdirectories);
                    }
                    else
                    {
                        return m;
                    }
                }
            }

            return null;
        }

        // Returns true if successfully added the account. False, otherwise.
        public bool AddAccount(Account account)
        {
            DatabaseManager dm = new DatabaseManager();
            bool success = dm.InsertAccount(account);
            if (success)
            {
                BeginSyncMailboxList(account);
                Accounts.Add(account);
                this.eventAggregator.GetEvent<NewAccountAddedEvent>().Publish(account);
            }
            else
            {
                Error = dm.Error;
            }

            return success;
        }

        public bool DeleteAccount(Account account)
        {
            DatabaseManager dm = new DatabaseManager();
            bool success = dm.DeleteAccount(account);
            if (!success)
            {
                Error = dm.Error;
                return false;
            }

            Account ac = GetAccountByName(account.AccountName);
            Accounts.Remove(ac);
            this.eventAggregator.GetEvent<AccountDeletedEvent>().Publish(account.AccountName);

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
