using MinimalEmailClient.Models;
using MinimalEmailClient.Services;
using Prism.Mvvm;
using System;
using System.ComponentModel;

namespace MinimalEmailClient.ViewModels
{
    public class MessageHeaderViewModel : BindableBase
    {
        private Message message;
        public Message Message
        {
            get { return this.message; }
            private set { SetProperty(ref this.message, value); }
        }

        public string AccountName
        {
            get { return Message.AccountName; }
        }

        public string MailboxPath
        {
            get { return Message.MailboxPath; }
        }

        public string Subject
        {
            get { return Message.Subject; }
        }

        public string SenderAddress
        {
            get { return Message.SenderAddress; }
        }

        public string SenderName
        {
            get { return Message.SenderName; }
        }

        public bool IsSeen
        {
            get { return Message.IsSeen; }
        }

        private DateTime date;
        public DateTime Date
        {
            get { return this.date; }
            private set { SetProperty(ref this.date, value); }
        }

        public MessageHeaderViewModel(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message", "MessageHeaderViewModel constructor was expecting a Message object; received null.");
            }

            Message = message;
            Date = ImapParser.ParseDate(Message.DateString);
            Message.PropertyChanged += HandleModelPropertyChanged;
        }

        private void HandleModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "AccountName":
                case "MailboxPath":
                case "Subject":
                case "SenderAddress":
                case "SenderName":
                case "IsSeen":
                    OnPropertyChanged(e.PropertyName);
                    break;
                case "DateString":
                    Date = ImapParser.ParseDate(Message.DateString);
                    break;
            }
        }
    }
}
