using System.Windows.Controls;
using System.Collections.Generic;
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
        private string smtpServerName = string.Empty;
        private int smtpPortNumber;
        private string smtpLoginName = string.Empty;
        private string smtpLoginPassword = string.Empty;
        private List<string> receivers;
        private string subject = string.Empty;
        private string messageBody = string.Empty;
        private CreateMessageViewModel viewModel;
        private Account currentAccount;

        public Account CurrentAccount
        {
            get { return currentAccount; }
        }

        public CreateMessageView()
        {
            viewModel = new CreateMessageViewModel();
            this.DataContext = viewModel;
            currentAccount = viewModel.CurrentAccount;

            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            AccountManager accountManager = AccountManager.Instance;
            if (accountManager.Accounts.Count > 0)
            {
                //From.Text = currentAccount.EmailAddress;
                this.smtpServerName = currentAccount.SmtpServerName;
                this.smtpPortNumber = currentAccount.SmtpPortNumber;
                this.smtpLoginName = currentAccount.SmtpLoginName;
                this.smtpLoginPassword = currentAccount.SmtpLoginPassword;

                Trace.WriteLine("Smtp Server Name: " + this.smtpServerName);
                Trace.WriteLine("Smtp Port Number: " + this.smtpPortNumber);
                Trace.WriteLine("Smtp Login Name: " + this.smtpLoginName);
            }
        }

        private void SendButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.receivers = new List<string>();
            this.receivers.Add(To_TextBox.Text);
            this.receivers.Add(Cc_TextBox.Text);
            this.subject = Subject_TextBox.Text;
            this.messageBody = Body_TextBox.Text;
            Trace.WriteLine("Sending...");
        }

        private void AttachButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void To_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
