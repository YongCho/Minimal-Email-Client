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

            if (imapClient.Connect())
            {
                imapClient.Disconnect();
            }
            else
            {
                Error = imapClient.Error;
                return false;
            }

            return true;
        }
    }
}
