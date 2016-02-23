using System.Windows;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    public partial class AddAcountView : UserControl
    {
        public AddAcountView()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            parentWindow.Close();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            parentWindow.MinWidth = 400;
            parentWindow.MaxWidth = 400;
            MessagePanel.Width = 380;
        }
    }
}
