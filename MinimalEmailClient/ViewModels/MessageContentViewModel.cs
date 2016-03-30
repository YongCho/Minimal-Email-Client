using HtmlAgilityPack;
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

namespace MinimalEmailClient.ViewModels
{
    public class MessageContentViewModel : BindableBase, IInteractionRequestAware
    {
        private MessageContentsViewNotification notification;
        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is MessageContentsViewNotification)
                {
                    this.notification = value as MessageContentsViewNotification;
                    this.OnPropertyChanged(() => this.Notification);
                    Message = (value as MessageContentsViewNotification).SelectedMessage;
                }
            }
        }
        public Action FinishInteraction { get; set; }

        private Message message;
        public Message Message
        {
            get { return this.message; }
            set
            {
                SetProperty(ref this.message, value);
                if (this.message.Body == string.Empty)
                {
                    DownloadMessageBodyAsync();
                }
                else
                {
                    BrowserContent = PrepareDisplayHtml(this.message.HtmlBody);
                }
            }
        }

        private bool loading;
        public bool Loading
        {
            get { return this.loading; }
            private set { SetProperty(ref this.loading, value); }
        }

        private string browserContent = string.Empty;
        public string BrowserContent
        {
            get { return this.browserContent; }
            private set { SetProperty(ref this.browserContent, value); }
        }

        private string attachmentPath;
        Dictionary<string, string> savedAttachments = new Dictionary<string, string>();

        public MessageContentViewModel()
        {
            this.attachmentPath = Path.Combine(Globals.UserSettingsFolder, "Attachments");
            if (!Directory.Exists(this.attachmentPath))
            {
                Directory.CreateDirectory(this.attachmentPath);
            }
        }

        private string PrepareDisplayHtml(string htmlBody)
        {
            DirectoryInfo di = new DirectoryInfo(this.attachmentPath);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            this.savedAttachments.Clear();
            MimeUtility.SaveAttachments(Message.Body, this.attachmentPath, this.savedAttachments);

            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(htmlBody);

            var root = html.DocumentNode;
            var imgNodes = root.Descendants("img");

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

            return root.InnerHtml;
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
                BrowserContent = PrepareDisplayHtml(this.message.HtmlBody);
            }

            // Store the Body and IsSeen flag to the database.
            DatabaseManager.Update(Message);

            Loading = false;
        }
    }
}
