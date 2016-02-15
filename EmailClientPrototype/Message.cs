using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClientPrototype
{
    public class Message
    {
        public int uid { get; set; }
        public string subject { get; set; }
        public string senderName { get; set; }
        public string senderAddress { get; set; }
        public string recipientName { get; set; }
        public string recipientAddress { get; set; }
        public DateTime date { get; set; }
        public bool isSeen;
    }
}
