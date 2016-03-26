using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System.Windows.Input;

namespace MinimalEmailClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public InteractionRequest<WriteNewMessageNotification> WriteNewMessagePopupRequest { get; set; }
        public InteractionRequest<INotification> AddNewAccountPopupRequest { get; set; }
        public ICommand WriteNewMessageCommand { get; set; }
        public ICommand AddNewAccountCommand { get; set; }
        public ICommand DeleteMessageCommand { get; set; }

        private Account selectedAccount;

        public MainWindowViewModel()
        {
            WriteNewMessagePopupRequest = new InteractionRequest<WriteNewMessageNotification>();
            AddNewAccountPopupRequest = new InteractionRequest<INotification>();
            WriteNewMessageCommand = new DelegateCommand(RaiseWriteNewMessagePopupRequest);
            AddNewAccountCommand = new DelegateCommand(RaiseAddNewAccountPopupRequest);
            DeleteMessageCommand = new DelegateCommand(RaiseDeleteMessagesEvent);

            GlobalEventAggregator.Instance.GetEvent<AccountSelectionEvent>().Subscribe(HandleAccountSelection);
        }

        private void RaiseWriteNewMessagePopupRequest()
        {
            Account sendingAccount;
            if (this.selectedAccount == null)
            {
                if (AccountManager.Instance.Accounts.Count > 0)
                {
                    sendingAccount = AccountManager.Instance.Accounts[0];
                }
                else
                {
                    // We should not get here because we should only allow sending a message
                    // when there exists at least one user account to be used as the sender address.
                    sendingAccount = null;
                }
            }
            else
            {
                sendingAccount = this.selectedAccount;
            }
            WriteNewMessageNotification notification = new WriteNewMessageNotification(sendingAccount);
            notification.Title = "New Message";
            WriteNewMessagePopupRequest.Raise(notification);
        }

        private void RaiseAddNewAccountPopupRequest()
        {
            AddNewAccountPopupRequest.Raise(new Notification{ Content = "", Title = "New Account"});
        }

        private void RaiseDeleteMessagesEvent()
        {
            GlobalEventAggregator.Instance.GetEvent<DeleteMessagesEvent>().Publish("Dummy Payload");
        }

        private void HandleAccountSelection(Account selectedAccount)
        {
            this.selectedAccount = selectedAccount;
        }
    }
}
