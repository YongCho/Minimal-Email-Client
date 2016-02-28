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
            set { SetProperty(ref this.message, value); }
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
                    Message = (value as SelectedMessageNotification).Message;
                }
            }
        }
    }
}
