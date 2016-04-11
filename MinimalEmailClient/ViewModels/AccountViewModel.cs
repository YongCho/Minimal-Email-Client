#undef TRACE
using MinimalEmailClient.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace MinimalEmailClient.ViewModels
{
    public class AccountViewModel : BindableBase
    {
        private Account account;
        public Account Account
        {
            get { return this.account; }
            private set { SetProperty(ref this.account, value); }
        }

        public string EmailAddress
        {
            get { return Account.EmailAddress; }
        }

        private bool isExpanded = true;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set { SetProperty(ref this.isExpanded, value); }
        }

        public ObservableCollection<MailboxViewModel> MailboxViewModelTree { get; set; }

        public AccountViewModel(Account account)
        {
            if (account == null)
            {
                throw new ArgumentNullException("account", "AccountViewModel constructor was expecting an Account object; received null.");
            }

            Account = account;
            MailboxViewModelTree = ConstructMailboxViewModelTree(Account.Mailboxes.ToList());

            Account.PropertyChanged += HandleModelPropertyChanged;
            Account.Mailboxes.CollectionChanged += HandleMailboxListChanged;
        }

        private void HandleModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "EmailAddress":
                    OnPropertyChanged(e.PropertyName);
                    break;
            }
        }

        private void HandleMailboxListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Trace.WriteLine("HandleMailboxListChanged");

            ObservableCollection <MailboxViewModel> newTree = ConstructMailboxViewModelTree(Account.Mailboxes.ToList());

            Thread.Sleep(5);  // TODO: Investigate why we get an exception sometimes without a delay here.
            Application.Current.Dispatcher.Invoke(() =>
            {
                MailboxViewModelTree.Clear();
                MailboxViewModelTree.AddRange(newTree);
            });
        }

        private ObservableCollection<MailboxViewModel> ConstructMailboxViewModelTree(List<Mailbox> rawMailboxes)
        {
            Trace.WriteLine("ConstructMailboxViewModelTree");
            ObservableCollection<MailboxViewModel> mailboxViewModelTree = new ObservableCollection<MailboxViewModel>();

            foreach (Mailbox mailbox in rawMailboxes)
            {
                // Check if the mailbox is a child of another mailbox.
                // If so, add it to the parent mailbox's subtree.
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
                    MailboxViewModel parent = FindMailboxViewModelRecursive(parentPath, mailbox.PathSeparator, mailboxViewModelTree);
                    if (parent != null)
                    {
                        parent.MailboxViewModelSubTree.Add(new MailboxViewModel(mailbox));
                    }
                    else
                    {
                        // We shouldn't get here unless the server returned the child mailbox before any of its parents.
                        // For now, we are treating this mailbox as one of the root mailboxes.
                        mailboxViewModelTree.Add(new MailboxViewModel(mailbox));
                    }
                }
                else
                {
                    mailboxViewModelTree.Add(new MailboxViewModel(mailbox));
                }
            }

            foreach (MailboxViewModel mailboxVm in mailboxViewModelTree)
            {
                if (mailboxVm.DisplayName.ToLower() == "inbox")
                {
                    mailboxViewModelTree.Move(mailboxViewModelTree.IndexOf(mailboxVm), 0);
                    break;
                }
            }

            return mailboxViewModelTree;
        }

        // Given the path string "pp/qq/rr/ss", finds ss's viewmodel object in the collection.
        private MailboxViewModel FindMailboxViewModelRecursive(string path, string separator, ObservableCollection<MailboxViewModel> mailboxViewModelTree)
        {
            bool hasChild = path.Contains(separator);
            string root = string.Empty;
            string theRest = string.Empty;

            if (hasChild)
            {
                string rootPattern = "^([^" + separator + "]+)" + separator + "(.+)$";
                Regex regex = new Regex(rootPattern);
                Match m = regex.Match(path);
                root = m.Groups[1].ToString();  // "pp" in "pp/qq/rr/ss"
                theRest = m.Groups[2].ToString();  // "qq/rr/ss" in "pp/qq/rr/ss"
            }
            else
            {
                root = path;
            }

            // Check if root directory is in the tree.
            foreach (MailboxViewModel mailboxVm in mailboxViewModelTree)
            {
                // ToLower() is needed because the server sometimes returns the same mailbox name
                // with different capitalization. For example, "INBOX/test1" vs "Inbox/test1/test2".
                if (mailboxVm.Mailbox.MailboxName.ToLower() == root.ToLower().Replace("\"", ""))
                {
                    if (hasChild)
                    {
                        return FindMailboxViewModelRecursive(theRest, separator, mailboxVm.MailboxViewModelSubTree);
                    }
                    else
                    {
                        return mailboxVm;
                    }
                }
            }

            return null;
        }
    }
}
