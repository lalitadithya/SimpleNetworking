using Microsoft.Extensions.Logging;
using SimpleNetworking.Networking;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Client
{
    public class InsecureClient : Client, IInsecureClient
    {
        public InsecureClient(ILoggerFactory loggerFactory = null)
        {
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<InsecureClient>();
            }
        }

        internal InsecureClient(TcpNetworkTransport tcpNetworkTransport, ISerializer serializer)
        {
            this.serializer = serializer;
            this.networkTransport = tcpNetworkTransport;
            networkTransport.OnDataReceived += DataReceived;
        }

        public void Connect(string hostName, int port, ISerializer serializer)
        {
            this.serializer = serializer;
            networkTransport = new TcpNetworkTransport();
            ((ITcpNetworkTransport)networkTransport).Connect(hostName, port);
            networkTransport.OnDataReceived += DataReceived;
        }
    }
}
