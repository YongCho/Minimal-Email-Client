using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Threading;

namespace EmailClientPrototype2.Models
{
    class Downloader
    {
        public string ServerName { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        private const int bufferSize = 65536;
        private byte[] buffer = new byte[bufferSize];
        int nextTagSequence = 0;
        string response;

        TcpClient tcpClient;
        SslStream sslStream;
        

        public Downloader(string serverName, int port, string user, string password)
        {
            ServerName = serverName;
            Port = port;
            User = user;
            Password = password;

            tcpClient = new TcpClient(ServerName, Port);
            sslStream = new SslStream(tcpClient.GetStream(), false);
        }

        ~Downloader()
        {
            CleanUpConnection();
        }

        // Working with dummy messages for now to test the rest of the program.
        // TODO: Change this to real IMAP logic.
        public List<Message> getDummyMessages()
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

            getMessagesRegex(1, 10000, "Inbox");


            return msgs;
        }

        public List<Message> getMessagesRegex(int startUid, int endUid, string mailBox)
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
            string nonTaggedResponsePattern = "(^|\r\n)(\\* (.*\r\n)*?)(\\*|" + tag + ") ";
            string taggedResponsePattern = "^" + tag + " .*\r\n";

            while (!doneFetching)
            {
                // Read the next chunk from the stream.
                int bytesRead = sslStream.Read(buffer, bytesInBuffer, buffer.Length - bytesInBuffer);

                string strBuffer = Encoding.UTF8.GetString(buffer, 0, bytesInBuffer + bytesRead);
                Match match;
                string remainder = strBuffer;
                bool doneMatching = false;
                while (!doneMatching)
                {
                    match = Regex.Match(remainder, nonTaggedResponsePattern);
                    if (match.Success)
                    {
                        string header = match.Groups[2].ToString();
                        Debug.WriteLine(header);



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
                                    buffer[i] = (byte)remainder[i];
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
            if (!sslStream.IsAuthenticated)
            {
                Debug.WriteLine("Logging in");
                sslStream.AuthenticateAsClient(ServerName);
                // We are greeted by the server at this point.
                sslStream.ReadTimeout = 5000;  // For synchronous read calls.
            }

            // Attempt to log in.
            response = "";
            string tag = NextTag();
            SendCommand(tag + " LOGIN " + User + " " + Password);
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
            return "A" + (++nextTagSequence);
        }

        private void SendCommand(string command)
        {
            sslStream.Write(Encoding.ASCII.GetBytes(command + "\r\n"));
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

        // Reads the entire response string into _response.
        private bool ReadResponse(string tag)
        {
            int byteCount;
            bool? tagOk = null;

            while (!tagOk.HasValue)
            {
                byteCount = ReadItemIntoBuffer(0);
                byte[] data = new byte[byteCount];
                Array.Copy(buffer, data, byteCount);
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
                else
                {
                    // Process the item.
                }

                Debug.Write(response);
            }

            return (bool)tagOk;
        }

        // Call only when logged in.
        private int ReadItemIntoBuffer(int offset)
        {
            int byteCount = 0;
            bool done = false;

            while (!done)
            {
                try
                {
                    // Offset always points to the next available slot.
                    byteCount += sslStream.Read(buffer, offset, buffer.Length - offset);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception in ReadItemIntoBuffer", e.Message);
                    return byteCount;
                }

                offset += byteCount;
                if (buffer[offset - 2] == '\r' && buffer[offset - 1] == '\n')
                {
                    done = true;
                }
            }
            return byteCount;
        }
        
        private void CleanUpConnection()
        {
            try
            {
                sslStream.Close();
                tcpClient.Close();
            }
            catch
            {
                // Do nothing. Maybe they are already closed.
            }
        }
    }
}
