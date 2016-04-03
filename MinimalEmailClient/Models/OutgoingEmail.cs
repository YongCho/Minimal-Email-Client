using Prism.Mvvm;

namespace MinimalEmailClient.Models
{
    public class OutgoingEmail : BindableBase
    {
        private string to = string.Empty;
        public string To
        {
            get { return this.to; }
            set { SetProperty(ref this.to, value); }
        }

        private string cc = string.Empty;
        public string Cc
        {
            get { return this.cc; }
            set { SetProperty(ref this.cc, value); }
        }

        private string bcc = string.Empty;
        public string Bcc
        {
            get { return this.bcc; }
            set { SetProperty(ref this.bcc, value); }
        }

        private string subject = string.Empty;
        public string Subject
        {
            get { return this.subject; }
            set { SetProperty(ref this.subject, value); }
        }

        private string message = string.Empty;
        public string Message
        {
            get { return this.message; }
            set { SetProperty(ref this.message, value); }
        }
    }
}
