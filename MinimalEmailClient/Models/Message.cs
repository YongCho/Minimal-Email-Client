using Prism.Mvvm;
using System;
using System.ComponentModel;

namespace MinimalEmailClient.Models
{
    public class Message : BindableBase
    {
        public string AccountName = string.Empty;
        public string MailboxPath = string.Empty;

        private int uid = 0;
        public int Uid
        {
            get { return this.uid; }
            set { SetProperty(ref this.uid, value); }
        }

        private string subject = string.Empty;
        public string Subject
        {
            get { return this.subject; }
            set { SetProperty(ref this.subject, value); }
        }

        private string senderName = string.Empty;
        public string SenderName
        {
            get { return this.senderName; }
            set { SetProperty(ref this.senderName, value); }
        }

        private string senderAddress = string.Empty;
        public string SenderAddress
        {
            get { return this.senderAddress; }
            set { SetProperty(ref this.senderAddress, value); }
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
                Date = ResponseParser.ParseDate(this.dateString);
            }
        }

        private DateTime date;
        public DateTime Date
        {
            get { return this.date; }
            private set { SetProperty(ref this.date, value); }
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

        public override string ToString()
        {
            return string.Format("Message:\nUID={0}, Subject={1}, Sender={2}<{3}>, Recipient={4}, Date={5}, FlagsString={6}, IsSeen?={7}",
                Uid, Subject, SenderName, SenderAddress, Recipient, Date.ToString(), FlagString, IsSeen ? "Yes" : "No");
        }
    }
}
