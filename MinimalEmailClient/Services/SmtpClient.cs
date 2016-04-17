using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.IO;
using MinimalEmailClient.Models;
using MimeKit;

namespace MinimalEmailClient.Services
{
    public class SmtpClient
    {
        #region Constructors

        public SmtpClient(Account account)
        {
            Account = account;
        }

        public SmtpClient(Account account, OutgoingEmail email)
        {
            Account = account;
            NewEmail = email;
        }

        #endregion
        #region Members

        SslStream sslStream;
        public string Error = string.Empty;
        private byte[] buffer = new byte[2048];
        private string response = string.Empty;
        private enum smtpCodes
        {
          UnableToConnect = 101,
          RefusedConnection = 111,
          SystemStatus = 211,
          HelpMessage = 214,
          ServiceReady = 220,
          ServiceClosingTransmissionChannel = 221,
          AuthenticationSuccessful = 235,
          Ok = 250,
          UserNotLocalWillForward = 251,
          CannotVerifyUserWillAttemptDelivery = 252,
          AuthenticationChallenge = 334,
          StartMailInput = 354,
          ServiceNotAvailable = 421,
          PasswordTransitionNeeded = 432,
          MailboxBusy = 450,
          ErrorInProcessing = 451,
          InsufficientStorage = 452,
          TemporaryAuthenticationFailure = 454,
          CommandUnrecognized = 500,
          SyntaxError = 501,
          CommandNotImplemented = 502,
          BadCommandSequence = 503,
          CommandParameterNotImplemented = 504,
          AuthenticationRequired = 530,
          AuthenticationMechanismTooWeak = 534,
          AuthenticationInvalidCredentials = 535,
          EncryptionRequiredForAuthenticationMechanism = 538,
          MailboxUnavailable = 550,
          UserNotLocalTryAlternatePath = 551,
          ExceededStorageAllocation = 552,
          MailboxNameNotAllowed = 553,
          TransactionFailed = 554
        };        

        #endregion
        #region Account(s)

        private Account account;
        public Account Account
        {
            get { return this.account; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Please select user account");

                if (this.account != value)
                {
                    this.account = value;
                }
            }
        }

        #endregion
        #region NewEmail

        private OutgoingEmail newEmail;
        public OutgoingEmail NewEmail
        {
            get { return newEmail; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Email contents are empty");      
                newEmail = value;
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

            Trace.WriteLine(((int)smtpCodes.ServiceReady).ToString());
            if (ReadResponse(stream) != (int)smtpCodes.ServiceReady)
            {
                //Error = "SMTP Server did not respond to connection request";
                return false;
            }

            streamWriter.WriteLine("EHLO " + Account.SmtpServerName);
            if (ReadResponse(stream) != (int)smtpCodes.Ok)
            {
                Error = "SMTP Server did not respond to HELO request";
                return false;
            }

            streamWriter.WriteLine("STARTTLS");
            if (ReadResponse(stream) != (int)smtpCodes.ServiceReady)
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
            if (!AuthorizeAndPrepareServer()) return false;            
            string encapsulationToken = GenerateEncapsulationToken();

            Trace.WriteLine("\nSending Message..\n");
            // Main Email headers
            SendString(string.Format("From: {0}", Account.SmtpLoginName));
            SendString(string.Format("To: {0}", NewEmail.ToAccounts()));
            if(NewEmail.Cc != null)
                SendString(string.Format("Cc: {0}", NewEmail.CcAccounts()));
            if (NewEmail.Bcc != null)
                SendString(string.Format("Bcc: {0}", NewEmail.BccAccounts()));
            SendString(string.Format("Subject: {0}", NewEmail.Subject));

            // MIME header
            SendString(string.Format("MIME-Version: 1.0\r\nContent-Type: multipart/alternative; boundary=\"{0}\"", encapsulationToken));
            // Text Body
            SendString(string.Format("--{0}\r\nContent-Type: text/plain; charset=\"UTF - 8\"\r\n\r\n{1}", encapsulationToken, NewEmail.Message));
            // Attachments
            foreach (MimePart attachment in NewEmail.AttachmentList)
            {
                SendString("--" + encapsulationToken);
                SendString(string.Format("Content-Type: {0}", attachment.ContentType));
                attachment.WriteTo(this.sslStream);
            }
            SendString("--" + encapsulationToken + "--\r\n.");
            ReadResponse();

            return true;
        }

        private bool AuthorizeAndPrepareServer()
        {
            // Some server require initial greeting (smtp etiquette)
            SendString("EHLO " + Account.SmtpServerName);
            if (ReadResponse() != (int)smtpCodes.Ok)
            {
                Error = "Failed authentication greeting";
                return false;
            }
            // Authorize Sender
            Trace.WriteLine("\nAUTH LOGIN" + '\n');
            SendString("AUTH LOGIN");
            if (ReadResponse() != (int)smtpCodes.AuthenticationChallenge)
            {
                Error = "Failed AUTH LOGIN handshake";
                return false;
            }

            Trace.WriteLine('\n' + Account.SmtpLoginName + '\n');
            SendString(Base64Encode(Account.SmtpLoginName));
            if (ReadResponse() != (int)smtpCodes.AuthenticationChallenge)
            {
                Error = "Failed to validate account login username";
                return false;
            }

            Trace.WriteLine('\n' + Account.SmtpServerName + '\n');
            SendString(Base64Encode(Account.SmtpLoginPassword));
            if (ReadResponse() != (int)smtpCodes.AuthenticationSuccessful)
            {
                Error = "Failed validate account password";
                return false;
            }

            Trace.WriteLine("\nMAIL FROM: " + Account.SmtpLoginName + '\n');
            SendString("MAIL FROM: <" + Account.SmtpLoginName + ">");
            if (ReadResponse() != (int)smtpCodes.Ok)
            {
                Error = "Server unable to recognize sender";
                return false;
            }

            if (!ListRecipients(NewEmail.To)) return false;

            // Send Carbon Copy to Recipients
            if (NewEmail.Cc != null)
            {
                if (!ListRecipients(NewEmail.Cc))
                    return false;
            }
            // Send Blind Carbon Copy to Recipients
            if (NewEmail.Bcc != null)
            {
                if (!ListRecipients(NewEmail.Bcc))
                    return false;
            }

            Trace.WriteLine("\nDATA\n");
            SendString("DATA");
            if (ReadResponse() != (int)smtpCodes.StartMailInput)
            {
                Error = "Server does not recognize command";
                return false;
            }

            return true;
        }

        private bool ListRecipients(List<string> recipients)
        {
            foreach (string recipient in recipients)
            {
                Trace.WriteLine("\nRCPT TO: " + recipient + '\n');
                SendString("RCPT TO: <" + recipient + ">");
                if (ReadResponse() != (int)smtpCodes.Ok)
                {
                    Error = "Server unable to find sender";
                    return false;
                }
            }
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

        private int ReadResponse()
        {
            return ReadResponse(this.sslStream);
        }

        private int ReadResponse(Stream stream)
        {
            int bytesRead;
            bytesRead = stream.Read(buffer, 0, buffer.Length);
            response = Encoding.ASCII.GetString(buffer);
            Trace.WriteLine(response);
            response = response.Substring(0, 3);
            return Int32.Parse(response);
        }

        #endregion
        #region EncapsulationToken

        private string GenerateEncapsulationToken()
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] stringChars = new char[15];
            Random random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
        }

        #endregion        
    }
}
