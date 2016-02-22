using Prism.Mvvm;
using System.Diagnostics;

namespace MinimalEmailClient.ViewModels
{
    class MailboxTreeViewModel : BindableBase
    {
        private string selectedMailboxName;
        public string SelectedMailboxName
        {
            get { return this.selectedMailboxName; }
            set { SetProperty(ref this.selectedMailboxName, value); }
        }

        public MailboxTreeViewModel()
        {

        }
    }
}
