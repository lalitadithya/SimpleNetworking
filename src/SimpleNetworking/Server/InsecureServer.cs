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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Server
{
    public class InsecureServer : IInsecureServer
    {
        private const int handshakeTimeout = 30 * 1000;

        private TcpListener tcpListener;

        private int millisecondsIntervalForPacketResend;
        private ILoggerFactory loggerFactory;
        private ISerializer serializer;
        private CancellationToken cancellationToken;
        private IOrderingService orderingService;
        private ISendIdempotencyService<Guid, Packet> sendIdempotencyService;
        private IReceiveIdempotencyService<string> receiveIdempotencyService;
        private ISequenceGenerator delaySequenceGenerator;

        private readonly ConcurrentDictionary<string, InsecureClient> clients;
        private ILogger logger;

        public event ClientConnectedHandler OnClientConnected;

        public InsecureServer(ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend)
        {
            this.loggerFactory = loggerFactory;
            this.serializer = serializer;
            this.orderingService = orderingService;
            this.cancellationToken = cancellationToken;
            this.sendIdempotencyService = sendIdempotencyService;
            this.receiveIdempotencyService = receiveIdempotencyService;
            this.delaySequenceGenerator = delaySequenceGenerator;
            this.millisecondsIntervalForPacketResend = millisecondsIntervalForPacketResend;

            if(this.loggerFactory != null)
            {
                logger = this.loggerFactory.CreateLogger<InsecureServer>();
            }

            clients = new ConcurrentDictionary<string, InsecureClient>();
        }

        public void StartListening(IPAddress localAddress, int port, ISerializer serializer, CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            this.serializer = serializer;

            cancellationToken.Register(() => Stop());

            tcpListener = new TcpListener(localAddress, port);
            tcpListener.Start();
            AcceptLoop();
        }

        private void AcceptLoop()
        {
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await tcpListener.AcceptTcpClientAsync();
                    ProcessClient(client);
                }
            });
        }

        private void ProcessClient(TcpClient client)
        {
            logger.LogInformation("{0} connected", client.Client.RemoteEndPoint);

            TcpNetworkTransport tcpNetworkTransport = new TcpNetworkTransport(cancellationToken, client, loggerFactory);
            AutoResetEvent handshakeCompleteEvent = new AutoResetEvent(false);

            string clientId = "";
            DataReceivedHandler handshakeHandler = (data) =>
            {
                clientId = Encoding.Unicode.GetString(data);
                handshakeCompleteEvent.Set();
            };

            tcpNetworkTransport.OnDataReceived += handshakeHandler;

            if (handshakeCompleteEvent.WaitOne(handshakeTimeout))
            {
                tcpNetworkTransport.OnDataReceived -= handshakeHandler;
                if (clients.ContainsKey(clientId))
                {
                    logger?.LogInformation("{0} is reconnected", client.Client.RemoteEndPoint);
                    clients[clientId].ClientReconnected(tcpNetworkTransport);
                }
                else
                {
                    logger?.LogInformation("{0} is a new client", client.Client.RemoteEndPoint);
                    InsecureClient insecureClient = new InsecureClient(tcpNetworkTransport, loggerFactory, serializer, orderingService, 
                        cancellationToken, sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator, millisecondsIntervalForPacketResend);
                    clients.TryAdd(clientId, insecureClient);
                    OnClientConnected?.Invoke(insecureClient);
                }
            }
            else
            {
                logger.LogWarning("{0} handshake failed", client.Client.RemoteEndPoint);

                tcpNetworkTransport.OnDataReceived -= handshakeHandler;
                tcpNetworkTransport.DropConnection();
                client.Close();
                client.Dispose();
            }
        }

        private void Stop()
        {
            tcpListener?.Stop();
        }
    }
}
