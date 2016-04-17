using Prism.Interactivity.InteractionRequest;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.Notifications
{
    public class WriteNewMessageNotification : Notification
    {
        public Account CurrentAccount;
        public string Recipient = string.Empty;
        public string Subject= string.Empty;

        public WriteNewMessageNotification(Account currentAccount)
        {
            CurrentAccount = currentAccount;
        }

        public WriteNewMessageNotification(Account currentAccount, string recipient, string subject)
        {
            CurrentAccount = currentAccount;
            Recipient = recipient;
            Subject = "re: " + subject;
        }
    }
}
