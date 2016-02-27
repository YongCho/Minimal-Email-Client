using Prism.Events;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.Events
{
    public class NewAccountAddedEvent : PubSubEvent<Account>
    {
    }
}
