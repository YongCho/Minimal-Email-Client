using EmailClientPrototype2.Models;
using System.Collections.ObjectModel;
using System;
using System.ComponentModel;

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
        }

        public async void OnSync()
        {

        }
    }
}
