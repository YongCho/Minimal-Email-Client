using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Models
{
    public class Mailbox : BindableBase
    {
        private string accountName = string.Empty;
        public string AccountName
        {
            get { return this.accountName; }
            set { SetProperty(ref this.accountName, value); }
        }

        private string displayName = string.Empty;
        public string DisplayName
        {
            get { return this.displayName; }
            private set { SetProperty(ref this.displayName, value); }
        }

        private string mailboxName = string.Empty;
        public string MailboxName
        {
            get { return this.mailboxName; }
            private set { SetProperty(ref this.mailboxName, value); }
        }

        private string directoryPath = string.Empty;
        public string DirectoryPath
        {
            get { return this.directoryPath; }
            set
            {
                SetProperty(ref this.directoryPath, value);
                SetMailboxName();
            }
        }

        private string pathSeparator = string.Empty;
        public string PathSeparator
        {
            get { return this.pathSeparator; }
            set
            {
                SetProperty(ref this.pathSeparator, value);
                SetMailboxName();
            }
        }

        private string flagString = string.Empty;
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

        private void SetMailboxName()
        {
            if (PathSeparator == string.Empty || DirectoryPath == string.Empty)
            {
                return;
            }

            string pattern = "[^" + PathSeparator + "]+$";
            Match match = Regex.Match(DirectoryPath, pattern);
            if (match.Success)
            {
                MailboxName = match.Value.ToString().Trim('"');
            }
            else
            {
                MailboxName = DirectoryPath.Trim('"');
            }

            string displayName = MailboxName.Trim(' ');
            if (displayName.ToLower() == "inbox")
            {
                DisplayName = "Inbox";
            }
            else
            {
                DisplayName = displayName;
            }
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
