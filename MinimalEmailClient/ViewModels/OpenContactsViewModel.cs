using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using MinimalEmailClient.Models;
using MinimalEmailClient.Notifications;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace MinimalEmailClient.ViewModels
{
    public class OpenContactsViewModel : BindableBase, IInteractionRequestAware
    {
        public List<string> ContactsList = new List<string>();
        #region INotification

        private OpenContactsNotification notification;
        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is OpenContactsNotification)
                {
                    this.notification = value as OpenContactsNotification;
                    this.OnPropertyChanged(() => this.Notification);
                    if (this.notification.Contacts != null)
                    {
                        ContactsList = this.notification.Contacts;
                    }
                }
            }
        }

        public Action FinishInteraction { get; set; }

        #endregion
        private string selectedContact = string.Empty;
        public string SelectedContact
        {
            get { return this.selectedContact; }
            set
            {
                SetProperty(ref this.selectedContact, value);
                RaiseViewModelPropertiesChanged();
            }
        }

        private void RaiseViewModelPropertiesChanged()
        {
            OnPropertyChanged("SelectedContact");
        }

        public ObservableCollection<string> Contacts;

        public OpenContactsViewModel()
        {
            Trace.WriteLine("Address Book populating..");
            Contacts = new ObservableCollection<string>(ContactsList);            
        }
    }
}
