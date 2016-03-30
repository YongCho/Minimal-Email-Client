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

        public string Error = string.Empty;
        SslStream sslStream;

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
            using (var newTcpClient = new TcpClient(account.SmtpServerName, account.SmtpPortNumber))
            {
                using (var stream = newTcpClient.GetStream())
                using (var streamReader = new StreamReader(stream))
                using (var streamWriter = new StreamWriter(stream) { AutoFlush = true })
                using (var newSslStream = new SslStream(stream))
                {
                    var connectResponse = streamReader.ReadLine();
                    if (!connectResponse.StartsWith("220"))
                        Error = "SMTP Server did not respond to connection request";

                    streamWriter.WriteLine("HELO");
                    var helloResponse = streamReader.ReadLine();
                    if (!helloResponse.StartsWith("250"))
                        Error = "SMTP Server did not respond to HELO request";

                    streamWriter.WriteLine("STARTTLS");
                    var startTlsResponse = streamReader.ReadLine();
                    if (!startTlsResponse.StartsWith("220"))
                        Error = "SMTP Server did not respond to STARTTLS request";
                    try
                    {
                        newSslStream.AuthenticateAsClient(account.SmtpServerName);
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                        return false;
                    }
                    this.sslStream = newSslStream;
                }                
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
        #region ReadResponse

        private byte[] buffer = new byte[65536];

        private bool ReadResponse(string tag)
        {
            string response;
            return ReadResponse(tag, out response);
        }

        private bool ReadResponse(string tag, out string response)
        {
            return ReadResponse(tag, out response, this.sslStream);
        }

        // Reads all incoming strings up to the line containing the specified tag.
        private bool ReadResponse(string tag, out string response, SslStream stream)
        {
            int byteCount;
            response = string.Empty;
            bool? tagOk = null;
            string pattern = "^" + tag + " (?<ok>[a-zA-Z]+).*\r\n";
            Match m;

            while (!tagOk.HasValue)
            {
                byteCount = stream.Read(this.buffer, 0, this.buffer.Length);
                byte[] data = new byte[byteCount];
                Array.Copy(this.buffer, data, byteCount);
                response += Encoding.ASCII.GetString(data);
                m = Regex.Match(response, pattern, RegexOptions.Multiline);
                if (m.Success)
                {
                    if (m.Groups["ok"].ToString() == "OK")
                    {
                        tagOk = true;
                    }
                    else
                    {
                        tagOk = false;
                    }
                }
            }           

            // Debug.Write("Response:\n" + response);
            return (bool)tagOk;
        }

        #endregion
    }
}
