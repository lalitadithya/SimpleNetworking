using Microsoft.Extensions.Logging;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Client
{
    public class InsecureClient : Client, IInsecureClient
    {
        public InsecureClient(ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000)
        {
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<InsecureClient>();
            }
            this.idempotencyService = new SendIdempotencyService<Guid, Packet>(maximumPacketBacklog);
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
