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
        #region Constructor

        // Initialize a new instance of the CreateMessageViewModel
        public CreateMessageViewModel()
        {
            SendCommand = new SendMailCommand(this);
        }

        #endregion
        #region Accounts

        private Account fromAccount;
        public Account FromAccount
        {
            get
            {
                return this.fromAccount;
            }
            private set
            {
                SetProperty(ref this.fromAccount, value);
            }
        }

        #endregion
        #region SendMailCommand

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

        #endregion
        #region INotification

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
                        FromAccount = this.notification.CurrentAccount;
                    }
                }
            }
        }

        #endregion
        #region ToAccounts

        private string toAccounts = string.Empty;
        public string ToAccounts
        {
            get { return this.toAccounts; }
            set
            {
                SetProperty(ref this.toAccounts, value);
                Trace.WriteLine("ToAccounts: " + ToAccounts);
            }
        }

        #endregion
        #region CcAccounts
        private string ccAccounts = string.Empty;
        public string CcAccounts
        {
            get
            {
                return this.ccAccounts;
            }
            set
            {
                SetProperty(ref this.ccAccounts, value);
                Trace.WriteLine("CcAccounts: " + CcAccounts);
            }
        }
        #endregion
        #region Subject

        private string subject = string.Empty;
        public string Subject
        {
            get
            {
                return this.subject;
            }
            set
            {
                SetProperty(ref this.subject, value);
                Trace.WriteLine("Subject: " + Subject);
            }
        }

        #endregion
        #region MessageBody

        private string messageBody = string.Empty;
        public string MessageBody
        {
            get
            {
                return this.messageBody;
            }
            set
            {
                SetProperty(ref this.messageBody, value);
                Trace.WriteLine("MessageBody: " + MessageBody);
            }
        }

        #endregion

        public void SendEmail()
        {

            Trace.WriteLine("Sending from server: " + FromAccount.SmtpServerName);
        }

        public Action FinishInteraction { get; set; }
    }
}
