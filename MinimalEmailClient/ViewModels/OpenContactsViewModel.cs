using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using MinimalEmailClient.Services;
using MinimalEmailClient.Notifications;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System;
using System.ComponentModel;
using System.Windows.Input;
using Prism.Commands;
using MinimalEmailClient.Events;

namespace MinimalEmailClient.ViewModels
{
    public class OpenContactsViewModel : BindableBase, IInteractionRequestAware
    {
        private ObservableCollection<string> contacts;
        public ObservableCollection<string> Contacts
        {
            get { return this.contacts; }
            set
            {
                SetProperty(ref this.contacts, value);
            }
        }
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
                    if (this.notification.User == null)
                    {
                        Trace.WriteLine("No user account selected.");
                    }
                    else
                    {
                        Contacts = new ObservableCollection<string>(DatabaseManager.GetContacts(this.notification.User));
                    }
                }
            }
        }

        public Action FinishInteraction
        {
            get; 
            set;
        }

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

        public ICommand UpdateRecipientCommand { get; }
        public OpenContactsViewModel()
        {
            UpdateRecipientCommand = new DelegateCommand(UpdateRecipient);
        }

        private void UpdateRecipient()
        {
            GlobalEventAggregator.Instance.GetEvent<UpdateRecipientsEvent>().Publish(SelectedContact);
        }
    }
}