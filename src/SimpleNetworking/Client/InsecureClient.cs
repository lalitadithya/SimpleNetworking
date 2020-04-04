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
        public InsecureClient(ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000)
        {
            Init(loggerFactory, maximumPacketBacklog, expiryTime);
        }


        internal InsecureClient(TcpNetworkTransport tcpNetworkTransport, ISerializer serializer, ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000)
        {
            this.serializer = serializer;
            this.networkTransport = tcpNetworkTransport;
            networkTransport.OnDataReceived += DataReceived;

            Init(loggerFactory, maximumPacketBacklog, expiryTime);
        }

        public void Connect(string hostName, int port, ISerializer serializer)
        {
            this.serializer = serializer;
            networkTransport = new TcpNetworkTransport();
            ((ITcpNetworkTransport)networkTransport).Connect(hostName, port);
            networkTransport.OnDataReceived += DataReceived;
        }
        private void Init(ILoggerFactory loggerFactory, int maximumPacketBacklog, int expiryTime)
        {
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<InsecureClient>();
            }
            this.sendIdempotencyService = new SendIdempotencyService<Guid, Packet>(maximumPacketBacklog);
            this.receiveIdempotencyService = new ReceiveIdempotencyService<string>(expiryTime);
        }
    }
}
