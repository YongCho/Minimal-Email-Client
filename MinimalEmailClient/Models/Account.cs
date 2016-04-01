using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace MinimalEmailClient.Models
{
    public class Account : BindableBase
    {
        private string accountName = string.Empty;
        public string AccountName
        {
            get { return this.accountName; }
            set { SetProperty(ref this.accountName, value); }
        }
        private string emailAddress = string.Empty;
        public string EmailAddress
        {
            get { return this.emailAddress; }
            set { SetProperty(ref this.emailAddress, value); }
        }

        private string imapServerName = string.Empty;
        public string ImapServerName
        {
            get { return this.imapServerName; }
            set { SetProperty(ref this.imapServerName, value); }
        }
        private string imapLoginName = string.Empty;
        public string ImapLoginName
        {
            get { return this.imapLoginName; }
            set { SetProperty(ref this.imapLoginName, value); }
        }
        private string imapLoginPassword = string.Empty;
        public string ImapLoginPassword
        {
            get { return this.imapLoginPassword; }
            set { SetProperty(ref this.imapLoginPassword, value); }
        }
        private int imapPortNumber;
        public int ImapPortNumber
        {
            get { return this.imapPortNumber; }
            set { SetProperty(ref this.imapPortNumber, value); }
        }

        private string smtpServerName = string.Empty;
        public string SmtpServerName
        {
            get { return this.smtpServerName; }
            set { SetProperty(ref this.smtpServerName, value); }
        }
        private string smtpLoginName = string.Empty;
        public string SmtpLoginName
        {
            get { return this.smtpLoginName; }
            set { SetProperty(ref this.smtpLoginName, value); }
        }
        private string smtpLoginPassword = string.Empty;
        public string SmtpLoginPassword
        {
            get { return this.smtpLoginPassword; }
            set { SetProperty(ref this.smtpLoginPassword, value); }
        }
        private int smtpPortNumber;
        public int SmtpPortNumber
        {
            get { return this.smtpPortNumber; }
            set { SetProperty(ref this.smtpPortNumber, value); }
        }

        public ObservableCollection<Mailbox> Mailboxes { get; set; }

        public Account()
        {
            Mailboxes = new ObservableCollection<Mailbox>();
        }

        public override string ToString()
        {
            return
                "Account:\n" +
                "AccountName: " + AccountName + "\n" +
                "EmailAddress: " + EmailAddress + "\n" +
                "ImapServerName: " + ImapServerName + "\n" +
                "ImapLoginName: " + ImapLoginName + "\n" +
                "ImapPortNumber: " + ImapPortNumber + "\n" +
                "SmtpServerName: " + SmtpServerName + "\n" +
                "SmtpLoginName: " + SmtpLoginName + "\n" +
                "SmtpPortNumber: " + SmtpPortNumber + "\n";
        }
    }
}
