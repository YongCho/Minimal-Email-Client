using Prism.Interactivity.InteractionRequest;

namespace MinimalEmailClient.Models
{
    public class SelectedMessageNotification : Notification
    {
        public Account SelectedAccount;
        public Mailbox SelectedMailbox;
        public Message SelectedMessage;

        public SelectedMessageNotification(Account account, Mailbox mailbox, Message message)
        {
            SelectedAccount = account;
            SelectedMailbox = mailbox;
            SelectedMessage = message;
        }
    }
}
