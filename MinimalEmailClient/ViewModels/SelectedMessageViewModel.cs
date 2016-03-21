using System;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.ViewModels
{
    public class SelectedMessageViewModel : BindableBase, IInteractionRequestAware
    {
        private SelectedMessageNotification notification;
        private Message message;
        public Message Message
        {
            get { return this.message; }
            set
            {
                SetProperty(ref this.message, value);
                if (this.message.Body == string.Empty)
                {
                    GetMsgBody();
                }
            }
        }
        public Action FinishInteraction { get; set; }
        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is SelectedMessageNotification)
                {
                    this.notification = value as SelectedMessageNotification;
                    this.OnPropertyChanged(() => this.Notification);
                    Message = (value as SelectedMessageNotification).SelectedMessage;
                }
            }
        }

        private void GetMsgBody()
        {
            ImapClient imap = new ImapClient(notification.SelectedAccount);
            if (imap.Connect())
            {
                // Not readonly because we need to set the \Seen flag.
                bool readOnly = false;
                if (imap.SelectMailbox(notification.SelectedMailbox.DirectoryPath, readOnly))
                {
                    this.message.Body = imap.FetchBody(Message.Uid);
                }

                imap.Disconnect();

                if (!Message.IsSeen)
                {
                    Message.IsSeen = true;

                    // The server should automatically set the \Seen flag when BODY is fetched.
                    // We shouldn't have to send command for this.
                }

                // Store the Body and IsSeen flag to the database.
                DatabaseManager.Update(Message);
            }
        }
    }
}
