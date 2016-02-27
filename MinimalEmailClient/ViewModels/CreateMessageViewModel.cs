﻿using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using MinimalEmailClient.Models;
using System;

namespace MinimalEmailClient.ViewModels
{
    public class CreateMessageViewModel : BindableBase, IInteractionRequestAware
    {
        private WriteNewMessageNotification notification;
        public Action FinishInteraction { get; set; }
        public INotification Notification
        {
            get
            {
                return this.notification;
            }
            set
            {
                if (value is WriteNewMessageNotification)
                {
                    this.notification = value as WriteNewMessageNotification;
                    this.OnPropertyChanged(() => this.Notification);
                }
            }
        }
    }
}
