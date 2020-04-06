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

        public override event PeerDeviceDisconnectedHandler OnPeerDeviceDisconnected;
        public override event PeerDeviceReconnectedHandler OnPeerDeviceReconnected;

        public InsecureClient(ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend)
        {
            id = Guid.NewGuid().ToString();

            Init(loggerFactory, serializer, orderingService, cancellationToken, 
                sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator, 
                millisecondsIntervalForPacketResend);

            if (this.loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<InsecureClient>();
            }
        }

        internal InsecureClient(TcpNetworkTransport tcpNetworkTransport, ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend)
        {
            this.networkTransport = tcpNetworkTransport;
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostNoReconnect;

            Init(loggerFactory, serializer, orderingService, cancellationToken,
                sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator,
                millisecondsIntervalForPacketResend);

            if (this.loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<InsecureClient>();
            }
        }

        public async void Connect(string hostName, int port)
        {
            this.hostName = hostName;
            this.port = port;

            await Connect();
            StartPacketResend(false);
        }

        public override void ClientReconnected(NetworkTransport networkTransport)
        {
            this.networkTransport.OnDataReceived -= DataReceived;
            this.networkTransport.OnConnectionLost -= NetworkTransport_OnConnectionLostNoReconnect;
            this.networkTransport.DropConnection();

            this.networkTransport = networkTransport;
            this.networkTransport.OnDataReceived += DataReceived;
            this.networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostNoReconnect;
            ClientReconnected();
        }

        private void NetworkTransport_OnConnectionLostNoReconnect()
        {
            ClientDisconnected();
        }

        protected override async Task Connect()
        {
            networkTransport = new TcpNetworkTransport(cancellationToken, loggerFactory);
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostWithReconnect;

            ((ITcpNetworkTransport)networkTransport).Connect(hostName, port);
            await networkTransport.SendData(Encoding.Unicode.GetBytes(id));
        }

        private void NetworkTransport_OnConnectionLostWithReconnect()
        {
            Task.Run(async () => await Reconnect());
        }

        protected override void RaisePeerDeviceReconnected()
        {
            try
            {
                OnPeerDeviceReconnected?.Invoke();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "OnPeerDeviceReconnected threw exception");
            }
        }

        protected override void RaisePeerDeviceDisconnected()
        {
            try
            {
                OnPeerDeviceDisconnected?.Invoke();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "OnPeerDeviceReconnected threw exception");
            }
        }
    }
}
