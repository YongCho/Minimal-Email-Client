using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace MinimalEmailClient.Models
{
    public struct ExamineResult
    {
        public int Recent;
        public int Exists;
        public int UidValidity;
        public int UidNext;
        public List<string> PermanemtFlags;
        public List<string> Flags;
    }

    public class ImapClient
    {
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
        public string Error = string.Empty;
        private byte[] buffer = new byte[65536];
        private int nextTagSequence = 0;

        SslStream sslStream;

        public ImapClient(Account account)
        {
            Account = account;
        }

        public bool Connect()
        {
            TcpClient newTcpClient = new TcpClient();
            try
            {
                var result = newTcpClient.BeginConnect(account.ImapServerName, account.ImapPortNumber, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                if (!success || !newTcpClient.Connected)
                {
                    Error = "Unable to connect to " + account.ImapServerName + ":" + account.ImapPortNumber;
                    return false;
                }
            }
            catch (Exception e)
            {
                Error = e.Message;
                return false;
            }

            try
            {
                SslStream newSslStream = new SslStream(newTcpClient.GetStream(), false);
                newSslStream.AuthenticateAsClient(account.ImapServerName);
                newSslStream.ReadTimeout = 5000;  // For synchronous read calls.

                if (TryLogin(newSslStream))
                {
                    this.sslStream = newSslStream;
                }
                else
                {
                    Error = "Could not log in to server. Check credentials.";
                    return false;
                }
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
                this.sslStream.Dispose();
            }
        }

        private bool TryLogin(SslStream stream)
        {
            string tag = NextTag();
            string command = tag + " LOGIN " + account.ImapLoginName + " " + account.ImapLoginPassword + "\r\n";
            stream.Write(Encoding.ASCII.GetBytes(command));

            string response = string.Empty;
            return ReadResponse(tag, out response, stream);
        }

        // Sends LIST command and returns the result as a list of Mailbox objects.
        // Each Mailbox object is populated with the result of the LIST command.
        public List<Mailbox> ListMailboxes()
        {
            string tag = NextTag();
            SendString(tag + " LIST \"\" *");

            string response;
            List<Mailbox> mailboxes = new List<Mailbox>();
            if (ReadResponse(tag, out response))
            {
                // Matches
                // * LIST (\HasChildren \Noselect) "/" "INBOX"
                // * LIST (\HasNoChildren) "\" INBOX/test1/test2/test3
                string untaggedResponsePattern = "^\\* LIST \\((?<flags>.*)\\) \"(?<separator>.+)\" (?<mailboxName>[^\r\n]+)\r\n";
                Regex regex = new Regex(untaggedResponsePattern, RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(response);
                foreach (Match m in matches)
                {
                    Mailbox mailbox = new Mailbox()
                    {
                        AccountName = account.AccountName,
                        DirectoryPath = m.Groups["mailboxName"].ToString(),
                        PathSeparator = m.Groups["separator"].ToString(),
                        FlagString = m.Groups["flags"].ToString(),
                    };
                    mailboxes.Add(mailbox);
                }
            }

            return mailboxes;
        }

        public bool SelectMailbox(string mailboxPath, bool readOnly)
        {
            ExamineResult result = new ExamineResult();
            return SelectMailbox(mailboxPath, readOnly, out result);
        }
        // Sends 'EXAMINE' command to the server with the specified mailbox.
        public bool SelectMailbox(string mailboxPath, bool readOnly, out ExamineResult status)
        {
            status = new ExamineResult();

            string tag = NextTag();
            if (readOnly)
            {
                SendString(tag + " EXAMINE " + mailboxPath);
            }
            else
            {
                SendString(tag + " SELECT " + mailboxPath);
            }

            string response = string.Empty;
            if (!ReadResponse(tag, out response))
            {
                Error = response;
                return false;
            }

            status = ResponseParser.ParseExamine(response);
            return true;
        }

        // Fetches body of a message.
        // ExamineMailbox must be called to select the mailbox before calling this method.
        public string FetchBody(int uid)
        {
            string tag = NextTag();
            SendString(string.Format("{0} UID FETCH {1} BODY[]", tag, uid));

            string response = string.Empty;
            if (ReadResponse(tag, out response))
            {
                string bodyPattern = "^\\*[^\r\n]*\r\n(?<body>[\\s\\S]*)\r\n.*\\)\r\n" + tag + " OK";
                Match m = Regex.Match(response, bodyPattern);
                if (m.Success)
                {
                    response = m.Groups["body"].ToString();
                }
            }

            return response;
        }

        // Fetches message headers and returns them as a list of Message objects.
        public List<Message> FetchHeaders(int startSeqNum, int count, bool useUid)
        {
            var messages = new List<Message>(count);

            if (startSeqNum < 1 || count < 1)
            {
                return messages;
            }

            string tag = NextTag();
            string command;
            if (useUid)
            {
                command = "{0} UID FETCH {1}:{2} (FLAGS BODY[HEADER.FIELDS (SUBJECT DATE FROM TO)] UID)";
            }
            else
            {
                command = "{0} FETCH {1}:{2} (FLAGS BODY[HEADER.FIELDS (SUBJECT DATE FROM TO)] UID)";
            }
            SendString(string.Format(command, tag, startSeqNum, startSeqNum + count - 1));

            int bytesInBuffer = 0;
            bool doneFetching = false;

            while (!doneFetching)
            {
                // Read the next chunk from the stream.
                int bytesRead = this.sslStream.Read(this.buffer, bytesInBuffer, this.buffer.Length - bytesInBuffer);

                string strBuffer = Encoding.ASCII.GetString(this.buffer, 0, bytesInBuffer + bytesRead);
                Match match;
                string remainder = strBuffer;
                bool doneMatching = false;
                while (!doneMatching)
                {
                    // Each untagged response item represents one message.
                    string unTaggedResponsePattern = "(^|\r\n)(\\* (.*\r\n)*?)(\\*|" + tag + ") ";
                    match = Regex.Match(remainder, unTaggedResponsePattern);
                    if (match.Success)
                    {
                        string untaggedResponse = match.Groups[2].ToString();
                        Message message = ResponseParser.ParseFetchHeader(untaggedResponse);

                        if (message != null)
                        {
                            messages.Add(message);
                        }

                        remainder = remainder.Substring(match.Groups[2].ToString().Length);
                    }
                    else
                    {
                        // Did not find an untagged response. Check if it is a tagged response.
                        string taggedResponsePattern = "^" + tag + " .*\r\n";
                        match = Regex.Match(remainder, taggedResponsePattern);
                        if (match.Success)
                        {
                            doneMatching = true;
                            doneFetching = true;
                        }
                        else
                        {
                            // Neither untagged nor tagged response was found. Maybe only a portion of
                            // the response was read. Move the dangling bytes to the front of the buffer
                            // for the next read.
                            doneMatching = true;
                            if (remainder.Length > 0)
                            {
                                for (int i = 0; i < remainder.Length; ++i)
                                {
                                    this.buffer[i] = (byte)remainder[i];
                                }
                                bytesInBuffer = remainder.Length;
                            }
                        }
                    }
                }
            }

            return messages;
        }

        public void DeleteMessages(List<Message> messages)
        {
            bool selectOk = true;

            while (messages.Count > 0 && selectOk)
            {
                string mailboxPath = messages[0].MailboxPath;
                bool readOnly = false;
                selectOk = SelectMailbox(mailboxPath, readOnly);
                if (selectOk)
                {
                    int responseCount = 0;
                    string response = string.Empty;
                    string tag = string.Empty;

                    // Delete all messages in this mailbox.
                    for (int i = messages.Count - 1; i >= 0; --i)
                    {
                        Message msg = messages[i];
                        if (msg.MailboxPath == mailboxPath)
                        {
                            tag = NextTag();
                            SendString(tag + " UID STORE " + msg.Uid + " +FLAGS (\\Seen \\Deleted)");
                            ++responseCount;

                            // Empty the socket buffer every 10 commands.
                            if (responseCount == 10)
                            {
                                ReadResponse(tag, out response);
                                Trace.WriteLine("Response:\n" + response);
                                responseCount = 0;
                            }

                            messages.RemoveAt(i);
                        }
                    }

                    // Read the remaining responses.
                    if (responseCount > 0)
                    {
                        ReadResponse(tag, out response);
                        Trace.WriteLine("Response: " + response);
                        responseCount = 0;
                    }

                    // Expunge the tagged messages and move out of the currently selected mailbox.
                    tag = NextTag();
                    SendString(tag + " CLOSE");
                    ReadResponse(tag, out response);
                    Trace.WriteLine(response);
                }
            }
        }

        private void Logout()
        {
            string tag = NextTag();
            SendString(tag + " LOGOUT");
            ReadResponse(tag);
        }

        private string NextTag()
        {
            return "A" + (++this.nextTagSequence);
        }

        private void SendString(string str, SslStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes(str + "\r\n"));
        }

        private void SendString(string str)
        {
            SendString(str, this.sslStream);
        }

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
    }
}
