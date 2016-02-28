using System.Windows;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for SelectedMessageView.xaml
    /// </summary>
    public partial class SelectedMessageView : UserControl
    {
        public SelectedMessageView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            parentWindow.MinWidth = 800;
            parentWindow.MinHeight = 600;
        }
    }
}
