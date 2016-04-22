using MinimalEmailClient.ViewModels;

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
    }
}
