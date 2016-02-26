using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MinimalEmailClient.Models
{
    public class Account : BindableBase
    {
        private string accountName;
        public string AccountName
        {
            get { return this.accountName; }
            set { SetProperty(ref this.accountName, value); }
        }
        private string userName;
        public string UserName
        {
            get { return this.userName; }
            set { SetProperty(ref this.userName, value); }
        }
        private string emailAddress;
        public string EmailAddress
        {
            get { return this.emailAddress; }
            set { SetProperty(ref this.emailAddress, value); }
        }

        private string imapServerName;
        public string ImapServerName
        {
            get { return this.imapServerName; }
            set { SetProperty(ref this.imapServerName, value); }
        }
        private string imapLoginName;
        public string ImapLoginName
        {
            get { return this.imapLoginName; }
            set { SetProperty(ref this.imapLoginName, value); }
        }
        private string imapLoginPassword;
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

        private string smtpServerName;
        public string SmtpServerName
        {
            get { return this.smtpServerName; }
            set { SetProperty(ref this.smtpServerName, value); }
        }
        private string smtpLoginName;
        public string SmtpLoginName
        {
            get { return this.smtpLoginName; }
            set { SetProperty(ref this.smtpLoginName, value); }
        }
        private string smtpLoginPassword;
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

        public List<Mailbox> Mailboxes { get; set; }

        public Account()
        {
            Mailboxes = new List<Mailbox>();
        }

        public override string ToString()
        {
            string str = string.Format("AccountName: {0}\nUserName: {1}\nEmailAddress: {2}\nImapServerName: {3}\nImapLoginName: {4}\nImapLoginPassword: {5}\nImapPortNumber: {6}\nSmtpServerName: {7}\nSmtpLoginName: {8}\nSmtpLoginPassword: {9}\nSmtpPortNumber: {10}\n", AccountName, UserName, EmailAddress, ImapServerName, ImapLoginName, ImapLoginPassword, ImapPortNumber, SmtpServerName, SmtpLoginName, SmtpLoginPassword, SmtpPortNumber);
            return str;
        }
    }
}
