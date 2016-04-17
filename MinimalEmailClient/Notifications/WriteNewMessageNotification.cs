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
    }
}
