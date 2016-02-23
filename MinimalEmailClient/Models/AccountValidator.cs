using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;

namespace MinimalEmailClient.Models
{
    class AccountValidator
    {
        public string ImapServerName { get; set; }
        public int ImapPort { get; set; }
        public string ImapLoginName { get; set; }
        public string ImapPassword { get; set; }
        public string SmtpServerName { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpLoginName { get; set; }
        public string SmtpLoginPassword { get; set; }

        public List<string> Errors = new List<string>();

        public bool Validate()
        {
            Errors.Clear();
            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect(ImapServerName, ImapPort);
            }
            catch
            {
                Errors.Add("Unable to connect to " + ImapServerName + ":" + ImapPort + ".");
                return false;
            }

            SslStream sslStream = new SslStream(tcpClient.GetStream(), false);
            try
            {
                sslStream.AuthenticateAsClient(ImapServerName);
            }
            catch
            {
                Errors.Add("Unable to connect to " + ImapServerName + ":" + ImapPort + ".");
                return false;
            }

            return true;
        }
    }
}
