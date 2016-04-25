using Prism.Interactivity.InteractionRequest;
using MinimalEmailClient.Models;
using System.Collections.Generic;

namespace MinimalEmailClient.Notifications
{
    public class WriteNewMessageNotification : Notification
    {
        public Account CurrentAccount;
        public string Recipient = string.Empty;
        public string Subject= string.Empty;
        public string TextBody = string.Empty;
        public string HtmlBody = string.Empty;
        public Dictionary<string, string> SavedAttachments = new Dictionary<string, string>();

        public WriteNewMessageNotification(Account currentAccount)
        {
            CurrentAccount = currentAccount;
        }

        public WriteNewMessageNotification(Account currentAccount, string recipient, string subject, string textBody, string htmlBody, Dictionary<string, string> savedAttachments)
        {
            CurrentAccount = currentAccount;
            Recipient = recipient;
            Subject = "RE: " + subject;
            TextBody = textBody;
            HtmlBody = htmlBody;
            SavedAttachments = savedAttachments;
        }

        public WriteNewMessageNotification(Account currentAccount, string subject, string textBody, string htmlBody, Dictionary<string, string> savedAttachments)
        {
            CurrentAccount = currentAccount;
            Subject = "FW: " + subject;
            TextBody = textBody;
            HtmlBody = htmlBody;
            SavedAttachments = savedAttachments;
        }
    }
}
