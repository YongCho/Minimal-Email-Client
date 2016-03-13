using System.Windows.Controls;
using MinimalEmailClient.ViewModels;
using MinimalEmailClient.Models;
using System.Diagnostics;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for ComposeMailView.xaml
    /// </summary>
    public partial class CreateMessageView : UserControl
    {
        private string smtpServerName;
        private int smtpPortNumber;
        private string smtpLoginName;
        private string smtpLoginPassword;
        
        public CreateMessageView()
        {
            this.DataContext = new CreateMessageViewModel();
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            AccountManager accountManager = AccountManager.Instance();
            if (accountManager.Accounts.Count > 0)
            {
                Account account = accountManager.Accounts[0];
                this.From.Text = account.EmailAddress;
                this.smtpServerName = account.SmtpServerName;
                this.smtpPortNumber = account.SmtpPortNumber;
                this.smtpLoginName = account.SmtpLoginName;
                this.smtpLoginPassword = account.SmtpLoginPassword;

                Trace.WriteLine("Smtp Server Name: " + this.smtpServerName);
                Trace.WriteLine("Smtp Port Number: " + this.smtpPortNumber);
                Trace.WriteLine("Smtp Login Name: " + this.smtpLoginName);
            }
        }

        private void SendButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Trace.WriteLine("Sending...");
        }
    }
}
