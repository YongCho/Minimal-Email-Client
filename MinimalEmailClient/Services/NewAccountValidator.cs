using MinimalEmailClient.Models;

namespace MinimalEmailClient.Services
{
    public class NewAccountValidator
    {
        private Account account;
        public Account Account
        {
            get { return this.account; }
            set
            {
                if (this.account != value)
                {
                    this.account = value;
                    Error = string.Empty;
                }
            }
        }

        public string Error;

        public NewAccountValidator(Account account)
        {
            Account = account;
        }

        public bool Validate()
        {
            ImapClient imapClient = new ImapClient(Account);
            Error = string.Empty;

            int tryCount = 2;
            if (imapClient.Connect(tryCount))
            {
                imapClient.Disconnect();
            }
            else
            {
                Error = imapClient.Error;
                return false;
            }

            SmtpClient smtpClient = new SmtpClient(Account);

            if (smtpClient.Connect())
            {
                smtpClient.Disconnect();
            }
            else
            {
                Error = smtpClient.Error;
                return false;
            }

            return true;
        }
    }
}
