using SimpleNetworking.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Client
{
    public class InsecureClient : Client, IInsecureClient
    {
        public void Connect(string hostName, int port)
        {
            networkTransport = new TcpNetworkTransport();
            ((IInsecureClient)networkTransport).Connect(hostName, port);
        }
    }
}
