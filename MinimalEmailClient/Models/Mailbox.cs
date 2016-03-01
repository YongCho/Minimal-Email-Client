using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

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

        private string mailboxName;
        public string MailboxName
        {
            get { return this.mailboxName; }
            set { SetProperty(ref this.mailboxName, value); }
        }

        private string fullPath;
        public string FullPath
        {
            get { return this.fullPath; }
            set { SetProperty(ref this.fullPath, value); }
        }

        private string pathSeparator;
        public string PathSeparator
        {
            get { return this.pathSeparator; }
            set { SetProperty(ref this.pathSeparator, value); }
        }

        public List<string> Attributes { get; set; }
        public ObservableCollection<Mailbox> Subdirectories { get; set; }

        public Mailbox()
        {
            Attributes = new List<string>();
            Subdirectories = new ObservableCollection<Mailbox>();
        }

        public override string ToString()
        {
            string str = string.Format("AccountName: {0}\nDisplayName: {1}\nFullPath: {2}\nPathSeparator: {3}\nAttributes: ", AccountName, MailboxName, FullPath, PathSeparator);
            foreach (string attribute in Attributes)
            {
                str += attribute;
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
            return (x.AccountName == y.AccountName) && (x.FullPath == y.FullPath);
        }

        public int GetHashCode(Mailbox obj)
        {
            return obj.AccountName.GetHashCode() ^ obj.FullPath.GetHashCode();
        }
    }
}
