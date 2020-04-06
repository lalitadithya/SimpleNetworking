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
    public class InsecureServer : Server, IInsecureServer
    {
        private TcpListener tcpListener;

        public event ClientConnectedHandler OnClientConnected;

        public InsecureServer(ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend)
        {
            Init(loggerFactory, serializer, orderingService, 
                sendIdempotencyService, receiveIdempotencyService, 
                delaySequenceGenerator, millisecondsIntervalForPacketResend, cancellationToken);

            if (this.loggerFactory != null)
            {
                logger = this.loggerFactory.CreateLogger<InsecureServer>();
            }
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
            switch(PerformHandshake(client, tcpNetworkTransport, out string clientId))
            {
                case HandshakeResults.NewClientConnected:
                    InsecureClient insecureClient = new InsecureClient(tcpNetworkTransport, loggerFactory, serializer, orderingService,
                        cancellationToken, sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator, millisecondsIntervalForPacketResend);
                    clients.TryAdd(clientId, insecureClient);
                    OnClientConnected?.Invoke(insecureClient);
                    break;
                case HandshakeResults.ExsistingClientReconnected:
                    clients[clientId].ClientReconnected(tcpNetworkTransport);
                    break;
                case HandshakeResults.HandshakeFailed:
                    tcpNetworkTransport.DropConnection();
                    client.Close();
                    client.Dispose();
                    break;
            }
        }

        private void Stop()
        {
            tcpListener?.Stop();
        }
    }
}
