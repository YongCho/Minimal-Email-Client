using Prism.Interactivity.InteractionRequest;

namespace MinimalEmailClient.Models
{
    public class SelectedMessageNotification : Notification
    {
        public Message Message;

        public SelectedMessageNotification(Message message)
        {
            Message = message;
        }
    }
}
