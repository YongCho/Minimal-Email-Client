using MinimalEmailClient.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalEmailClient.Services
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

    public class ImapMonitorEventArgs : EventArgs
    {
        public string AccountName { get; private set; }
        public string MailboxName { get; private set; }
        public int Uid { get; private set; }

        public ImapMonitorEventArgs(string accountName, string mailboxName, int uid)
        {
            AccountName = accountName;
            MailboxName = mailboxName;
            Uid = uid;
        }
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
        public string SelectedMailboxName { get; private set; }
        public string Error = string.Empty;
        private byte[] buffer = new byte[65536];
        private int nextTagSequence = 0;
        bool abortLatch = false;

        TcpClient tcpClient;
        SslStream sslStream;

        public event EventHandler<Message> NewMessageAtServer;
        public event EventHandler<ImapMonitorEventArgs> MessageUnseenAtServer;
        public event EventHandler<ImapMonitorEventArgs> MessageSeenAtServer;
        public event EventHandler<ImapMonitorEventArgs> MessageDeletedAtServer;

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
                    this.tcpClient = newTcpClient;
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

        private bool TryReconnect(int tryCount)
        {
            Disconnect();
            while (tryCount > 0)
            {
                Trace.WriteLine("Attempting to reconnect to " + Account.ImapServerName + ":" + Account.ImapPortNumber);
                Thread.Sleep(1000);
                if (Connect())
                {
                    return true;
                }
                else
                {
                    --tryCount;
                }
            }

            return false;
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

        // Selects specified mailbox as working directory.
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
                return false;
            }

            SelectedMailboxName = mailboxPath;
            status = ImapParser.ParseExamine(response);
            return true;
        }

        // Returns a sorted list of uids that match the condition.
        // Example condition string: "ALL", "SEEN", "UNSEEN"
        public List<int> SearchUids(string conditionString)
        {
            if (string.IsNullOrEmpty(SelectedMailboxName))
            {
                Error = "Mailbox must be selected before SEARCH operation.";
                return null;
            }

            List<int> uidList = new List<int>();
            string tag = NextTag();
            SendString(string.Format("{0} UID SEARCH {1}", tag, conditionString));

            string response = string.Empty;
            if (ReadResponse(tag, out response))
            {
                Match m = Regex.Match(response, "^\\* SEARCH ([0-9 ]*)\r\n");
                response = m.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(response))
                {
                    string[] uidStrings = response.Split(' ');
                    foreach (string uidString in uidStrings)
                    {
                        uidList.Add(Convert.ToInt32(uidString));
                    }
                }
            }

            return uidList;
        }

        // Fetches body of a message.
        // SelectMailbox must be called to select the mailbox before calling this method.
        public string FetchBody(int uid)
        {
            if (string.IsNullOrEmpty(SelectedMailboxName))
            {
                Error = "Mailbox must be selected before FETCH operation.";
                return null;
            }

            string tag = NextTag();
            SendString(string.Format("{0} UID FETCH {1} BODY[]", tag, uid));

            string response = string.Empty;
            if (ReadResponse(tag, out response))
            {
                // Trim the leading and trailing IMAP strings.
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
        // SelectMailbox must be called to select the mailbox before calling this method.
        public List<Message> FetchHeaders(int startSeqNum, int count, bool useUid)
        {
            if (string.IsNullOrEmpty(SelectedMailboxName))
            {
                Error = "Mailbox must be selected before FETCH operation.";
                return null;
            }

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
                        Message message = ImapParser.ParseFetchHeader(untaggedResponse);
                        message.AccountName = Account.AccountName;
                        message.MailboxPath = SelectedMailboxName;

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
                        Trace.WriteLine("Response:\n" + response);
                        responseCount = 0;
                    }

                    // Closing the mailbox automatically expunges the deleted messages.
                    CloseMailbox(out response);

                    Trace.WriteLine(response);
                }
            }
        }

        public void CloseMailbox()
        {
            string ignoredResponse;
            CloseMailbox(out ignoredResponse);
        }

        public void CloseMailbox(out string response)
        {
            response = string.Empty;
            string tag = NextTag();
            SendString(tag + " CLOSE");
            ReadResponse(tag, out response);
            SelectedMailboxName = null;
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
                try
                {
                    byteCount = stream.Read(this.buffer, 0, this.buffer.Length);
                    byte[] data = new byte[byteCount];
                    Array.Copy(this.buffer, data, byteCount);
                    response += Encoding.ASCII.GetString(data);
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return false;
                }

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

        public void BeginMonitor(string mailboxName)
        {
            int defaultPollIntervalMillisec = 5000;
            BeginMonitor(mailboxName, defaultPollIntervalMillisec);
        }

        // Poll periodically for newly arrived messages.
        public void BeginMonitor(string mailboxName, int pollIntervalMillisec)
        {
            Task.Run(() => {
                int prevUidNext = -1;
                bool readOnly = true;
                ExamineResult examineResult;
                List<int> prevUids = null;
                List<int> prevUnseenUids = null;

                while (!this.abortLatch)
                {
                    while (!SelectMailbox(mailboxName, readOnly, out examineResult) && !this.abortLatch)
                    {
                        if (this.tcpClient.Client != null && this.tcpClient.Client.Connected)
                        {
                            // Not a connection problem. Maybe the mailbox is deleted?
                            return;
                        }
                        else
                        {
                            // We do have a connection problem. Try reconnecting at an interval.
                            while (!TryReconnect(1) && !this.abortLatch)
                                Thread.Sleep(5000);
                        }
                    }

                    if (this.abortLatch)
                    {
                        return;
                    }

                    if (prevUidNext < 0)
                    {
                        // Initially retrieve the UIDNEXT parameter.
                        prevUidNext = examineResult.UidNext;
                    }
                    else if (examineResult.UidNext > prevUidNext)
                    {
                        // New message(s) showed up at the server. Download it.
                        int newMessagesCount = examineResult.UidNext - prevUidNext;
                        bool useUidFetch = true;
                        List<Message> newMessages = FetchHeaders(prevUidNext, newMessagesCount, useUidFetch);
                        foreach (Message newMessage in newMessages)
                        {
                            OnNewMessageArrived(newMessage);
                        }

                        prevUidNext = examineResult.UidNext;
                    }
                    else if (examineResult.UidNext < prevUidNext)
                    {
                        // UIDNEXT parameter decreased. According to RFC3501, this could only happen when the
                        // physical message store is re-ordered and the server had to regenerate all UIDs.
                        // If this happens, UIDs of all local messages become invalid and we will need an
                        // application-wide mechanism to handle it.
                        Error = "UID reordered";
                        return;
                    }

                    // Check for deletion of messages.
                    List<int> currentUids = SearchUids("ALL");
                    if (prevUids != null)
                    {
                        var prevNotCurrent = prevUids.Except(currentUids).ToList();

                        if (prevNotCurrent.Count > 0)
                        {
                            // Message(s) deleted on server.
                            foreach (int uid in prevNotCurrent)
                            {
                                OnMessageDeleted(Account.AccountName, mailboxName, uid);
                            }
                        }
                    }

                    prevUids = currentUids;

                    // Check for changes in 'unseen' flag (messages read/unread).
                    List<int> currentUnseenUids = SearchUids("UNSEEN");
                    if (prevUnseenUids != null)
                    {
                        // Compare both ways because a message can go from 'read' to 'unread' or vice versa.
                        var prevNotCurrent = prevUnseenUids.Except(currentUnseenUids).ToList();
                        var currentNotPrev = currentUnseenUids.Except(prevUnseenUids).ToList();

                        if (prevNotCurrent.Count > 0)
                        {
                            // Previously unseen message(s) was marked as Seen or deleted on server.
                            foreach (int uid in prevNotCurrent)
                            {
                                OnMessageSeen(Account.AccountName, mailboxName, uid);
                            }
                        }
                        if (currentNotPrev.Count > 0)
                        {
                            // Previously seen message(s) was marked as Unseen on server; or new message(s) arrived.
                            foreach (int uid in currentNotPrev)
                            {
                                OnMessageUnseen(Account.AccountName, mailboxName, uid);
                            }
                        }
                    }

                    prevUnseenUids = currentUnseenUids;

                    CloseMailbox();
                    Thread.Sleep(pollIntervalMillisec);
                }
            });
        }

        protected virtual void OnNewMessageArrived(Message newMessage)
        {
            if (NewMessageAtServer != null)
            {
                NewMessageAtServer(this, newMessage);
            }
        }

        protected virtual void OnMessageDeleted(string accountName, string mailboxName, int uid)
        {
            if (MessageDeletedAtServer != null)
            {
                MessageDeletedAtServer(this, new ImapMonitorEventArgs(accountName, mailboxName, uid));
            }
        }

        protected virtual void OnMessageSeen(string accountName, string mailboxName, int uid)
        {
            if (MessageSeenAtServer != null)
            {
                MessageSeenAtServer(this, new ImapMonitorEventArgs(accountName, mailboxName, uid));
            }
        }

        protected virtual void OnMessageUnseen(string accountName, string mailboxName, int uid)
        {
            if (MessageUnseenAtServer != null)
            {
                MessageUnseenAtServer(this, new ImapMonitorEventArgs(accountName, mailboxName, uid));
            }
        }

    }
}
