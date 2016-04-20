using MimeKit;
using Prism.Mvvm;
using System.Collections.Generic;

namespace MinimalEmailClient.Models
{
    public class OutgoingEmail : BindableBase
    {
        #region Headers
        #region ToAccounts

        private List<string> to = new List<string> ();

        public List<string> To
        {
            get { return this.to; }
            set { SetProperty(ref this.to, value); }
        }

        public string ToAccounts()
        {
            IList<string> toAccounts = to;
            return string.Join(",", toAccounts);
        }

        #endregion
        #region CcAccounts

        private List<string> cc = new List<string>();

        public List<string> Cc
        {
            get { return this.cc; }
            set { SetProperty(ref this.cc, value); }
        }

        public string CcAccounts()
        {
            IList<string> ccAccounts = cc;
            return string.Join(",", ccAccounts);
        }

        #endregion
        #region BccAccounts

        private List<string> bcc = new List<string>();

        public List<string> Bcc
        {
            get { return this.bcc; }
            set { SetProperty(ref this.bcc, value); }
        }

        public string BccAccounts()
        {
            IList<string> bccAccounts = bcc;
            return string.Join(",", bccAccounts);
        }

        #endregion

        private string subject = string.Empty;
        public string Subject
        {
            get { return this.subject; }
            set { SetProperty(ref this.subject, value); }
        }

        #endregion

        private string message = string.Empty;
        public string Message
        {
            get { return this.message; }
            set { SetProperty(ref this.message, value); }
        }

        private List<MimePart> attachmentList = new List<MimePart>();
        public List<MimePart> AttachmentList
        {
            get { return attachmentList; }
        }

        public void Add(MimePart MP)
        {
            attachmentList.Add(MP);
        }
    }
}
