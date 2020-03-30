using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Networking
{
    public class TcpNetworkTransport : NetworkTransport, ITcpNetworkTransport
    {
        public void Connect(string hostName, long port)
        {
            throw new NotImplementedException();
        }
    }
}
