using Prism.Interactivity.InteractionRequest;

namespace MinimalEmailClient.Models
{
    public class WriteNewMessageNotification : Notification
    {
        // Account info to be used as the sender.
        public string SomeAccountInfo;

        public WriteNewMessageNotification(string someAccountInfo)
        {
            SomeAccountInfo = someAccountInfo;
        }
    }
}
