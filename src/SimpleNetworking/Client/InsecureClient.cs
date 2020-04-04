using Microsoft.Extensions.Logging;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.SequenceGenerator;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public class InsecureClient : Client, IInsecureClient
    {
        private readonly string id;
        private string hostName;
        private int port;

        public InsecureClient(ISerializer serializer, CancellationToken cancellationToken, ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000, int maximumBackoffTime = 60 * 1000)
        {
            this.cancellationToken = cancellationToken;
            delaySequenceGenerator = new ExponentialSequenceGenerator(maximumBackoffTime);
            Init(loggerFactory, maximumPacketBacklog, expiryTime, serializer);
            id = Guid.NewGuid().ToString();
        }

        internal InsecureClient(TcpNetworkTransport tcpNetworkTransport, ISerializer serializer, CancellationToken cancellationToken, ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000)
        {
            this.cancellationToken = cancellationToken;
            this.networkTransport = tcpNetworkTransport;
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostNoReconnect;

            Init(loggerFactory, maximumPacketBacklog, expiryTime, serializer);
        }

        public async void Connect(string hostName, int port)
        {
            this.hostName = hostName;
            this.port = port;
            networkTransport = new TcpNetworkTransport(cancellationToken);

            await Connect();
        }

        public void ClientReconnected(TcpNetworkTransport networkTransport)
        {
            this.networkTransport = networkTransport;
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostNoReconnect;
            ClientReconnected();
        }

        private void NetworkTransport_OnConnectionLostNoReconnect()
        {
            ClientDisconnected();
        }

        protected override async Task Connect()
        {
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostWithReconnect;

            ((ITcpNetworkTransport)networkTransport).Connect(hostName, port);
            await networkTransport.SendData(Encoding.Unicode.GetBytes(id));
        }

        private void NetworkTransport_OnConnectionLostWithReconnect()
        {
            Task.Run(async () => await Reconnect());
        }

        private void Init(ILoggerFactory loggerFactory, int maximumPacketBacklog, int expiryTime, ISerializer serializer)
        {
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<InsecureClient>();
            }
            this.sendIdempotencyService = new SendIdempotencyService<Guid, Packet>(maximumPacketBacklog);
            this.receiveIdempotencyService = new ReceiveIdempotencyService<string>(expiryTime);
            this.serializer = serializer;
        }
    }
}
