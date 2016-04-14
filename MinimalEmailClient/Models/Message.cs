using Prism.Mvvm;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Models
{
    public class Message : BindableBase
    {
        private string accountName = string.Empty;
        public string AccountName
        {
            get { return this.accountName; }
            set
            {
                SetProperty(ref this.accountName, value);
                UniqueKeyString = CreateUniqueKeyString();
            }
        }

        private string mailboxPath = string.Empty;
        public string MailboxPath
        {
            get { return this.mailboxPath; }
            set
            {
                SetProperty(ref this.mailboxPath, value);
                UniqueKeyString = CreateUniqueKeyString();
            }
        }

        private int uid = 0;
        public int Uid
        {
            get { return this.uid; }
            set
            {
                SetProperty(ref this.uid, value);
                UniqueKeyString = CreateUniqueKeyString();
            }
        }

        private string subject = string.Empty;
        public string Subject
        {
            get { return this.subject; }
            set { SetProperty(ref this.subject, value); }
        }

        private string sender = string.Empty;
        public string Sender
        {
            get { return this.sender; }
            set
            {
                SetProperty(ref this.sender, value);
                SetSenderNameAndAddress(this.sender);
            }
        }

        private string senderName = string.Empty;
        public string SenderName
        {
            get { return this.senderName; }
            private set { SetProperty(ref this.senderName, value); }
        }

        private string senderAddress = string.Empty;
        public string SenderAddress
        {
            get { return this.senderAddress; }
            private set { SetProperty(ref this.senderAddress, value); }
        }

        private string recipient = string.Empty;
        public string Recipient
        {
            get { return this.recipient; }
            set { SetProperty(ref this.recipient, value); }
        }

        private string dateString = string.Empty;
        public string DateString
        {
            get { return this.dateString; }
            set
            {
                SetProperty(ref this.dateString, value);
            }
        }

        private string flagString = string.Empty;
        public string FlagString
        {
            get { return this.flagString; }
            set
            {
                SetProperty(ref this.flagString, value.Trim(' '));
                if (this.flagString.Contains(@"\Seen") && !IsSeen)
                {
                    IsSeen = true;
                }
                else if(!this.flagString.Contains(@"\Seen") && IsSeen)
                {
                    IsSeen = false;
                }
            }
        }

        private bool isSeen = false;
        public bool IsSeen
        {
            get { return this.isSeen; }
            set
            {
                SetProperty(ref this.isSeen, value);
                if (this.isSeen && !FlagString.Contains(@"\Seen"))
                {
                    FlagString += @" \Seen";
                }
                else if (!this.isSeen && FlagString.Contains(@"\Seen"))
                {
                    FlagString = FlagString.Replace(@"\Seen", "").Trim(' ');
                }
            }
        }

        private string body = string.Empty;
        public string Body
        {
            get { return this.body; }
            set { SetProperty(ref this.body, value); }
        }

        public string UniqueKeyString { get; private set; }

        private void SetSenderNameAndAddress(string sender)
        {
            string name = string.Empty;
            string address = string.Empty;
            string senderPattern = "(?<name>[^<>]*)(<(?<address>[^<>]*)>)?";
            Match m = Regex.Match(sender, senderPattern);
            if (m.Success)
            {
                name = m.Groups["name"].ToString().Trim(' ', '"');
                address = m.Groups["address"].ToString().Trim(' ', '"');
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = address;
                }
                else if (name.Contains("@") && string.IsNullOrWhiteSpace(address))
                {
                    address = name;
                }
            }
            else
            {
                name = sender;
            }

            SenderName = name.Trim('"', ' ');
            SenderAddress = address.Trim('"', ' ');
        }

        private string CreateUniqueKeyString()
        {
            if (string.IsNullOrEmpty(AccountName) || string.IsNullOrEmpty(MailboxPath) || Uid == 0)
            {
                return string.Empty;
            }

            return CreateUniqueKeyString(AccountName, MailboxPath, Uid);
        }

        public static string CreateUniqueKeyString(string accountName, string mailboxPath, int uid)
        {
            return accountName + ";" + mailboxPath + ";" + uid;
        }

        public override string ToString()
        {
            return
                "Message:\n" +
                "AccountName: " + AccountName + "\n" +
                "MailboxPath: " + MailboxPath + "\n" +
                "UID: " + Uid + "\n" +
                "Subject: " + Subject + "\n" +
                "Sender: " + Sender + "\n" +
                "Recipient: " + (Recipient.Length > 80 ? Recipient.Substring(0, 76) + " ..." : Recipient) + "\n" +
                "Date: " + DateString + "\n" +
                "FlagsString: " + FlagString + "\n" +
                "IsSeen?: " + (IsSeen ? "Yes" : "No") + "\n";
        }
    }
}
