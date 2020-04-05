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
            TcpNetworkTransport tcpNetworkTransport = new TcpNetworkTransport(cancellationToken, client);
            AutoResetEvent handshakeCompleteEvent = new AutoResetEvent(false);

            string clientId = "";
            DataReceivedHandler handshakeHandler = (data) =>
            {
                clientId = Encoding.Unicode.GetString(data);
                handshakeCompleteEvent.Set();
            };

            tcpNetworkTransport.OnDataReceived += handshakeHandler;

            if (handshakeCompleteEvent.WaitOne(30 * 1000))
            {
                tcpNetworkTransport.OnDataReceived -= handshakeHandler;
                if (clients.ContainsKey(clientId))
                {
                    clients[clientId].ClientReconnected(tcpNetworkTransport);
                }
                else
                {
                    InsecureClient insecureClient = new InsecureClient(tcpNetworkTransport, loggerFactory, serializer, orderingService, 
                        cancellationToken, sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator, millisecondsIntervalForPacketResend);
                    clients.TryAdd(clientId, insecureClient);
                    OnClientConnected?.Invoke(insecureClient);
                }
            }
            else
            {
                tcpNetworkTransport.OnDataReceived -= handshakeHandler;
                tcpNetworkTransport.Dispose();
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
