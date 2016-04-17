using Prism.Interactivity.InteractionRequest;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.Notifications
{
    public class WriteNewMessageNotification : Notification
    {
        public Account CurrentAccount;
        public string Recipient = string.Empty;
        public string Subject= string.Empty;
        public string TextBody = string.Empty;
        public string HtmlBody = string.Empty;

        public WriteNewMessageNotification(Account currentAccount)
        {
            CurrentAccount = currentAccount;
        }

        public WriteNewMessageNotification(Account currentAccount, string recipient, string subject, string textBody, string htmlBody)
        {
            CurrentAccount = currentAccount;
            Recipient = recipient;
            Subject = "re: " + subject;
            TextBody = textBody;
            HtmlBody = htmlBody;
        }
    }
}
