using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Models
{
    class AccountManager : BindableBase
    {
        public ObservableCollection<Account> Accounts;
        public string Error = string.Empty;
        public readonly int MaxAccountNameLength = 30;

        public AccountManager()
        {
            Accounts = new ObservableCollection<Account>();
            LoadAccounts();
        }

        // Loads all accounts from database.
        public void LoadAccounts()
        {
            DatabaseManager databaseManager = new DatabaseManager();
            List<Account> accounts = databaseManager.GetAccounts();

            Accounts.Clear();
            foreach (Account account in accounts)
            {
                Downloader downloader = new Downloader(account);
                List<Mailbox> mailboxes = downloader.GetMailboxes();
                foreach (Mailbox mailbox in mailboxes)
                {
                    // DisplayName is the directory name without its path string.
                    string pattern = "[^" + mailbox.PathSeparator + "]+$";
                    Regex regx = new Regex(pattern);
                    Match match = regx.Match(mailbox.FullPath);
                    mailbox.MailboxName = match.Value.ToString();

                    // Check if the mailbox a child of another mailbox.
                    // If so, add it to the parent mailbox's Subdirectories.
                    // Otherwise, treat it as a root mailbox and add it directly to the account's mailbox list.
                    // Note: This method of searching the parent mailbox relies on that the
                    // parent mailbox comes before the children mailbox within the 'mailboxes' list.
                    // This is in turn under the assumption that the server will return the parent
                    // mailbox name before its children on the LIST command. If for some reason the server
                    // returns a child mailbox before the parent, the child mailbox will show up in the
                    // root of the tree instead of under the parent.
                    if (mailbox.FullPath.Contains(mailbox.PathSeparator))
                    {
                        // Matches "pp/qq/rr" from "pp/qq/rr/ss".
                        string parentPathPattern = "^(.*)" + mailbox.PathSeparator + "[^" + mailbox.PathSeparator + "]+$";
                        Regex regex = new Regex(parentPathPattern);
                        Match m = regex.Match(mailbox.FullPath);
                        string parentPath = m.Groups[1].ToString();

                        // Find the parent mailbox.
                        Mailbox parent = FindMailboxRecursive(parentPath, mailbox.PathSeparator, account.Mailboxes);
                        if (parent != null)
                        {
                            parent.Subdirectories.Add(mailbox);
                        }
                        else
                        {
                            account.Mailboxes.Add(mailbox);
                        }
                    }
                    else
                    {
                        account.Mailboxes.Add(mailbox);
                    }
                }
                Accounts.Add(account);
            }
        }

        Mailbox FindMailboxRecursive(string path, string separator, List<Mailbox> mailboxes)
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
                if (m.MailboxName.ToLower() == root.ToLower())
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
            bool success = dm.AddAccount(account);
            if (success)
            {
                Accounts.Add(account);
            }
            else
            {
                Error = dm.Error;
            }

            return success;
        }

        public List<Mailbox> GetMailboxes(string accountName)
        {
            List<Mailbox> mailboxes = new List<Mailbox>();

            foreach (Account account in Accounts)
            {
                if (account.AccountName == accountName)
                {
                    Downloader downloader = new Downloader(account);
                    mailboxes = downloader.GetMailboxes();
                    foreach (Mailbox mailbox in mailboxes)
                    {
                        string pattern = "[^" + mailbox.PathSeparator + "]+$";
                        Regex regx = new Regex(pattern);
                        Match match = regx.Match(mailbox.FullPath);
                        mailbox.MailboxName = match.Value.ToString();
                    }
                    break;
                }
            }

            // Update the database with the newly downloaded data.
            DatabaseManager dm = new DatabaseManager();
            dm.UpdateMailboxes(accountName, mailboxes);

            return mailboxes;
        }
    }
}
