namespace MinimalEmailClient.Models
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
            bool success = false;
            Error = string.Empty;

            if (imapClient.Connect())
            {
                success = true;
            }
            else
            {
                Error = imapClient.Error;
            }
            imapClient.Disconnect();

            return success;
        }
    }
}
