using Prism.Events;

namespace MinimalEmailClient.Events
{
    // I'm using this class to create a singleton instance of an IEventAggregator throughout the program.
    // There is probably a better way to do this.
    public class GlobalEventAggregator
    {
        public IEventAggregator EventAggregator { get; set; }

        private static GlobalEventAggregator instance;
        protected GlobalEventAggregator() { EventAggregator = new EventAggregator(); }
        public static GlobalEventAggregator Instance()
        {
            if (instance == null)
            {
                instance = new GlobalEventAggregator();
            }

            return instance;
        }
    }
}
