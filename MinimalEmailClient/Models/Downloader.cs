using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Threading;

namespace MinimalEmailClient.Models
{
    class Downloader
    {
        private readonly Account account;
        private byte[] buffer = new byte[65536];
        private int nextTagSequence = 0;

        TcpClient tcpClient;
        SslStream sslStream;


        public Downloader(Account account)
        {
            this.account = account;
            // TODO: Check for exception.
            this.tcpClient = new TcpClient(account.ImapServerName, account.ImapPortNumber);
            this.sslStream = new SslStream(this.tcpClient.GetStream(), false);
        }

        ~Downloader()
        {
            CleanUpConnection();
        }

        // Working with dummy messages for now to test the rest of the program.
        // TODO: Change this to real IMAP logic.
        public List<Message> GetDummyMessages()
        {
            List<Message> msgs = new List<Message>(20);

            //for (int i = 0; i < 20; ++i)
            //{
            //    for (int j = 1; j < 999; ++j)
            //    {
            //        Debug.WriteLine("Downloading message " + i);
            //    }
            //    Message msg = new Message()
            //    {
            //        Uid = i,
            //        Subject = string.Format("Subject of message {0}", i),
            //        SenderAddress = string.Format("Sender{0}@gmail.com", i),
            //        SenderName = string.Format("Sender Name {0}", i),
            //        RecipientAddress = string.Format("Recipient{0}@gmail.com", i),
            //        RecipientName = string.Format("Recipient Name {0}", i),
            //        Date = DateTime.Now,
            //        IsSeen = (new Random().Next(2) == 0) ? false : true,
            //    };

            //    msgs.Add(msg);
            //}

            GetMessagesRegex(10, 20, "Inbox");


            return msgs;
        }

        public List<Mailbox> GetMailboxes()
        {
            Login();

            string tag = NextTag();
            SendCommand(tag + " LIST \"\" *");

            string response;
            List<Mailbox> mailboxes = new List<Mailbox>();
            if (ReadResponse(tag, out response))
            {
                // Matches
                // * LIST (\HasChildren \Noselect) "/" "INBOX"
                // * LIST (\HasNoChildren) "\" INBOX/test1/test2/test3
                string untaggedResponsePattern = "^\\* LIST \\((?<attributes>.*)\\) \"(?<separator>.+)\" \"?(?<mailboxName>[^\"]+)\"?\r\n";
                Regex regex = new Regex(untaggedResponsePattern, RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(response);
                foreach (Match m in matches)
                {
                    Mailbox mailbox = new Mailbox()
                    {
                        AccountName = account.AccountName,
                        FullPath = m.Groups["mailboxName"].ToString(),
                        PathSeparator = m.Groups["separator"].ToString(),
                        Attributes = new List<string>(m.Groups["attributes"].ToString().Split(' ')),
                    };
                    mailboxes.Add(mailbox);
                }
            }

            return mailboxes;
        }

        public List<Message> GetMessagesRegex(int startUid, int endUid, string mailBox)
        {
            Login();

            string tag = NextTag();
            SendCommand(tag + " EXAMINE " + mailBox);
            ReadResponse(tag);

            // a1 UID FETCH 1:2 (BODY[HEADER.FIELDS (SUBJECT DATE FROM)] UID)
            tag = NextTag();
            SendCommand(string.Format("{0} UID FETCH {1}:{2} (BODY[HEADER.FIELDS (SUBJECT DATE FROM)] UID)", tag, startUid, endUid));

            var messages = new List<Message>(endUid - startUid + 1);
            int bytesInBuffer = 0;
            bool doneFetching = false;
            string unTaggedResponsePattern = "(^|\r\n)(\\* (.*\r\n)*?)(\\*|" + tag + ") ";
            string taggedResponsePattern = "^" + tag + " .*\r\n";

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
                    match = Regex.Match(remainder, unTaggedResponsePattern);
                    if (match.Success)
                    {
                        string responseItem = match.Groups[2].ToString();
                        Debug.WriteLine(responseItem);

                        string itemHeaderPattern = "^(.*)\r\n";
                        string itemHeader = Regex.Match(responseItem, itemHeaderPattern).Groups[1].ToString();
                        Debug.WriteLine("Item Header: " + itemHeader);

                        string subjectPattern = "\r\nSubject: (.*)\r\n";
                        string subject = Regex.Match(responseItem, subjectPattern).Groups[1].ToString();
                        subject = Decoder.DecodeHeaderElement(subject);
                        Debug.WriteLine("Subject: " + subject);

                        string datePattern = "\r\nDate: (.*)\r\n";
                        string date = Regex.Match(responseItem, datePattern).Groups[1].ToString();
                        Debug.WriteLine("Date: " + date);

                        string senderPattern = "\r\nFrom: (.*)<(\\S*)>\r\n";
                        Match m = Regex.Match(responseItem, senderPattern);
                        string senderName = Decoder.DecodeHeaderElement(m.Groups[1].ToString());
                        string senderAddress = m.Groups[2].ToString();
                        if (senderName.Trim() == string.Empty)
                        {
                            senderName = senderAddress;
                        }
                        Debug.WriteLine("Sender Name: " + senderName);
                        Debug.WriteLine("Sender Address: " + senderAddress);

                        remainder = remainder.Substring(match.Groups[2].ToString().Length);
                    }
                    else
                    {
                        // Did not find an untagged response. Check if it is a tagged response.
                        match = Regex.Match(remainder, taggedResponsePattern);
                        if (match.Success)
                        {
                            Debug.WriteLine(match.Groups[0].ToString());
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

            Logout();

            return messages;
        }

        public void Login()
        {
            if (!this.sslStream.IsAuthenticated)
            {
                this.sslStream.AuthenticateAsClient(account.ImapServerName);
                // We are greeted by the server at this point.
                this.sslStream.ReadTimeout = 5000;  // For synchronous read calls.
            }

            // Attempt to log in.
            string tag = NextTag();
            SendCommand(tag + " LOGIN " + account.ImapLoginName + " " + account.ImapLoginPassword);
            if (!ReadResponse(tag))
            {
                Logout();
            }
        }

        private void Logout()
        {
            string tag = NextTag();
            SendCommand(tag + " LOGOUT");
            ReadResponse(tag);
        }

        private string NextTag()
        {
            return "A" + (++this.nextTagSequence);
        }

        private void SendCommand(string command)
        {
            this.sslStream.Write(Encoding.ASCII.GetBytes(command + "\r\n"));
        }

        private bool hasTagAtBeginning(string responseLine, string tag, out bool tagOk)
        {
            string pattern = "(^|\r\n)" + tag + " (\\w+) ";
            Match match = Regex.Match(responseLine, pattern);
            bool retVal = false;
            tagOk = false;

            if (match.Success)
            {
                retVal = true;
                if (match.Groups[2].ToString() == "OK")
                {
                    tagOk = true;
                }
            }
            return retVal;
        }

        // Reads all incoming strings up to the line containing the specified tag.
        private bool ReadResponse(string tag, out string response)
        {
            int byteCount;
            response = string.Empty;
            bool? tagOk = null;

            while (!tagOk.HasValue)
            {
                byteCount = ReadItemIntoBuffer(0);
                byte[] data = new byte[byteCount];
                Array.Copy(this.buffer, data, byteCount);
                response += Encoding.ASCII.GetString(data);
                string pattern = "(^|\r\n)" + tag + " (\\w+) ";
                Match match = Regex.Match(response, pattern);
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
            }

            // Debug.Write("Response:\n" + response);
            return (bool)tagOk;
        }

        private bool ReadResponse(string tag)
        {
            string response;
            return ReadResponse(tag, out response);
        }

        // Call only when logged in.
        private int ReadItemIntoBuffer(int offset)
        {
            int totalBytes = 0;
            bool done = false;

            while (!done)
            {
                int bytesRead = 0;
                try
                {
                    // Offset always points to the next available slot.
                    bytesRead = this.sslStream.Read(this.buffer, offset, this.buffer.Length - offset);
                    totalBytes += bytesRead;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in ReadItemIntoBuffer", e.Message);
                    return totalBytes;
                }

                offset += bytesRead;
                if (this.buffer[offset - 2] == '\r' && this.buffer[offset - 1] == '\n')
                {
                    done = true;
                }
            }
            return totalBytes;
        }

        private void CleanUpConnection()
        {
            try
            {
                this.sslStream.Close();
                this.tcpClient.Close();
            }
            catch
            {
                // Do nothing. Maybe they are already closed.
            }
        }
    }
}
