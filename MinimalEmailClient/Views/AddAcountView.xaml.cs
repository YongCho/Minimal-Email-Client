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
            int fixecWidth = 320;
            parentWindow.MinWidth = fixecWidth;
            parentWindow.MaxWidth = fixecWidth;
            MessagePanel.Width = fixecWidth - 20;
        }
    }
}
