using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Globalization;

namespace MinimalEmailClient.Models
{
    public class Mailbox : BindableBase
    {
        private string accountName;
        public string AccountName
        {
            get { return this.accountName; }
            set { SetProperty(ref this.accountName, value); }
        }

        private string displayName;
        public string DisplayName
        {
            get { return this.displayName; }
            set
            {
                if (value.ToLower() == "inbox")
                {
                    SetProperty(ref this.displayName, "Inbox");
                }
                else
                {
                    SetProperty(ref this.displayName, value);
                }
            }
        }

        private string mailboxName;
        public string MailboxName
        {
            get { return this.mailboxName; }
            set
            {
                SetProperty(ref this.mailboxName, value);
                DisplayName = this.mailboxName.Trim(new char[] { '"', ' ' });
            }
        }

        private string directoryPath;
        public string DirectoryPath
        {
            get { return this.directoryPath; }
            set { SetProperty(ref this.directoryPath, value); }
        }

        private string pathSeparator;
        public string PathSeparator
        {
            get { return this.pathSeparator; }
            set { SetProperty(ref this.pathSeparator, value); }
        }

        private string flagString;
        public string FlagString
        {
            get { return this.flagString; }
            set
            {
                SetProperty(ref this.flagString, value);
                string[] flags = this.flagString.Split(' ');
                Flags.Clear();
                Flags.AddRange(flags);
            }
        }

        private int uidNext;
        public int UidNext
        {
            get { return this.uidNext; }
            set { SetProperty(ref this.uidNext, value); }
        }

        private int uidValidity;
        public int UidValidity
        {
            get { return this.uidValidity; }
            set { SetProperty(ref this.uidValidity, value); }
        }

        public List<string> Flags { get; set; }
        public ObservableCollection<Mailbox> Subdirectories { get; set; }

        private bool isExpanded = false;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set { SetProperty(ref this.isExpanded, value); }
        }

        public Mailbox()
        {
            Flags = new List<string>();
            Subdirectories = new ObservableCollection<Mailbox>();
        }

        public override string ToString()
        {
            string str = string.Format("AccountName: {0}\nMailboxName: {1}\nDirectoryPath: {2}\nPathSeparator: {3}\nUidNext: {4}\nUidValidity: {5}\nFlags: ", AccountName, MailboxName, DirectoryPath, PathSeparator, UidNext, UidValidity);
            foreach (string flag in Flags)
            {
                str += flag;
                str += " ";
            }
            str += "\n";

            return str;
        }
    }

    public class CompareMailbox : IEqualityComparer<Mailbox>
    {
        public bool Equals(Mailbox x, Mailbox y)
        {
            if (x == null || y == null)
            {
                return false;
            }
            return (x.AccountName == y.AccountName) && (x.DirectoryPath == y.DirectoryPath) && (x.FlagString == y.FlagString) && (x.PathSeparator == y.PathSeparator);
        }

        public int GetHashCode(Mailbox obj)
        {
            return obj.AccountName.GetHashCode() ^ obj.DirectoryPath.GetHashCode() ^ obj.FlagString.GetHashCode() ^ obj.PathSeparator.GetHashCode();
        }
    }
}
