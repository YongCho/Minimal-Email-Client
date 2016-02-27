using Prism.Commands;
using Prism.Mvvm;
using System.Diagnostics;
using System.Windows.Input;
using MinimalEmailClient.Models;
using System;
using System.Text.RegularExpressions;
using Prism.Interactivity.InteractionRequest;
using Prism.Events;

namespace MinimalEmailClient.ViewModels
{
    public class AddAccountViewModel : BindableBase, IInteractionRequestAware
    {
        #region AccountName
        private string accountName;
        public string AccountName
        {
            get { return this.accountName; }
            set
            {
                SetProperty(ref this.accountName, value);
                AccountNameValidated = !String.IsNullOrWhiteSpace(AccountName);
                HandleInputChange();
            }
        }
        private bool accountNameValidated = false;
        public bool AccountNameValidated
        {
            get { return this.accountNameValidated; }
            set { SetProperty(ref this.accountNameValidated, value); }
        }
        #endregion
        #region UserName
        private string userName;
        public string UserName
        {
            get { return this.userName; }
            set
            {
                SetProperty(ref this.userName, value);
                UserNameValidated = !String.IsNullOrWhiteSpace(UserName);
                HandleInputChange();
            }
        }
        private bool userNameValidated = false;
        public bool UserNameValidated
        {
            get { return this.userNameValidated; }
            set { SetProperty(ref this.userNameValidated, value); }
        }
        #endregion
        #region EmailAddress
        private string emailAddress;
        public string EmailAddress
        {
            get { return this.emailAddress; }
            set
            {
                SetProperty(ref this.emailAddress, value);
                EmailAddressValidated = !String.IsNullOrWhiteSpace(EmailAddress);
                HandleInputChange();
            }
        }
        private bool emailAddressValidated = false;
        public bool EmailAddressValidated
        {
            get { return this.emailAddressValidated; }
            set { SetProperty(ref this.emailAddressValidated, value); }
        }
        #endregion
        #region LoginName
        private string loginName;
        public string LoginName
        {
            get { return this.loginName; }
            set
            {
                SetProperty(ref this.loginName, value);
                LoginNameValidated = !String.IsNullOrWhiteSpace(LoginName);
                HandleInputChange();
            }
        }
        private bool loginNameValidated = false;
        public bool LoginNameValidated
        {
            get { return this.loginNameValidated; }
            set { SetProperty(ref this.loginNameValidated, value); }
        }
        #endregion
        #region LoginPassword
        private string loginPassword;
        public string LoginPassword
        {
            get { return this.loginPassword; }
            set
            {
                SetProperty(ref this.loginPassword, value);
                LoginPasswordValidated = !String.IsNullOrWhiteSpace(LoginPassword);
                HandleInputChange();
            }
        }
        private bool loginPasswordValidated = false;
        public bool LoginPasswordValidated
        {
            get { return this.loginPasswordValidated; }
            set { SetProperty(ref this.loginPasswordValidated, value); }
        }
        #endregion
        #region ImapServerName
        private string imapServerName;
        public string ImapServerName
        {
            get { return this.imapServerName; }
            set
            {
                SetProperty(ref this.imapServerName, value);
                ImapServerNameValidated = !String.IsNullOrWhiteSpace(ImapServerName);
                HandleInputChange();
            }
        }
        private bool imapServerNameValidated = false;
        public bool ImapServerNameValidated
        {
            get { return this.imapServerNameValidated; }
            set { SetProperty(ref this.imapServerNameValidated, value); }
        }
        #endregion
        #region ImapPortString
        private string imapPortString;
        public string ImapPortString
        {
            get { return this.imapPortString; }
            set
            {
                SetProperty(ref this.imapPortString, value);
                ImapPortStringValidated = !String.IsNullOrWhiteSpace(ImapPortString);
                HandleInputChange();
            }
        }
        private bool imapPortStringValidated = false;
        public bool ImapPortStringValidated
        {
            get { return this.imapPortStringValidated; }
            set { SetProperty(ref this.imapPortStringValidated, value); }
        }
        #endregion
        #region SmtpServerName
        private string smtpServerName;
        public string SmtpServerName
        {
            get { return this.smtpServerName; }
            set
            {
                SetProperty(ref this.smtpServerName, value);
                SmtpServerNameValidated = !String.IsNullOrWhiteSpace(SmtpServerName);
                HandleInputChange();
            }
        }
        private bool smtpServerValidated = false;
        public bool SmtpServerNameValidated
        {
            get { return this.smtpServerValidated; }
            set { SetProperty(ref this.smtpServerValidated, value); }
        }
        #endregion
        #region SmtpPortString
        private string smtpPortString;
        public string SmtpPortString
        {
            get { return this.smtpPortString; }
            set
            {
                SetProperty(ref this.smtpPortString, value);
                SmtpPortStringValidated = !String.IsNullOrWhiteSpace(SmtpPortString);
                HandleInputChange();
            }
        }
        private bool smtpPortValidated = false;
        public bool SmtpPortStringValidated
        {
            get { return this.smtpPortValidated; }
            set { SetProperty(ref this.smtpPortValidated, value); }
        }
        #endregion

        private bool accountValidated = false;
        public bool AccountValidated
        {
            get { return this.accountValidated; }
            set { SetProperty(ref this.accountValidated, value); }
        }
        private string message = string.Empty;
        public string Message
        {
            get { return this.message; }
            set { SetProperty(ref this.message, value); }
        }
        private bool isFormComplete = false;
        public bool IsFormComplete
        {
            get { return this.isFormComplete; }
            set { SetProperty(ref this.isFormComplete, value); }
        }

        public ICommand SubmitCommand { get; set; }
        public ICommand ValidateCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public Action FinishInteraction { get; set; }
        private Notification notification;
        public INotification Notification
        {
            get
            {
                return this.notification;
            }

            set
            {
                this.notification = value as Notification;
                OnPropertyChanged(() => Notification);
            }
        }

        private readonly string defaultImapPortString = "993";
        private readonly string defaultSmtpPortString = "587";

        public AddAccountViewModel()
        {
            ValidateCommand = new DelegateCommand(ValidateConnection);
            SubmitCommand = new DelegateCommand(Submit);
            CancelCommand = new DelegateCommand(Cancel);
            ImapPortString = this.defaultImapPortString;
            SmtpPortString = this.defaultSmtpPortString;
        }

        private void Cancel()
        {
            FinishInteraction();
            ResetForm();
        }

        private void Submit()
        {
            if (AccountValidated)
            {
                Account account = new Account();
                account.AccountName = AccountName;
                account.UserName = UserName;
                account.EmailAddress = EmailAddress;
                account.ImapLoginName = LoginName;
                account.ImapLoginPassword = LoginPassword;
                account.ImapServerName = ImapServerName;
                account.ImapPortNumber = Convert.ToInt32(ImapPortString);
                account.SmtpLoginName = LoginName;
                account.SmtpLoginPassword = LoginPassword;
                account.SmtpServerName = SmtpServerName;
                account.SmtpPortNumber = Convert.ToInt32(SmtpPortString);

                AccountManager accountManager = AccountManager.Instance();
                bool success = accountManager.AddAccount(account);
                if (success)
                {
                    FinishInteraction();
                    ResetForm();
                }
                else
                {
                    Message = accountManager.Error;
                }
            }
        }

        private void ValidateConnection()
        {
            int imapPortNumber = -1;
            int smtpPortNumber = -1;
            string numberPattern = @"^\d+$";
            Regex numberChecker = new Regex(numberPattern);
            if (numberChecker.IsMatch(ImapPortString))
            {
                imapPortNumber = Convert.ToInt32(ImapPortString);
                if (imapPortNumber >= 0 && imapPortNumber <= 65536)
                {
                    ImapPortStringValidated = true;
                }
            }

            if (numberChecker.IsMatch(SmtpPortString))
            {
                smtpPortNumber = Convert.ToInt32(SmtpPortString);
                if (smtpPortNumber >= 0 && smtpPortNumber <= 65536)
                {
                    SmtpPortStringValidated = true;
                }
            }

            if (IsFormComplete)
            {
                AccountValidator validator = new AccountValidator();
                validator.ImapServerName = ImapServerName;
                validator.ImapPort = imapPortNumber;
                validator.ImapLoginName = LoginName;
                validator.ImapPassword = LoginPassword;
                validator.SmtpServerName = SmtpServerName;
                validator.SmtpPort = smtpPortNumber;
                validator.SmtpLoginName = LoginName;
                validator.SmtpLoginPassword = LoginPassword;

                if (validator.Validate())
                {
                    AccountValidated = true;
                    Message = "Validated";
                }
                else
                {
                    AccountValidated = false;
                    Message = validator.Errors[0];
                }
            }
        }

        private void HandleInputChange()
        {
            AccountValidated = false;
            Message = string.Empty;

            if ((bool)AccountNameValidated &&
                (bool)UserNameValidated &&
                (bool)EmailAddressValidated &&
                (bool)LoginNameValidated &&
                (bool)LoginPasswordValidated &&
                (bool)ImapServerNameValidated &&
                (bool)SmtpServerNameValidated &&
                (bool)ImapPortStringValidated &&
                (bool)SmtpPortStringValidated)
            {
                IsFormComplete = true;
            }
            else
            {
                IsFormComplete = false;
            }
        }

        private void ResetForm()
        {
            AccountName = string.Empty;
            UserName = string.Empty;
            EmailAddress = string.Empty;
            LoginName = string.Empty;
            LoginPassword = string.Empty;
            ImapServerName = string.Empty;
            ImapPortString = this.defaultImapPortString;
            SmtpServerName = string.Empty;
            SmtpPortString = this.defaultSmtpPortString;
            Message = string.Empty;
        }
    }
}
