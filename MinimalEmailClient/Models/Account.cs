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
    }
}
