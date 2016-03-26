using Prism.Events;

namespace MinimalEmailClient.Events
{
    // I'm using this class to create a singleton instance of an IEventAggregator throughout the program.
    // There is probably a better way to do this.
    public static class GlobalEventAggregator
    {
        private static IEventAggregator instance = new EventAggregator();
        public static IEventAggregator Instance
        {
            get { return instance; }
            set
            {
                if (instance != value)
                {
                    instance = value;
                }
            }
        }
    }
}
