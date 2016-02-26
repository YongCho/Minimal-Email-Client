using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
            set { SetProperty(ref this.displayName, value); }
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
            string str = string.Format("AccountName: {0}\nDisplayName: {1}\nFullPath: {2}\nPathSeparator: {3}\nAttributes: ", AccountName, DisplayName, FullPath, PathSeparator);
            foreach (string attribute in Attributes)
            {
                str += attribute;
                str += " ";
            }
            str += "\n";

            return str;
        }
    }
}
