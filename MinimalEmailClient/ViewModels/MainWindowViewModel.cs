using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using Prism.Commands;
using Prism.Events;
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
        private IEventAggregator eventAggregator;

        private Account selectedAccount;
        private Mailbox selectedMailbox;

        public MainWindowViewModel()
        {
            WriteNewMessagePopupRequest = new InteractionRequest<WriteNewMessageNotification>();
            AddNewAccountPopupRequest = new InteractionRequest<INotification>();
            WriteNewMessageCommand = new DelegateCommand(RaiseWriteNewMessagePopupRequest);
            AddNewAccountCommand = new DelegateCommand(RaiseAddNewAccountPopupRequest);

            this.eventAggregator = GlobalEventAggregator.Instance().EventAggregator;
            this.eventAggregator.GetEvent<MailboxSelectionEvent>().Subscribe(HandleMailboxSelection);
            this.eventAggregator.GetEvent<AccountSelectionEvent>().Subscribe(HandleAccountSelection);
        }

        private void RaiseWriteNewMessagePopupRequest()
        {
            WriteNewMessageNotification notification = new WriteNewMessageNotification(this.selectedAccount);
            notification.Title = "New Message";
            WriteNewMessagePopupRequest.Raise(notification);
        }

        private void RaiseAddNewAccountPopupRequest()
        {
            AddNewAccountPopupRequest.Raise(new Notification{ Content = "", Title = "New Account"});
        }

        private void HandleMailboxSelection(Mailbox selectedMailbox)
        {
            this.selectedAccount = AccountManager.Instance().GetAccountByName(selectedMailbox.AccountName);
            this.selectedMailbox = selectedMailbox;
        }

        private void HandleAccountSelection(Account selectedAccount)
        {
            this.selectedAccount = selectedAccount;
        }
    }
}
