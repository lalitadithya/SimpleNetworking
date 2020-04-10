using Microsoft.Extensions.Logging;
using SimpleNetworking.Client;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.OrderingService;
using SimpleNetworking.SequenceGenerator;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleNetworking.Server
{
    public abstract class Server
    {
        private const int handshakeTimeout = 1 * 1000;

        protected ConcurrentDictionary<string, IClient> clients;

        protected enum HandshakeResults { NewClientConnected, ExsistingClientReconnected, HandshakeFailed }
        protected string Id { get; private set; }

        protected int millisecondsIntervalForPacketResend;
        protected ILoggerFactory loggerFactory;
        protected ISerializer serializer;
        protected CancellationToken cancellationToken;
        protected IOrderingService orderingService;
        protected ISendIdempotencyService<Guid, Packet> sendIdempotencyService;
        protected IReceiveIdempotencyService<string> receiveIdempotencyService;
        protected ISequenceGenerator delaySequenceGenerator;

        protected ILogger logger;

        protected void Init(ILoggerFactory loggerFactory, ISerializer serializer, 
            IOrderingService orderingService, ISendIdempotencyService<Guid, Packet> sendIdempotencyService, 
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator, 
            int millisecondsIntervalForPacketResend, CancellationToken cancellationToken)
        {
            Id = Guid.NewGuid().ToString();
            this.loggerFactory = loggerFactory;
            this.serializer = serializer;
            this.orderingService = orderingService;
            this.cancellationToken = cancellationToken;
            this.sendIdempotencyService = sendIdempotencyService;
            this.receiveIdempotencyService = receiveIdempotencyService;
            this.delaySequenceGenerator = delaySequenceGenerator;
            this.millisecondsIntervalForPacketResend = millisecondsIntervalForPacketResend;

            clients = new ConcurrentDictionary<string, IClient>();
        }

        protected HandshakeResults PerformHandshake(TcpClient client, NetworkTransport tcpNetworkTransport, out string clientId)
        {
            AutoResetEvent handshakeCompleteEvent = new AutoResetEvent(false);

            string _clientId = "";
            DataReceivedHandler handshakeHandler = (data) =>
            {
                _clientId = Encoding.Unicode.GetString(data);
                handshakeCompleteEvent.Set();
            };

            tcpNetworkTransport.OnDataReceived += handshakeHandler;

            if (handshakeCompleteEvent.WaitOne(handshakeTimeout) && Guid.TryParse(_clientId, out _))
            {
                tcpNetworkTransport.OnDataReceived -= handshakeHandler;
                clientId = _clientId;
                if (clients.ContainsKey(clientId))
                {
                    logger?.LogInformation("{0} is reconnected", client.Client.RemoteEndPoint);
                    return HandshakeResults.ExsistingClientReconnected;
                }
                else
                {
                    logger?.LogInformation("{0} is a new client", client.Client.RemoteEndPoint);
                    return HandshakeResults.NewClientConnected;
                }
            }
            else
            {
                tcpNetworkTransport.OnDataReceived -= handshakeHandler;
                logger.LogWarning("{0} handshake failed", client.Client.RemoteEndPoint);
                clientId = "";
                return HandshakeResults.HandshakeFailed;
            }
        }
    }
}
