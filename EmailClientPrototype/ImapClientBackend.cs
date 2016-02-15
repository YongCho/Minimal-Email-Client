using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace EmailClientPrototype
{
    public class NewMessageEventArgs : EventArgs
    {
        public string mailboxName { get; set; }
        public List<Message> messages { get; set; }
    }

    class ImapClientBackend
    {
        private ConnectionInfo _connectionInfo;
        TcpClient _tcpClient;
        SslStream _sslStream;
        byte[] _buffer = new byte[65536];
        int _nextTagSequence = 0;
        string _response;

        public ImapClientBackend(ConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
        }
        
        ~ImapClientBackend()
        {
            CleanUpConnection();
        }

        public void ConnectAndLogIn()
        {
            _tcpClient = new TcpClient(_connectionInfo.serverName, _connectionInfo.port);
            _sslStream = new SslStream(_tcpClient.GetStream(), false);
            _sslStream.AuthenticateAsClient(_connectionInfo.serverName);
            // We are greeted by the server at this point.

            _sslStream.ReadTimeout = 5000;  // For synchronous read calls.

            // Attempt to log in.
            _response = "";
            string tag = NextTag();
            WriteToStream(tag + " LOGIN " + _connectionInfo.user + " " + _connectionInfo.password);
            if (!ReadResponse(tag))
            {
                tag = NextTag();
                LogOutAndDisconnect();
            }
        }

        private void LogOutAndDisconnect()
        {
            WriteToStream(NextTag() + " LOGOUT");
            CleanUpConnection();
        }

        public void fetch(string mailboxName, int startUid, int endUid)
        {
            var messages = new List<Message>();

            if (startUid < 0 || startUid > endUid)
            {
                return;
            }

            try
            {
                ConnectAndLogIn();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return;
            }
            
            messages.Capacity = endUid - startUid + 1;
            for (int i = startUid; i <= endUid; ++i)
            {
                var message = new Message
                {
                    uid = i,
                    subject = "Subject of message " + i,
                    senderName = "SenderName" + i,
                    senderAddress = "SenderAddress" + i,
                    recipientName = "RecipientName" + i,
                    recipientAddress = "RecipientAddress" + i,
                    date = new DateTime(2016, 2, 14, 12, i, 00),
                    isSeen = true,
                };
                messages.Add(message);
            }

            LogOutAndDisconnect();
            OnFetchFinished(mailboxName, messages);
        }

        public event EventHandler<NewMessageEventArgs> FetchFinished;
        protected virtual void OnFetchFinished(string mailboxName, List<Message> messages)
        {
            if (FetchFinished != null)
            {
                NewMessageEventArgs args = new NewMessageEventArgs()
                {
                    mailboxName = mailboxName,
                    messages = messages,
                };
                FetchFinished(this, args);
            }
        }

        // Reads the entire response string into _response.
        private bool ReadResponse(string tag)
        {
            int byteCount;
            bool? tagOk = null;
            
            while (!tagOk.HasValue)
            {
                byteCount = ReadItemIntoBuffer();
                byte[] data = new byte[byteCount];
                Array.Copy(_buffer, data, byteCount);
                _response += Encoding.ASCII.GetString(data);
                string pattern = "(^|\r\n)" + tag + " (\\w+) ";
                Match match = Regex.Match(_response, pattern);
                if (match.Success)
                {
                    if (match.Groups[2].ToString() == "OK")
                    {
                        tagOk = true;
                    }
                    else
                    {
                        tagOk = false;
                    }                    
                }
                else
                {
                    // Process the item.
                }

                Debug.Write(_response);
            }

            return (bool) tagOk;
        }
        
        private int ReadItemIntoBuffer()
        {
            int byteCount;

            byteCount = _sslStream.Read(_buffer, 0, _buffer.Length);
            while (!(_buffer[byteCount - 2] == '\r' && _buffer[byteCount - 1] == '\n'))
            {
                byteCount += _sslStream.Read(_buffer, byteCount, _buffer.Length);
            }

            return byteCount;
        }

        private void WriteToStream(string command)
        {
            _sslStream.Write(Encoding.ASCII.GetBytes(command + "\r\n"));
        }

        private string NextTag()
        {
            return "A" + (++_nextTagSequence);
        }

        private void CleanUpConnection()
        {
            _sslStream.Close();
            _tcpClient.Close();
        }
    }
}
