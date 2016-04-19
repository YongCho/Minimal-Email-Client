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
                        ToAccounts = this.notification.Recipient;
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

        private List<string> attachments = new List<string>();

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
            if (attachments != null ) StoreEntities(email, attachments);

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
            attachments.Clear();
            NewConnection.Disconnect();
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
                attachments.Add(filePath);
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

        private bool StoreEntities(OutgoingEmail email,List<string> attachmentList)
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
    }
}
