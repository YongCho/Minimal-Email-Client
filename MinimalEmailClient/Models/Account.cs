namespace MinimalEmailClient.Models
{
    public class Account
    {
        public string AccountName { get; set; }
        public string UserName { get; set; }
        public string EmailAddress { get; set; }

        public string ImapServerName { get; set; }
        public string ImapLoginName { get; set; }
        public string ImapLoginPassword { get; set; }
        public int ImapPortNumber { get; set; }

        public string SmtpServerName { get; set; }
        public string SmtpLoginName { get; set; }
        public string SmtpLoginPassword { get; set; }
        public int SmtpPortNumber { get; set; }

        public override string ToString()
        {
            string str = string.Format("AccountName: {0}\nUserName: {1}\nEmailAddress: {2}\nImapServerName: {3}\nImapLoginName: {4}\nImapLoginPassword: {5}\nImapPortNumber: {6}\nSmtpServerName: {7}\nSmtpLoginName: {8}\nSmtpLoginPassword: {9}\nSmtpPortNumber: {10}\n", AccountName, UserName, EmailAddress, ImapServerName, ImapLoginName, ImapLoginPassword, ImapPortNumber, SmtpServerName, SmtpLoginName, SmtpLoginPassword, SmtpPortNumber);
            return str;
        }
    }
}
