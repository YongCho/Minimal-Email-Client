using MinimalEmailClient.Models;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
            set { SetProperty(ref this.isExpanded, value); }
        }

        private bool isSelected = false;
        public bool IsSelected
        {
            get { return this.isSelected; }
            set { SetProperty(ref this.isSelected, value); }
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
