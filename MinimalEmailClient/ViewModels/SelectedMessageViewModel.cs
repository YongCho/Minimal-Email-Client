using System;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using MinimalEmailClient.Models;
using System.Diagnostics;

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
                SyncMsgBody();
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

        private void SyncMsgBody()
        {
            Debug.WriteLine(Message);
            Debug.WriteLine(notification.SelectedAccount);

            ImapClient imap = new ImapClient(notification.SelectedAccount);
            if (imap.Connect())
            {
                if (imap.ExamineMailbox(notification.SelectedMailbox.DirectoryPath))
                {
                    Message.Body = imap.FetchBody(Message.Uid);
                }
                imap.Disconnect();
            }

        }
    }
}
