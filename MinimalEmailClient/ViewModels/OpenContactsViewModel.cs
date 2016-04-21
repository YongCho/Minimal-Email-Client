using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using MinimalEmailClient.Models;
using MinimalEmailClient.Services;
using MinimalEmailClient.Notifications;


namespace MinimalEmailClient.ViewModels
{
    public class OpenContactsViewModel : BindableBase
    {
        public List<string> Contacts = new List<string>();
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
                    if (this.notification.Contacts == null)
                    {
                        Contacts = this.notification.Contacts;
                    }
                }
            }
        }

        #endregion
    }
}
