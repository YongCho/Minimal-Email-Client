using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using MinimalEmailClient.Models;
using MinimalEmailClient.Services;
using MinimalEmailClient.Notifications;
using Prism.Commands;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.IO;
using MimeKit;
using MimeKit.IO;
using MimeKit.IO.Filters;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MinimalEmailClient.ViewModels
{
    public class NewEmailViewModel : BindableBase, IInteractionRequestAware
    {
        #region Constructor

        // Initialize a new instance of the MultiPartViewModel
        public NewEmailViewModel()
        {
            SendCommand = new DelegateCommand(SendEmail, CanSend);
            AttachFileCommand = new DelegateCommand(AttachFile);
            Attachments = new ObservableCollection<AttachmentViewModel>();
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
        #region AttachFileCommand

        public ICommand AttachFileCommand { get; set; }

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
                    if (!String.IsNullOrEmpty(this.notification.Recipient))
                    {
                        ToAccounts = ExtractSender(this.notification.Recipient); ;
                    }
                    if (!String.IsNullOrEmpty(this.notification.Subject))
                    {
                        Subject = this.notification.Subject;
                    }
                    if (!String.IsNullOrEmpty(this.notification.HtmlBody))
                    {
                        isHtml = true;
                        HtmlBody = this.notification.HtmlBody;
                    } else if (!String.IsNullOrEmpty(this.notification.TextBody))
                    {
                        MessageBody = "\n--------------------------------------------------------------------------------\n";
                        MessageBody += this.notification.TextBody;
                    }

                }
            }
        }

        #endregion
        #region Headers
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

        private string htmlBody = string.Empty;
        public string HtmlBody
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

        private bool isHtml = false;

        #endregion
            #endregion
        #region Attachments

        public ObservableCollection<AttachmentViewModel> Attachments { get; set; }

        #endregion
        #region SendEmailMethod

        public void SendEmail()
        {
            OutgoingEmail email = new OutgoingEmail();
            email.To = ExtractRecipients(toAccounts);
            if (!String.IsNullOrEmpty(CcAccounts)) email.Cc = ExtractRecipients(CcAccounts);
            if (!String.IsNullOrEmpty(BccAccounts)) email.Bcc = ExtractRecipients(BccAccounts);
            email.Subject = Subject;
            email.Message = MessageBody;
            if (Attachments != null)
            {
                List<string> attachmentFilePaths = new List<string>();
                foreach (AttachmentViewModel attachmentVm in Attachments)
                {
                    attachmentFilePaths.Add(attachmentVm.FilePath);
                }
                StoreEntities(email, attachmentFilePaths);
            }

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

            SaveFavorites(email);
            FinishInteraction();
            Attachments.Clear();
            NewConnection.Disconnect();
        }

        private void SaveFavorites(OutgoingEmail email)
        {
            List<string> allOutgoingAddresses = new List<string>();
            foreach (string to in email.To)
            {
                allOutgoingAddresses.Add(to);
            }
            if (email.Cc != null)
            {
                foreach (string cc in email.Cc)
                {
                    allOutgoingAddresses.Add(cc);
                }
            }
            if (email.Bcc != null)
            {
                foreach (string bcc in email.Bcc)
                {
                    allOutgoingAddresses.Add(bcc);
                }
            }
            foreach (string receiver in allOutgoingAddresses)
            {
                if (!FromAccount.Favorites.Contains(receiver))
                {
                    FromAccount.Favorites.Add(receiver);
                    Trace.WriteLine("Adding " + receiver + " to address book.");
                }
            }
        }

        private void RaiseCanSendChanged()
        {
            (SendCommand as DelegateCommand).RaiseCanExecuteChanged();
        }

        #endregion
        #region AttachFile

        private void AttachFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                Attachments.Add(new AttachmentViewModel(filePath));
            }
        }

        #endregion
        #region ExtractRecipients

        List<string> ExtractRecipients(string unparsedRecipients)
        {
            Regex whiteSpace = new Regex(@"\s+");
            char[] delimiterChars = { ' ', ',', ';' };
            List<string> recipients = new List<String> ();

            // begin extraction of recipients from header contents
            var parsedRecipients = whiteSpace.Replace(unparsedRecipients, "");
            string[] splitRecipients = parsedRecipients.Split(delimiterChars);
            foreach (string recipient in splitRecipients)
            {
                recipients.Add(recipient);
            }

            return recipients;
        }

        #endregion
        #region MimeEntities

        private bool StoreEntities(OutgoingEmail email, List<string> attachmentList)
        {
            foreach (string iter in attachmentList)
            {
                FileStream stream = File.OpenRead(iter);
                if (!stream.CanRead)
                {
                    return false;
                }

                string mimeType = MimeTypes.GetMimeType(iter);
                ContentType fileType = ContentType.Parse(mimeType);
                MimePart attachment;
                if (fileType.IsMimeType("text", "*"))
                {
                    attachment = new TextPart(fileType.MediaSubtype);
                    foreach (var param in fileType.Parameters)
                        attachment.ContentType.Parameters.Add(param);
                }
                else
                {
                    attachment = new MimePart(fileType);
                }
                attachment.FileName = Path.GetFileName(iter);
                attachment.IsAttachment = true;

                MemoryBlockStream memoryBlockStream = new MemoryBlockStream();
                BestEncodingFilter encodingFilter = new BestEncodingFilter();
                byte[] fileBuffer = new byte[4096];
                int index, length, bytesRead;

                while ((bytesRead = stream.Read(fileBuffer, 0, fileBuffer.Length)) > 0)
                {
                    encodingFilter.Filter(fileBuffer, 0, bytesRead, out index, out length);
                    memoryBlockStream.Write(fileBuffer, 0, bytesRead);
                }

                encodingFilter.Flush(fileBuffer, 0, 0, out index, out length);
                memoryBlockStream.Position = 0;

                attachment.ContentTransferEncoding = encodingFilter.GetBestEncoding(EncodingConstraint.SevenBit);
                attachment.ContentObject = new ContentObject(memoryBlockStream);

                if (attachment != null) email.AttachmentList.Add(attachment);
            }
            return true;
        }

        static ContentType GetMimeType(string filePath)
        {
            string mimeType = MimeTypes.GetMimeType(filePath);
            return ContentType.Parse(mimeType);
        }

        #endregion
        public Action FinishInteraction { get; set; }
        bool invalid = false;

        string ExtractSender(string rawSender)
        {
            char[] delimiterChars = { '<', '>' };
            string[] parsedSender = rawSender.Split(delimiterChars);
            foreach (string s in parsedSender)
            {
                if (IsValidEmail(s))
                    return s;
            }
            Trace.WriteLine("sender email address cannot be found..");
            return null;
        }

        public bool IsValidEmail(string strIn)
        {
            invalid = false;
            if (String.IsNullOrEmpty(strIn))
                return false;

            // Use IdnMapping class to convert Unicode domain names.
            try
            {
                strIn = Regex.Replace(strIn, @"(@)(.+)$", this.DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            if (invalid)
                return false;

            // Return true if strIn is in valid e-mail format.
            try
            {
                return Regex.IsMatch(strIn,
                      @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                      RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private string DomainMapper(Match match)
        {
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;
            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                invalid = true;
            }
            return match.Groups[1].Value + domainName;
        }

    }
}
