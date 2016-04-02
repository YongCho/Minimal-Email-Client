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
            var newTcpClient = new TcpClient(account.SmtpServerName, account.SmtpPortNumber);
            var stream = newTcpClient.GetStream();
            var streamReader = new StreamReader(stream);
            var streamWriter = new StreamWriter(stream);
            streamWriter.AutoFlush = true;
            var newSslStream = new SslStream(stream);
            var connectResponse = streamReader.ReadLine();

            Trace.WriteLine(connectResponse.ToString());
            if (!connectResponse.StartsWith("220"))
            {
                Error = "SMTP Server did not respond to connection request";
                return false;
            }

            streamWriter.WriteLine("HELO");
            var helloResponse = streamReader.ReadLine();
            Trace.WriteLine(helloResponse.ToString());
            if (!helloResponse.StartsWith("250"))
            {
                Error = "SMTP Server did not respond to HELO request";
                return false;
            }

            streamWriter.WriteLine("STARTTLS");
            var startTlsResponse = streamReader.ReadLine();
            Trace.WriteLine(startTlsResponse.ToString());
            if (!startTlsResponse.StartsWith("220"))
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

        public bool SendMail()
        {
            SendString("EHLO");
            Trace.WriteLine(account.SmtpLoginName);          
            var reader = new StreamReader(this.sslStream);
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());
            Trace.WriteLine(reader.ReadLine());            
            SendString("AUTH LOGIN");
            Trace.WriteLine(reader.ReadLine());

            //SendString("MAIL FROM:<" + account.SmtpLoginName + ">");

            Trace.WriteLine("end");
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

        public static string Base64Encode(string plainText)
        {
            if (plainText == null)
            {
                return null;
            }

            byte[] textAsBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(textAsBytes);
        }

        public static string Base64Decode(string encodedText)
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
