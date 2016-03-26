using MinimalEmailClient.ViewModels;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.Commands
{
    using System;
    using System.Windows.Input;

    internal class SendMailCommand : ICommand
    {
        private CreateMessageViewModel _newMessage;

        // Initialize a new instance of the SendNewMessage class.
        public SendMailCommand(CreateMessageViewModel newMessage)
        {
            _newMessage = newMessage;
        }

        public void EstablishConection(Account account)
        {
            SmtpClient newClient = new SmtpClient(account);
            newClient.Connect();
        }

        #region ICommand Members

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value;  }
            remove { CommandManager.RequerySuggested -= value;  }
        }

        public bool CanExecute(object parameter)
        {
            return _newMessage.CanSend;
        }

        public void Execute(object parameter)
        {
            _newMessage.SendEmail();
        }

        #endregion
    }
}
