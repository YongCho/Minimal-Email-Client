using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using MinimalEmailClient.Models;
using MinimalEmailClient.Services;
using MinimalEmailClient.Notifications;
using Prism.Commands;

namespace MinimalEmailClient.ViewModels
{
    public class CreateMessageViewModel : BindableBase, IInteractionRequestAware
    {
        #region Constructor

        // Initialize a new instance of the CreateMessageViewModel
        public CreateMessageViewModel()
        {
            SendCommand = new DelegateCommand(SendEmail, CanSend);
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
                RaiseCanSendChanged();
            }
        }

        #endregion
        #region SendMailCommand

        public ICommand SendCommand { get; }
        
        public bool CanSend()
        {
            if (FromAccount == null || String.IsNullOrEmpty(ToAccounts) || String.IsNullOrEmpty(Subject) || String.IsNullOrEmpty(MessageBody))
                return false;
            return true;
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
                RaiseCanSendChanged();
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
            }
        }

        #endregion
        #region BccAccounts

        private string bccAccounts = string.Empty;
        public string BccAccounts
        {
            get
            {
                return this.bccAccounts;
            }
            set
            {
                SetProperty(ref this.bccAccounts, value);
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
                RaiseCanSendChanged();
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
                RaiseCanSendChanged();
            }
        }

        #endregion
        #region SendEmail

        public void SendEmail()
        {
            OutgoingEmail email = new OutgoingEmail();
            email.To = ToAccounts;
            email.Cc = CcAccounts;
            email.Bcc = BccAccounts;
            email.Subject = Subject;
            email.Message = MessageBody; 

            SmtpClient NewConnection = new SmtpClient(FromAccount, email);
            if (!NewConnection.Connect())
            {
                Trace.WriteLine(NewConnection.Error);
                MessageBoxResult result = MessageBox.Show(NewConnection.Error);
                return;
            }

            if (!NewConnection.SendMail())
            {
                Trace.WriteLine(NewConnection.Error);
                MessageBoxResult result = MessageBox.Show(NewConnection.Error);
                return;
            }

            FinishInteraction();
            NewConnection.Disconnect();
        }

        private void RaiseCanSendChanged()
        {
            (SendCommand as DelegateCommand).RaiseCanExecuteChanged();
        }

        #endregion

        public Action FinishInteraction { get; set; }
    }
}
