using MinimalEmailClient.Models;
using System.Collections.ObjectModel;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinimalEmailClient.ViewModels
{
    class MainWindowViewModel : CommonBase
    {
        public ObservableCollection<Message> Messages { get; set; }
        public Message SelectedMessage { get; set; }

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
    }
}
