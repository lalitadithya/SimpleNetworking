using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Networking
{
    public interface ITlsNetworkTransport
    {
        void Connect(string hostname, long port, string pfxFilePath, string pfxFilePassword);
    }
}
