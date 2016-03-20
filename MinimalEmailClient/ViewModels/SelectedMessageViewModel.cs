using System;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using MinimalEmailClient.Models;
using System.Diagnostics;
using NI.Email.Mime.Message;
using NI.Email.Mime.Field;
using NI.Email.Mime.Decoder;
using System.IO;
using System.Text;

namespace MinimalEmailClient.ViewModels
{
    public class SelectedMessageViewModel : BindableBase, IInteractionRequestAware
    {
        private SelectedMessageNotification notification;
        private Message message;
        public Message Message
        {
            get { return this.message; }
            set
            {
                SetProperty(ref this.message, value);
                SyncMsgBody();
            }
        }
        public Action FinishInteraction { get; set; }
        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is SelectedMessageNotification)
                {
                    this.notification = value as SelectedMessageNotification;
                    this.OnPropertyChanged(() => this.Notification);
                    Message = (value as SelectedMessageNotification).SelectedMessage;
                }
            }
        }

        private void SyncMsgBody()
        {
            ImapClient imap = new ImapClient(notification.SelectedAccount);
            if (imap.Connect())
            {
                bool readOnly = true;
                if (imap.SelectMailbox(notification.SelectedMailbox.DirectoryPath, readOnly))
                {
                    string rawBody = imap.FetchBody(Message.Uid);
                    Stream mimeMsgStream = new MemoryStream(Encoding.ASCII.GetBytes(rawBody));
                    MimeMessage mimeMsg = new MimeMessage(mimeMsgStream);
                    string plainText = ParseFromMime(mimeMsg, "text/plain");
                    string htmlText = ParseFromMime(mimeMsg, "text/html");
                    if (!string.IsNullOrEmpty(plainText))
                    {
                        Message.Body = plainText;
                    }
                    else if (!string.IsNullOrEmpty(htmlText))
                    {
                        Message.Body = htmlText;
                    }
                    else
                    {
                        Message.Body = rawBody;
                    }
                }
                imap.Disconnect();
            }
        }

        // Parses the content of the specified mime type.
        // Recognized mime types are "text/html" and "text/plain".
        private string ParseFromMime(Entity mimeEntity, string mimeType)
        {
            string parsedText = string.Empty;
            if (mimeEntity.IsMultipart)
            {
                Multipart multiPart = (Multipart)mimeEntity.Body;
                foreach (Entity part in multiPart.BodyParts)
                {
                    ContentTypeField contentType = part.Header.GetField(MimeField.ContentType) as ContentTypeField;
                    if (contentType == null)
                    {
                        continue;
                    }
                    if (part.Body is ITextBody && contentType.MimeType.Contains(mimeType))
                    {
                        parsedText = ParseTextBody(part);
                    }
                    else if (part.IsMultipart)
                    {
                        parsedText = ParseFromMime((Entity)part, mimeType);
                    }
                    else if (part.Body is MimeMessage)
                    {
                        parsedText = ParseFromMime((MimeMessage)part.Body, mimeType);
                    }
                }
            }
            else
            {
                ContentTypeField contentType = mimeEntity.Header.GetField(MimeField.ContentType) as ContentTypeField;
                if (mimeEntity.Body is ITextBody && contentType.MimeType.Contains(mimeType))
                {
                    parsedText = ParseTextBody(mimeEntity);
                }
            }

            return parsedText;
        }

        // Extracts the content out of ITextBody.
        private string ParseTextBody(Entity mimeBody)
        {
            if (!(mimeBody.Body is ITextBody))
            {
                return string.Empty;
            }

            ITextBody textBody = (ITextBody)mimeBody.Body;
            MemoryStream memStream = new MemoryStream();
            textBody.WriteTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);
            string encoding = mimeBody.ContentTransferEncoding.ToLower();
            Trace.WriteLine("ContentTransferEncoding: " + encoding);
            byte[] buffer = new byte[memStream.Length];
            int bytesRead;
            if (encoding == "quoted-printable")
            {
                QuotedPrintableInputStream qpStream = new QuotedPrintableInputStream(memStream);
                bytesRead = qpStream.Read(buffer, 0, buffer.Length);
            }
            else if (encoding == "base64" || encoding == "base-64")
            {
                Base64InputStream b64Stream = new Base64InputStream(memStream);
                bytesRead = b64Stream.Read(buffer, 0, buffer.Length);
            }
            else
            {
                bytesRead = memStream.Read(buffer, 0, buffer.Length);
            }

            return mimeBody.CurrentEncoding.GetString(buffer, 0, bytesRead);
        }
    }
}
