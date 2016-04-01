﻿using HtmlAgilityPack;
using MinimalEmailClient.Common;
using MinimalEmailClient.Models;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MinimalEmailClient.Services;
using MinimalEmailClient.Notifications;
using System.ComponentModel;

namespace MinimalEmailClient.ViewModels
{
    public class MessageContentViewModel : BindableBase, IInteractionRequestAware
    {
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
                        if (Message != null)
                        {
                            Message.PropertyChanged -= HandleModelPropertyChanged;
                        }

                        Message = newMessage;
                        Date = ImapParser.ParseDate(Message.DateString);

                        if (string.IsNullOrEmpty(Message.Body))
                        {
                            DownloadMessageBodyAsync();
                        }
                        else
                        {
                            TextBody = MimeUtility.GetTextBody(Message.Body);
                            HtmlBody = MimeUtility.GetHtmlBody(Message.Body);
                            BrowserContent = PrepareBrowserContent(HtmlBody);
                        }

                        Message.PropertyChanged += HandleModelPropertyChanged;
                    }
                }
            }
        }

        public Action FinishInteraction { get; set; }

        private Message message;
        public Message Message
        {
            get { return this.message; }
            private set
            {
                if (this.message != value)
                {
                    this.message = value;
                    RaiseViewModelPropertiesChanged();
                }
            }
        }

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

        private DateTime date;
        public DateTime Date
        {
            get { return this.date; }
            private set { SetProperty(ref this.date, value); }
        }

        private bool loading;
        public bool Loading
        {
            get { return this.loading; }
            private set { SetProperty(ref this.loading, value); }
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

        private string browserContent = string.Empty;
        public string BrowserContent
        {
            get { return this.browserContent; }
            private set { SetProperty(ref this.browserContent, value); }
        }

        private static readonly string attachmentDirPath = Path.Combine(Globals.UserSettingsFolder, "Attachments");
        Dictionary<string, string> savedAttachments = new Dictionary<string, string>();

        public MessageContentViewModel()
        {
            if (!Directory.Exists(attachmentDirPath))
            {
                Directory.CreateDirectory(attachmentDirPath);
            }
        }

        private string PrepareBrowserContent(string htmlBody)
        {
            // Clear the files in temporary attachment storage.
            DirectoryInfo di = new DirectoryInfo(attachmentDirPath);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            // Save all attachments and obtain their <cid, filePath> pairs.
            this.savedAttachments.Clear();
            MimeUtility.SaveAttachments(Message.Body, attachmentDirPath, this.savedAttachments);

            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(htmlBody);

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
                        if (this.savedAttachments.ContainsKey(cid))
                        {
                            node.SetAttributeValue("src", this.savedAttachments[cid]);
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
                    BrowserContent = PrepareBrowserContent(HtmlBody);
                    OnPropertyChanged(e.PropertyName);
                    break;
            }
        }
    }
}
