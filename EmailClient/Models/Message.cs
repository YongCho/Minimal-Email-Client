using System;
using System.ComponentModel;

namespace EmailClientPrototype2.Models
{
    class Message : CommonBase
    {
        private int uid;
        #region public int Uid
        public int Uid
        {
            get { return this.uid; }
            set
            {
                if (this.uid != value)
                {
                    this.uid = value;
                    RaisePropertyChanged("Uid");
                }
            }
        }
        #endregion

        private string subject;
        #region public string Subject
        public string Subject
        {
            get { return this.subject; }
            set
            {
                if (this.subject != value)
                {
                    this.subject = value;
                    RaisePropertyChanged("Subject");
                }
            }
        }
        #endregion

        private string senderName;
        #region public string SenderName
        public string SenderName
        {
            get { return this.senderName; }
            set
            {
                if (this.senderName != value)
                {
                    this.senderName = value;
                    RaisePropertyChanged("SenderName");
                }
            }
        }
        #endregion

        private string senderAddress;
        #region public string SenderAddress
        public string SenderAddress
        {
            get { return this.senderAddress; }
            set
            {
                if (this.senderAddress != value)
                {
                    this.senderAddress = value;
                    RaisePropertyChanged("SenderAddress");
                }
            }
        }
        #endregion

        private string recipientName;
        #region public string RecipientName
        public string RecipientName
        {
            get { return this.recipientName; }
            set
            {
                if (this.recipientName != value)
                {
                    this.recipientName = value;
                    RaisePropertyChanged("RecipientName");
                }
            }
        }
        #endregion

        private string recipientAddress;
        #region public string RecipientAddress
        public string RecipientAddress
        {
            get { return this.recipientAddress; }
            set
            {
                if (this.recipientAddress != value)
                {
                    this.recipientAddress = value;
                    RaisePropertyChanged("RecipientAddress");
                }
            }
        }
        #endregion

        private DateTime date;
        #region public DateTime Date
        public DateTime Date
        {
            get { return this.date; }
            set
            {
                if (this.date != value)
                {
                    this.date = value;
                    RaisePropertyChanged("Date");
                }
            }
        }
        #endregion

        private bool isSeen;
        #region public bool IsSeen
        public bool IsSeen
        {
            get { return this.isSeen; }
            set
            {
                if (this.isSeen != value)
                {
                    this.isSeen = value;
                    RaisePropertyChanged("IsSeen");
                }
            }
        }
        #endregion
        
        public override string ToString()
        {
            return string.Format("Message UID={0}, Subject={1}, Sender={2}<{3}>, Date={4}, New?={5}",
                Uid, Subject, SenderName, SenderAddress, Date.ToString(), IsSeen ? "Yes" : "No");
        }
    }
}
