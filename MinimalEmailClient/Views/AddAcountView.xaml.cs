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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            int fixecWidth = 320;
            parentWindow.MinWidth = fixecWidth;
            parentWindow.MaxWidth = fixecWidth;
            MessagePanel.Width = fixecWidth - 20;

            ResetForm();
        }

        private void ResetForm()
        {
            emailAddressTextBox.Clear();
            loginNameTextBox.Clear();
            loginPasswordBox.Clear();
            imapServerNameTextBox.Clear();
            smtpServerNameTextBox.Clear();
            messageTextBlock.Text = string.Empty;
        }
    }
}