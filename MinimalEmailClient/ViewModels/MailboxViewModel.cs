using System.ComponentModel;
using MinimalEmailClient.Models;
using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace MinimalEmailClient.ViewModels
{
    public class MailboxViewModel : BindableBase
    {
        private Mailbox mailbox;
        public Mailbox Mailbox
        {
            get { return this.mailbox; }
            private set
            {
                if (this.mailbox != value)
                {
                    this.mailbox = value;
                    DisplayName = MakeDisplayName(this.mailbox.MailboxName);
                }
            }
        }

        private string displayName = string.Empty;
        public string DisplayName
        {
            get { return this.displayName; }
            private set { SetProperty(ref this.displayName, value); }
        }

        private bool isExpanded = false;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set
            {
                if (this.isExpanded != value)
                {
                    this.isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }
            }
        }

        public ObservableCollection<MailboxViewModel> MailboxViewModelSubTree { get; set; }

        public MailboxViewModel(Mailbox mailbox)
        {
            Mailbox = mailbox;
            MailboxViewModelSubTree = new ObservableCollection<MailboxViewModel>();

            Mailbox.PropertyChanged += HandleModelPropertyChanged;
        }

        private void HandleModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "MailboxName":
                    DisplayName = MakeDisplayName(Mailbox.MailboxName);
                    break;
            }
        }

        private string MakeDisplayName(string mailboxName)
        {
            string displayName = mailboxName.Trim(' ', '"');
            if (displayName.ToLower() == "inbox")
            {
                return "Inbox";
            }

            return displayName;
        }

    }
}
