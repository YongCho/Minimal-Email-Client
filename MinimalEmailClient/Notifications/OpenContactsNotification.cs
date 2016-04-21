using Prism.Interactivity.InteractionRequest;
using MinimalEmailClient.Models;
using System.Collections.Generic;

namespace MinimalEmailClient.Notifications
{
    public class OpenContactsNotification : Notification
    {
        public List<string> Contacts = new List<string> ();

        public OpenContactsNotification(List<string> contacts)
        {
            Contacts = contacts;
        }
    }
}
