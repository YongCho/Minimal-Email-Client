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

        private Account _fromAccount;
        public Account FromAccount
        {
            get
            {
                return _fromAccount;
            }
            private set
            {
                SetProperty(ref this._fromAccount, value);
            }
        }
        private WriteNewMessageNotification notification;
        public ICommand SendCommand { get; }

        // Gets or sets a System.Boolean value indicating whether the Email can be sent.
        public bool CanSend
        {
            get
            {
                if (FromAccount == null)
                    return false;
                return true;
            }
        }

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
                        FromAccount = this.notification.CurrentAccount;
                    }
                }
            }
        }

        public Action FinishInteraction { get; set; }

        public void SendEmail()
        {
            Trace.WriteLine("Sending from server: " + FromAccount.SmtpServerName);
        }
    }
}
