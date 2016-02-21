using Prism.Mvvm;
using System;
using System.ComponentModel;

namespace MinimalEmailClient.Models
{
    class Message : BindableBase
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

        private string recipientName;
        #region public string RecipientName
        public string RecipientName
        {
            get { return this.recipientName; }
            set { SetProperty(ref this.recipientName, value); }
        }
        #endregion

        private string recipientAddress;
        #region public string RecipientAddress
        public string RecipientAddress
        {
            get { return this.recipientAddress; }
            set { SetProperty(ref this.recipientAddress, value); }
        }
        #endregion

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

        public override string ToString()
        {
            return string.Format("Message UID={0}, Subject={1}, Sender={2}<{3}>, Date={4}, New?={5}",
                Uid, Subject, SenderName, SenderAddress, Date.ToString(), IsSeen ? "Yes" : "No");
        }
    }
}
