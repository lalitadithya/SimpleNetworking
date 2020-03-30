using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Networking
{
    public class TlsNetworkTransport : NetworkTransport, ITlsNetworkTransport
    {
        public void Connect(string hostname, long port, string pfxFilePath, string pfxFilePassword)
        {
            throw new NotImplementedException();
        }
    }
}
