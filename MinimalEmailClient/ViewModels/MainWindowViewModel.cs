using MinimalEmailClient.Models;
using MinimalEmailClient.Views;
using System.Collections.ObjectModel;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;

namespace MinimalEmailClient.ViewModels
{
    class MainWindowViewModel : CommonBase
    {
        public ObservableCollection<Message> Messages { get; set; }
        public Message SelectedMessage { get; set; }
        private RelayCommand newMailCommand;

        private string selectedInboxName;
        public string SelectedInboxName
        {
            get { return this.selectedInboxName; }
            set
            {
                if (this.selectedInboxName != value)
                {
                    this.selectedInboxName = value;
                    RaisePropertyChanged("SelectedInboxName");
                }
            }
        }

        public MainWindowViewModel()
        {
            Messages = new ObservableCollection<Message>();

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

        public ICommand NewMailCommand
        {
            get
            {
                if (this.newMailCommand == null)
                {
                    newMailCommand = new RelayCommand(param => this.writeNewMail());
                }
                return this.newMailCommand;
            }
        }

        private void writeNewMail()
        {
            NewMailWindow view = new NewMailWindow();
            NewMailWindowViewModel newMailWindowViewModel = new NewMailWindowViewModel();  // Could pass an account info here.
            view.DataContext = newMailWindowViewModel;
            view.Show();
        }
    }
}
