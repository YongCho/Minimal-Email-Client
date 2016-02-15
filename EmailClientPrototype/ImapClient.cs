using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EmailClientPrototype
{
    public class ImapClientProxy
    {
        ConnectionInfo _connectionInfo;
        ImapClientBackend _backend;

        public ImapClientProxy(string serverName, UInt16 port, string user, string password)
        {
            _connectionInfo = new ConnectionInfo()
            {
                serverName = serverName,
                port = port,
                user = user,
                password = password,
            };
            
            _backend = new ImapClientBackend(_connectionInfo);
        }
        
        // Initiates downloading of message within the given UID range.
        public void beginFetch(string mailbox, int startUid, int endUid)
        {
            // TODO: Do this in another thread.
            
            _backend.fetch(mailbox, startUid, endUid);
        }
        
        public event EventHandler<NewMessageEventArgs> FetchFinishedRelay
        {
            add { _backend.FetchFinished += value; }
            remove { _backend.FetchFinished -= value; }
        }
    }
}
