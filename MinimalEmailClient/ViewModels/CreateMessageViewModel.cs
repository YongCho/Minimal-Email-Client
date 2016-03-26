using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Windows.Input;
using MinimalEmailClient.Models;
using MinimalEmailClient.Commands;

namespace MinimalEmailClient.ViewModels
{
    public class CreateMessageViewModel : BindableBase, IInteractionRequestAware
    {
        // Initialize a new instance of the CreateMessageViewModel
        public CreateMessageViewModel()
        {
            SendCommand = new SendMailCommand(this);
        }

        public bool CanSend
        {
            get
            {
                if (CurrentAccount == null)
                    return false;
                return true;
            }
        }

        private WriteNewMessageNotification notification;
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
                        Trace.WriteLine("No user account selected as the sending account.");
                    }
                    else
                    {
                        Trace.WriteLine(this.notification.CurrentAccount);
                        CurrentAccount = this.notification.CurrentAccount;
                    }
                }
            }
        }

        public ICommand SendCommand { get; private set; }

        public Action FinishInteraction { get; set; }

        private Account currentAccount;
        public Account CurrentAccount
        {
            get { return this.currentAccount; }
            set
            {
                SetProperty(ref this.currentAccount, value);
            }
        }

        public void SendEmail()
        {

        }
    }
}
