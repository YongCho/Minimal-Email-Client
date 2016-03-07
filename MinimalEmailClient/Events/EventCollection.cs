using Prism.Events;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.Events
{
    public class NewAccountAddedEvent : PubSubEvent<Account>
    {
    }

    public class AccountDeletedEvent : PubSubEvent<string>
    {
    }

    public class MailboxSelectionEvent : PubSubEvent<Mailbox>
    {
    }

    public class AccountSelectionEvent : PubSubEvent<Account>
    {
    }

    public class MailboxListSyncFinishedEvent : PubSubEvent<Account>
    {
    }
}
