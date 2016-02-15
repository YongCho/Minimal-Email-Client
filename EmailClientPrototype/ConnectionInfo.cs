using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClientPrototype
{
    class ConnectionInfo
    {
        public string serverName { get; set; }
        public string user { get; set; }
        public string password { get; set; }
        public UInt16 port { get; set; }
    }
}
