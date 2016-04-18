using MinimalEmailClient.ViewModels;
using System.Windows;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for ComposeMailView.xaml
    /// </summary>
    public partial class NewEmailView : System.Windows.Controls.UserControl
    {
        public NewEmailView()
        {
            InitializeComponent();
            this.DataContext = new NewEmailViewModel();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            To_TextBox.Clear();
            Cc_TextBox.Clear();
            Bcc_TextBox.Clear();
            Subject_TextBox.Clear();
            Body_TextBox.Clear();
        }
    }
}
