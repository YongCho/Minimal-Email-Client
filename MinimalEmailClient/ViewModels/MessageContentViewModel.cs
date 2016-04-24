using HtmlAgilityPack;
using MinimalEmailClient.Common;
using MinimalEmailClient.Models;
using MinimalEmailClient.Notifications;
using MinimalEmailClient.Services;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MinimalEmailClient.ViewModels
{
    public class MessageContentViewModel : BindableBase, IInteractionRequestAware
    {
        #region Model Properties - Subject, Sender, Recipient, Body
        public string Subject
        {
            get { return Message == null ? string.Empty : Message.Subject; }
        }

        public string Sender
        {
            get { return Message == null ? string.Empty : Message.Sender; }
        }

        public string Recipient
        {
            get { return Message == null ? string.Empty : Message.Recipient; }
        }

        public string Body
        {
            get { return Message == null ? string.Empty : Message.Body; }
        }
        #endregion

        #region ViewModel Properties - Date, TextBody, HtmlBody, ProcessedHtmlBody, Loading
        private DateTime date;
        public DateTime Date
        {
            get { return this.date; }
            private set { SetProperty(ref this.date, value); }
        }

        private string textBody = string.Empty;
        public string TextBody
        {
            get { return this.textBody; }
            private set { SetProperty(ref this.textBody, value); }
        }

        private string htmlBody = string.Empty;
        public string HtmlBody
        {
            get { return this.htmlBody; }
            private set { SetProperty(ref this.htmlBody, value); }
        }

        private string processedHtmlBody = string.Empty;
        public string ProcessedHtmlBody
        {
            get { return this.processedHtmlBody; }
            private set { SetProperty(ref this.processedHtmlBody, value); }
        }

        private bool loading;
        public bool Loading
        {
            get { return this.loading; }
            private set { SetProperty(ref this.loading, value); }
        }
        #endregion

        private Message message;
        public Message Message
        {
            get { return this.message; }
            private set
            {
                SetProperty(ref this.message, value);
                RaiseViewModelPropertiesChanged();
            }
        }

        private static readonly string cidContentDirPath = Path.Combine(Globals.UserSettingsFolder, "CidContents");  // embedded binaries
        private static readonly string attachmentDirPath = Path.Combine(Globals.UserSettingsFolder, "Attachments");  // explicit attachments
        private Dictionary<string, string> savedCidContents = new Dictionary<string, string>();
        private Dictionary<string, string> savedAttachments = new Dictionary<string, string>();
        public ObservableCollection<AttachmentViewModel> Attachments { get; set; }
        public InteractionRequest<WriteNewMessageNotification> WriteNewMessagePopupRequest { get; set; }
        public ICommand ReplyMessageCommand { get; set; }
        public ICommand ForwardMessageCommand { get; set; }
        public ICommand HandleUiCloseCommand { get; set; }
        public Action FinishInteraction { get; set; }

        private MessageContentViewNotification notification;
        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                // Setter called when new interaction notification comes in.
                // Replace the Message object with the new one that came with the notification.
                if (value is MessageContentViewNotification)
                {
                    this.notification = value as MessageContentViewNotification;
                    this.OnPropertyChanged(() => this.Notification);

                    Message newMessage = (value as MessageContentViewNotification).SelectedMessage;
                    if (newMessage == null)
                    {
                        throw new ArgumentNullException("SelectedMessage");
                    }

                    if (Message != newMessage)
                    {
                        SetMessage(newMessage);
                    }
                }
            }
        }

        public MessageContentViewModel()
        {
            WriteNewMessagePopupRequest = new InteractionRequest<WriteNewMessageNotification>();
            Attachments = new ObservableCollection<AttachmentViewModel>();
            HandleUiCloseCommand = new DelegateCommand(HandleInteractionFinished);
            ReplyMessageCommand = new DelegateCommand(RaiseReplyMessagePopupRequest);
            ForwardMessageCommand = new DelegateCommand(RaiseForwardMessagePopupRequest);

            if (!Directory.Exists(cidContentDirPath))
            {
                Directory.CreateDirectory(cidContentDirPath);
            }
            if (!Directory.Exists(attachmentDirPath))
            {
                Directory.CreateDirectory(attachmentDirPath);
            }
        }

        private void SetMessage(Message newMessage)
        {
            if (Message != null)
            {
                Message.PropertyChanged -= HandleModelPropertyChanged;
            }

            Message = newMessage;
            Message.PropertyChanged += HandleModelPropertyChanged;

            Date = ImapParser.ParseDate(Message.DateString);

            if (string.IsNullOrEmpty(Message.Body))
            {
                DownloadMessageBodyAsync();
            }
            else
            {
                TextBody = MimeUtility.GetTextBody(Message.Body);
                HtmlBody = MimeUtility.GetHtmlBody(Message.Body);
                ProcessedHtmlBody = ProcessHtmlBody(HtmlBody);
                LoadAttachments(Message.Body);
            }
        }

        private void RaiseReplyMessagePopupRequest()
        {
            Account sendingAccount;
            sendingAccount = AccountManager.Instance.GetAccountByName(Message.AccountName);
            if (sendingAccount == null)
            {
                MessageBoxResult error = MessageBox.Show("No user account selected for sender");
                return;
            }
            WriteNewMessageNotification notification = new WriteNewMessageNotification(sendingAccount, Sender, Subject, TextBody, HtmlBody);
            notification.Title = "RE: " + Subject;
            WriteNewMessagePopupRequest.Raise(notification);
        }

        private void RaiseForwardMessagePopupRequest()
        {
            Account sendingAccount;
            sendingAccount = AccountManager.Instance.GetAccountByName(Message.AccountName);
            if (sendingAccount == null)
            {
                MessageBoxResult error = MessageBox.Show("No user account selected for sender");
                return;
            }
            WriteNewMessageNotification notification = new WriteNewMessageNotification(sendingAccount, Subject, TextBody, HtmlBody);
            notification.Title = "FW: " + Subject;
            WriteNewMessagePopupRequest.Raise(notification);
        }

        private void HandleInteractionFinished()
        {
            Message.PropertyChanged -= HandleModelPropertyChanged;
            Message = null;
            Date = new DateTime();
            TextBody = string.Empty;
            HtmlBody = string.Empty;
            ProcessedHtmlBody = string.Empty;
            RaiseViewModelPropertiesChanged();
            Attachments.Clear();
        }

        private void LoadAttachments(string mimeSource)
        {
            // Clear the files in temporary storage.
            DirectoryInfo di = new DirectoryInfo(attachmentDirPath);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            this.savedAttachments.Clear();
            MimeUtility.SaveAttachments(mimeSource, attachmentDirPath, this.savedAttachments);

            Attachments.Clear();
            foreach (string path in this.savedAttachments.Values)
            {
                Attachments.Add(new AttachmentViewModel(path));
            }
        }

        private string ProcessHtmlBody(string rawHtmlBody)
        {
            // Clear the files in temporary storage.
            DirectoryInfo di = new DirectoryInfo(cidContentDirPath);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            // Save all attachments and obtain their <cid, filePath> pairs.
            this.savedCidContents.Clear();
            MimeUtility.SaveBinariesWithCid(Message.Body, cidContentDirPath, this.savedCidContents);

            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(rawHtmlBody);

            var rootNode = html.DocumentNode;
            var imgNodes = rootNode.Descendants("img");

            // Fix all cid references with corresponding file system paths.
            foreach (HtmlNode node in imgNodes)
            {
                string srcAttr = node.GetAttributeValue("src", null);
                if (srcAttr != null)
                {
                    Match m = Regex.Match(srcAttr, "cid:(.*)");
                    if (m.Success)
                    {
                        string cid = m.Groups[1].Value.Trim(' ');
                        if (this.savedCidContents.ContainsKey(cid))
                        {
                            node.SetAttributeValue("src", this.savedCidContents[cid]);
                        }
                    }
                }
            }

            return rootNode.InnerHtml;
        }

        private async void DownloadMessageBodyAsync()
        {
            Loading = true;

            string msgBody = string.Empty;

            await Task.Run(() => {
                ImapClient imap = new ImapClient(notification.SelectedAccount);
                if (imap.Connect())
                {
                    // Not readonly because we need to set the \Seen flag.
                    bool readOnly = false;
                    if (imap.SelectMailbox(notification.SelectedMailbox.DirectoryPath, readOnly))
                    {
                        msgBody = imap.FetchBody(Message.Uid);
                    }

                    imap.Disconnect();

                    if (!Message.IsSeen)
                    {
                        Message.IsSeen = true;

                        // The server should automatically set the \Seen flag when BODY is fetched.
                        // We shouldn't have to send command for this.
                    }
                }
            });

            if (msgBody != string.Empty)
            {
                Message.Body = msgBody;
            }

            // Store the Body and IsSeen flag to the database.
            DatabaseManager.Update(Message);

            Loading = false;
        }

        private void RaiseViewModelPropertiesChanged()
        {
            OnPropertyChanged("Subject");
            OnPropertyChanged("Sender");
            OnPropertyChanged("Recipient");
            OnPropertyChanged("Date");
            OnPropertyChanged("Body");
        }

        private void HandleModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Subject":
                case "Sender":
                case "Recipient":
                    OnPropertyChanged(e.PropertyName);
                    break;
                case "DateString":
                    Date = ImapParser.ParseDate(Message.DateString);
                    break;
                case "Body":
                    TextBody = MimeUtility.GetTextBody(Message.Body);
                    HtmlBody = MimeUtility.GetHtmlBody(Message.Body);
                    ProcessedHtmlBody = ProcessHtmlBody(HtmlBody);
                    LoadAttachments(Message.Body);
                    OnPropertyChanged(e.PropertyName);
                    break;
            }
        }
    }
}
