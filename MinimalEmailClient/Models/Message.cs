using Prism.Mvvm;
using System;
using System.ComponentModel;

namespace MinimalEmailClient.Models
{
    public class Message : BindableBase
    {
        private int uid;
        #region public int Uid
        public int Uid
        {
            get { return this.uid; }
            set { SetProperty(ref this.uid, value); }
        }
        #endregion

        private string subject;
        #region public string Subject
        public string Subject
        {
            get { return this.subject; }
            set { SetProperty(ref this.subject, value); }
        }
        #endregion

        private string senderName;
        #region public string SenderName
        public string SenderName
        {
            get { return this.senderName; }
            set { SetProperty(ref this.senderName, value); }
        }
        #endregion

        private string senderAddress;
        #region public string SenderAddress
        public string SenderAddress
        {
            get { return this.senderAddress; }
            set { SetProperty(ref this.senderAddress, value); }
        }
        #endregion

        private string recipient;
        #region public string Recipient
        public string Recipient
        {
            get { return this.recipient; }
            set { SetProperty(ref this.recipient, value); }
        }
        #endregion

        private string dateString;
        public string DateString
        {
            get { return this.dateString; }
            set { SetProperty(ref this.dateString, value); }
        }

        private DateTime date;
        #region public DateTime Date
        public DateTime Date
        {
            get { return this.date; }
            set { SetProperty(ref this.date, value); }
        }
        #endregion

        private bool isSeen;
        #region public bool IsSeen
        public bool IsSeen
        {
            get { return this.isSeen; }
            set { SetProperty(ref this.isSeen, value); }
        }
        #endregion

        private string body;
        public string Body
        {
            get { return this.body; }
            set { SetProperty(ref this.body, value); }
        }

        public override string ToString()
        {
            return string.Format("Message:\nUID={0}, Subject={1}, Sender={2}<{3}>, Recipient={4}, Date={5}, IsSeen?={6}",
                Uid, Subject, SenderName, SenderAddress, Recipient, Date.ToString(), IsSeen ? "Yes" : "No");
        }
    }
}
