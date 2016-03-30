using Prism.Events;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
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
                LoadMailboxListFromDb(account);
            }
        }

        // Populates the mailbox tree of the specified account.
        public void LoadMailboxListFromDb(Account account)
        {
            List<Mailbox> localMailboxes = DatabaseManager.GetMailboxes(account.AccountName);
            ObservableCollection<Mailbox> mailboxTree = ConstructMailboxTree(localMailboxes);
            account.Mailboxes.Clear();
            account.Mailboxes.AddRange(mailboxTree.ToArray());
        }

        // Updates the mailbox tree in the specified account with ones from the server.
        // Also updates the database to reflect the change.
        public void BeginSyncMailboxList(Account account)
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
                        ObservableCollection<Mailbox> mailboxTree = ConstructMailboxTree(serverMailboxes);
                        Application.Current.Dispatcher.Invoke(() => {
                            account.Mailboxes.Clear();
                            foreach (Mailbox mbox in mailboxTree)
                            {
                                account.Mailboxes.Add(mbox);
                            }
                        });

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

                GlobalEventAggregator.Instance.GetEvent<MailboxListSyncFinishedEvent>().Publish(account);
            });
        }

        private ObservableCollection<Mailbox> ConstructMailboxTree(List<Mailbox> rawMailboxes)
        {
            ObservableCollection<Mailbox> mailboxTree = new ObservableCollection<Mailbox>();

            foreach (Mailbox mailbox in rawMailboxes)
            {
                // Check if the mailbox is a child of another mailbox.
                // If so, add it to the parent mailbox's Subdirectories.
                // Otherwise, treat it as a root mailbox and add it directly to the account's mailbox list.
                // Note: This method of searching the parent mailbox relies on that the
                // parent mailbox comes before the children mailbox within the 'rawMailboxes' list.
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
                    Mailbox parent = FindMailboxRecursive(parentPath, mailbox.PathSeparator, mailboxTree);
                    if (parent != null)
                    {
                        parent.Subdirectories.Add(mailbox);
                    }
                    else
                    {
                        // We shouldn't get here unless the server returned the child mailbox before any of its parents.
                        // For now, we are treating this mailbox as one of the root mailboxes.
                        mailboxTree.Add(mailbox);
                    }
                }
                else
                {
                    mailboxTree.Add(mailbox);
                }
            }

            foreach (Mailbox mbox in mailboxTree)
            {
                if (mbox.DisplayName.ToLower() == "inbox")
                {
                    mailboxTree.Move(mailboxTree.IndexOf(mbox), 0);
                    break;
                }
            }

            return mailboxTree;
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
            string error;
            bool success = DatabaseManager.InsertAccount(account, out error);
            if (success)
            {
                BeginSyncMailboxList(account);
                Accounts.Add(account);
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
