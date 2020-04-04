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

        public InsecureClient(CancellationToken cancellationToken, ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000, int maximumBackoffTime = 60 * 1000)
        {
            this.cancellationToken = cancellationToken;
            delaySequenceGenerator = new ExponentialSequenceGenerator(maximumBackoffTime);
            Init(loggerFactory, maximumPacketBacklog, expiryTime);
            id = Guid.NewGuid().ToString();
        }

        internal InsecureClient(TcpNetworkTransport tcpNetworkTransport, ISerializer serializer, CancellationToken cancellationToken, ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000)
        {
            this.cancellationToken = cancellationToken;
            this.serializer = serializer;
            this.networkTransport = tcpNetworkTransport;
            networkTransport.OnDataReceived += DataReceived;

            Init(loggerFactory, maximumPacketBacklog, expiryTime);
        }

        public async void Connect(string hostName, int port, ISerializer serializer)
        {
            this.serializer = serializer;
            this.hostName = hostName;
            this.port = port;
            networkTransport = new TcpNetworkTransport(cancellationToken);

            await Connect();
        }

        public void ClientReconnected(TcpNetworkTransport networkTransport)
        {
            this.networkTransport = networkTransport;
            networkTransport.OnDataReceived += DataReceived;
        }

        protected override async Task Connect()
        {
            ((ITcpNetworkTransport)networkTransport).Connect(hostName, port);
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLost;
            await networkTransport.SendData(Encoding.Unicode.GetBytes(id));
        }

        private void NetworkTransport_OnConnectionLost()
        {
            Task.Run(async () => await Reconnect());
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
