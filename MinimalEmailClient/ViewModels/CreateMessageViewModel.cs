using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using MinimalEmailClient.Models;
using System;
using System.Diagnostics;

namespace MinimalEmailClient.ViewModels
{
    public class CreateMessageViewModel : BindableBase, IInteractionRequestAware
    {
        private WriteNewMessageNotification notification;
        public Action FinishInteraction { get; set; }
        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is WriteNewMessageNotification)
                {
                    this.notification = value as WriteNewMessageNotification;
                    this.OnPropertyChanged(() => this.Notification);
                    if (this.notification.CurrentAccount == null)
                    {
                        Debug.WriteLine("No user account selected as the sending account.");

                    }
                    else
                    {
                        Debug.WriteLine(this.notification.CurrentAccount);
                    }
                }
            }
        }
    }
}
