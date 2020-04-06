using SimpleNetworking.Client;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace SimpleNetworking.Server
{
    public delegate void ClientConnectedHandler(InsecureClient client);
    public interface IInsecureServer
    {
        event ClientConnectedHandler OnClientConnected;
        void StartListening(IPAddress localAddress, int port);
    }
}
