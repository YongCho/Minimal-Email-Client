using MinimalEmailClient.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;

namespace MinimalEmailClient.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        public ObservableCollection<Message> Messages { get; set; }
        public Message SelectedMessage { get; set; }
        public InteractionRequest<WriteNewMessageNotification> WriteNewMessagePopupRequest { get; set; }
        public ICommand WriteNewMessageCommand { get; set; }

        public MainWindowViewModel()
        {
            Messages = new ObservableCollection<Message>();
            WriteNewMessagePopupRequest = new InteractionRequest<WriteNewMessageNotification>();
            WriteNewMessageCommand = new DelegateCommand(RaiseWriteNewMessagePopupRequest);

            // Let's get some dummy messages to test the UI.
            Sync();
        }

        public async void Sync()
        {
            List<Message> msgs = await Task.Run<List<Message>>(() =>
            {
                Downloader downloader = new Downloader("imap.gmail.com", 993, "test.racketscience", "12#$zxCV");
                return downloader.getDummyMessages();
            });

            foreach (Message m in msgs)
            {
                Messages.Add(m);
            }

            foreach (Message m in Messages)
            {
                Debug.WriteLine(m.ToString());
            }
        }

        private void RaiseWriteNewMessagePopupRequest()
        {
            WriteNewMessageNotification notification = new WriteNewMessageNotification("Currently selected account info goes here");
            notification.Title = "New Message";
            WriteNewMessagePopupRequest.Raise(notification);
        }
    }
}
