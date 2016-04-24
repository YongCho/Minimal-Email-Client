using MinimalEmailClient.ViewModels;
using MinimalEmailClient.Events;
using System.Windows;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for OpenContactsView.xaml
    /// </summary>
    public partial class OpenContactsView : System.Windows.Controls.UserControl
    {
        public OpenContactsView()
        {
            InitializeComponent();
            this.DataContext = new OpenContactsViewModel();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalEventAggregator.Instance.GetEvent<AddressBookClosedEvent>().Publish("Address Book has finished interaction.");
        }
    }
}
