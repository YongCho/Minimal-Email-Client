using Prism.Interactivity.InteractionRequest;
using MinimalEmailClient.Models;
using System.Collections.Generic;

namespace MinimalEmailClient.Notifications
{
    public class OpenContactsNotification : Notification
    {
        public string User = string.Empty;

        public OpenContactsNotification(string user)
        {
            User = user;
        }
    }
}
