using EmailClientPrototype2.Models;
using System.Collections.ObjectModel;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace EmailClientPrototype2.ViewModels
{
    class MainWindowViewModel : ObservableClass
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
            OnSync();
        }

        public async void OnSync()
        {
            List<Message> msgs = await Task.Run<List<Message>>(() => 
            {
                return Downloader.getDummyMessages();
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
