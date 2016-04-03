using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using MinimalEmailClient.Models;

namespace MinimalEmailClient.Services
{
    public class SmtpClient
    {
        #region Constructor

        public SmtpClient(Account account)
        {
            Account = account;
        }

        #endregion
        #region Members

        SslStream sslStream;
        public string Error = string.Empty;
        private byte[] buffer = new byte[2048];
        private string response = string.Empty;

        #endregion
        #region Account(s)

        private Account account;
        public Account Account
        {
            get { return this.account; }
            set
            {
                if (this.account != value)
                {
                    this.account = value;
                }
            }
        }

        #endregion        
        #region TCP Connection(s)

        public bool Connect()
        {
            var newTcpClient = new TcpClient(account.SmtpServerName, account.SmtpPortNumber);
            NetworkStream stream = newTcpClient.GetStream();
            var streamWriter = new StreamWriter(stream);
            streamWriter.AutoFlush = true;
            var newSslStream = new SslStream(stream);

            ReadResponse(stream);
            if (!response.StartsWith("220"))
            {
                Error = "SMTP Server did not respond to connection request";
                return false;
            }

            streamWriter.WriteLine("EHLO " + Account.SmtpServerName);
            ReadResponse(stream);
            if (!response.StartsWith("250"))
            {
                Error = "SMTP Server did not respond to HELO request";
                return false;
            }

            streamWriter.WriteLine("STARTTLS");
            ReadResponse(stream);
            if (!response.StartsWith("220"))
            {
                Error = "SMTP Server did not respond to STARTTLS request";
                return false;
            }

            try
            {
                newSslStream.AuthenticateAsClient(account.SmtpServerName);
                this.sslStream = newSslStream;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return false;
            }

            return true;
        }

        public void Disconnect()
        {
            if (this.sslStream != null)
            {
                Trace.WriteLine("Connection to " + account.SmtpServerName + " has been disconnected.");
                this.sslStream.Dispose();
            }
        }

        #endregion
        #region SendMail

        public bool SendMail(string to, string cc, string subject, string message)
        {
            // Authorize Sender
            Trace.WriteLine("\nAUTH LOGIN" + '\n');
            SendString("AUTH LOGIN");
            ReadResponse();

            Trace.WriteLine('\n' + Account.SmtpLoginName + '\n');
            SendString(Base64Encode(Account.SmtpLoginName));
            ReadResponse();

            Trace.WriteLine('\n' + Account.SmtpLoginPassword + '\n');
            SendString(Base64Encode(Account.SmtpLoginPassword));
            ReadResponse();

            Trace.WriteLine("\nMAIL FROM: " + Account.SmtpLoginName + '\n');
            SendString("MAIL FROM: <" + Account.SmtpLoginName + ">");
            ReadResponse();

            Trace.WriteLine("\nRCPT TO: " + to + '\n');
            SendString("RCPT TO: <" + to + ">");
            ReadResponse();

            Trace.WriteLine("\nDATA\n");
            SendString("DATA");
            ReadResponse();

            Trace.WriteLine("\nMESSAGEBODY\n");
            SendString(String.Format("Subject: {0}\n{1}\r\n.", subject, message));
            ReadResponse();

            Trace.WriteLine("\nend");
            return true;
        }

        private void SendString(string str, SslStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(str + "\r\n"));
        }

        private void SendString(string str)
        {
            SendString(str, this.sslStream);
        }

        #endregion
        #region EncodeDecode

        private string Base64Encode(string plainText)
        {
            if (plainText == null)
            {
                return null;
            }

            byte[] textAsBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(textAsBytes);
        }

        private string Base64Decode(string encodedText)
        {
            if (encodedText == null)
            {
                return null;
            }

            byte[] textAsBytes = Convert.FromBase64String(encodedText);
            return Encoding.UTF8.GetString(textAsBytes);
        }

        #endregion
        #region ReadResponse

        private void ReadResponse()
        {
            int bytesRead;
            bytesRead = this.sslStream.Read(buffer, 0, buffer.Length);
            response = Encoding.ASCII.GetString(buffer);
            Trace.WriteLine(response);
        }

        private void ReadResponse(NetworkStream stream)
        {
            int bytesRead;
            bytesRead = stream.Read(buffer, 0, buffer.Length);
            response = Encoding.ASCII.GetString(buffer);
        }

        #endregion
    }
}
