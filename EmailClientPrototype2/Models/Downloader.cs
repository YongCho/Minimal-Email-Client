using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace EmailClientPrototype2.Models
{
    class Downloader
    {
        // Working with dummy messages for now to test the rest of the program.
        // TODO: Change this to real IMAP logic.
        public static List<Message> getDummyMessages()
        {
            List<Message> msgs = new List<Message>(20);

            for (int i = 0; i < 20; ++i)
            {
                for (int j = 1; j < 999; ++j)
                {
                    Debug.WriteLine("Downloading message " + i);
                }
                Message msg = new Message()
                {
                    Uid = i,
                    Subject = string.Format("Subject of message {0}", i),
                    SenderAddress = string.Format("Sender{0}@gmail.com", i),
                    SenderName = string.Format("Sender Name {0}", i),
                    RecipientAddress = string.Format("Recipient{0}@gmail.com", i),
                    RecipientName = string.Format("Recipient Name {0}", i),
                    Date = DateTime.Now,
                    IsSeen = (new Random().Next(2) == 0) ? false : true,
                };

                msgs.Add(msg);
            }

            return msgs;
        }
    }
}
