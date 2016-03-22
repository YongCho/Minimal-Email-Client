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
        private string to;
        private string cc;
        private string subject;
        private string messageBody;

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
                From.Text = account.EmailAddress;
                smtpServerName = account.SmtpServerName;
                smtpPortNumber = account.SmtpPortNumber;
                smtpLoginName = account.SmtpLoginName;
                smtpLoginPassword = account.SmtpLoginPassword;

                Trace.WriteLine("Smtp Server Name: " + smtpServerName);
                Trace.WriteLine("Smtp Port Number: " + smtpPortNumber);
                Trace.WriteLine("Smtp Login Name: " + smtpLoginName);
            }
        }

        private void SendButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            to = To_TextBox.Text;
            cc = Cc_TextBox.Text;
            subject = Subject_TextBox.Text;
            messageBody = Body_TextBox.Text;
            Trace.WriteLine("Sending...");
        }
    }
}
