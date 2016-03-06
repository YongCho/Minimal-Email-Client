using Prism.Interactivity.InteractionRequest;

namespace MinimalEmailClient.Models
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
