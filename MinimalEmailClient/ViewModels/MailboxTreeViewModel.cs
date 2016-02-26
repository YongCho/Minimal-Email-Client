using Prism.Mvvm;
using System.Collections.Generic;
using System.Diagnostics;
using MinimalEmailClient.Models;
using System.Collections.ObjectModel;

namespace MinimalEmailClient.ViewModels
{
    class MailboxTreeViewModel : BindableBase
    {
        public ObservableCollection<Account> Accounts { get; set; }
        private string selectedMailboxName;
        public string SelectedMailboxName
        {
            get { return this.selectedMailboxName; }
            set { SetProperty(ref this.selectedMailboxName, value); }
        }

        public MailboxTreeViewModel()
        {
            AccountManager am = new AccountManager();
            Accounts = am.Accounts;
        }
    }
}
