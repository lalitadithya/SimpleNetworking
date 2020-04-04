using Microsoft.Extensions.Logging;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.OrderingService;
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

        public InsecureClient(ISerializer serializer, CancellationToken cancellationToken, ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000, int maximumBackoffTime = 60 * 1000, IOrderingService orderingService = null, int millisecondsIntervalForPacketResend = 60 * 1000)
        {
            delaySequenceGenerator = new ExponentialSequenceGenerator(maximumBackoffTime);
            id = Guid.NewGuid().ToString();

            Init(loggerFactory, maximumPacketBacklog, expiryTime, serializer, orderingService, cancellationToken, millisecondsIntervalForPacketResend);
        }

        internal InsecureClient(TcpNetworkTransport tcpNetworkTransport, ISerializer serializer, CancellationToken cancellationToken, ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000, IOrderingService orderingService = null, int millisecondsIntervalForPacketResend = 60 * 1000)
        {
            this.networkTransport = tcpNetworkTransport;
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostNoReconnect;

            Init(loggerFactory, maximumPacketBacklog, expiryTime, serializer, orderingService, cancellationToken, millisecondsIntervalForPacketResend);
        }

        public async void Connect(string hostName, int port)
        {
            this.hostName = hostName;
            this.port = port;

            await Connect();
            StartPacketResend(false);
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
            networkTransport = new TcpNetworkTransport(cancellationToken);
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostWithReconnect;

            ((ITcpNetworkTransport)networkTransport).Connect(hostName, port);
            await networkTransport.SendData(Encoding.Unicode.GetBytes(id));
        }

        private void NetworkTransport_OnConnectionLostWithReconnect()
        {
            Task.Run(async () => await Reconnect());
        }

        private void Init(ILoggerFactory loggerFactory, int maximumPacketBacklog, int expiryTime, ISerializer serializer, IOrderingService orderingService, CancellationToken cancellationToken, int millisecondsIntervalForPacketResend = 60 * 1000)
        {
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<InsecureClient>();
            }
            this.sendIdempotencyService = new SendIdempotencyService<Guid, Packet>(maximumPacketBacklog);
            this.receiveIdempotencyService = new ReceiveIdempotencyService<string>(expiryTime);
            this.cancellationToken = cancellationToken;
            this.millisecondsIntervalForPacketResend = millisecondsIntervalForPacketResend;
            this.serializer = serializer;
            if (orderingService == null)
            {
                this.orderingService = new SimplePacketOrderingService();
            }
            else
            {
                this.orderingService = orderingService;
            }
        }
    }
}
