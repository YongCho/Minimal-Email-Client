using Prism.Interactivity.InteractionRequest;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.Notifications
{
    public class WriteNewMessageNotification : Notification
    {
        public Account CurrentAccount;

        public WriteNewMessageNotification(Account currentAccount)
        {
            CurrentAccount = currentAccount;
        }
    }
}
