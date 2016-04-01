using Prism.Interactivity.InteractionRequest;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.Notifications
{
    public class MessageContentViewNotification : Notification
    {
        public Account SelectedAccount;
        public Mailbox SelectedMailbox;
        public Message SelectedMessage;

        public MessageContentViewNotification(Account account, Mailbox mailbox, Message message)
        {
            SelectedAccount = account;
            SelectedMailbox = mailbox;
            SelectedMessage = message;
        }
    }
}
