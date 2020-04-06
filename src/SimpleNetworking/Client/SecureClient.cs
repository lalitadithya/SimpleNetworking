using Microsoft.Extensions.Logging;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.OrderingService;
using SimpleNetworking.SequenceGenerator;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public class SecureClient : Client, ISecureClient
    {
        private readonly string id;
        private string hostName;
        private int port;

        private readonly ServerCertificateValidationCallback serverCertificateValidationCallback;
        private readonly SslProtocols sslProtocols;

        public override event PeerDeviceDisconnectedHandler OnPeerDeviceDisconnected;
        public override event PeerDeviceReconnectedHandler OnPeerDeviceReconnected;

        public SecureClient(ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend, ServerCertificateValidationCallback serverCertificateValidationCallback, SslProtocols sslProtocols)
        {
            id = Guid.NewGuid().ToString();
            this.serverCertificateValidationCallback = serverCertificateValidationCallback;
            this.sslProtocols = sslProtocols;

            Init(loggerFactory, serializer, orderingService, cancellationToken,
                sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator,
                millisecondsIntervalForPacketResend);

            if (this.loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<SecureClient>();
            }
        }

        internal SecureClient(TlsNetworkTransport tlsNetworkTransport, ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend)
        {
            this.networkTransport = tlsNetworkTransport;
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostNoReconnect;

            Init(loggerFactory, serializer, orderingService, cancellationToken,
                sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator,
                millisecondsIntervalForPacketResend);

            if (this.loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<SecureClient>();
            }
        }

        public async void Connect(string hostName, int port)
        {
            this.hostName = hostName;
            this.port = port;

            await Connect();
            StartPacketResend(false);
        }

        public void ClientReconnected(TlsNetworkTransport networkTransport)
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
            networkTransport = new TlsNetworkTransport(cancellationToken, loggerFactory, serverCertificateValidationCallback, sslProtocols);
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLostWithReconnect;

            await ((ITlsNetworkTransport)networkTransport).Connect(hostName, port);
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
