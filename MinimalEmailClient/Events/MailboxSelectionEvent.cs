using Prism.Events;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.Events
{
    public class MailboxSelectionEvent : PubSubEvent<Mailbox>
    {
    }
}
