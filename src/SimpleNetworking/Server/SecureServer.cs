using Microsoft.Extensions.Logging;
using SimpleNetworking.Client;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.OrderingService;
using SimpleNetworking.SequenceGenerator;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Server
{
    public delegate bool ClientCertificateValidationCallback(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);

    public class SecureServer : Server
    {
        private TcpListener tcpListener;

        private X509Certificate serverCertificate;
        private bool clientCertificateRequired;
        private SslProtocols sslProtocols;
        private ClientCertificateValidationCallback clientCertificateValidationCallback;

        public event ClientConnectedHandler OnClientConnected;

        public SecureServer(ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend, X509Certificate serverCertificate, bool clientCertificateRequired,
            SslProtocols sslProtocols, ClientCertificateValidationCallback clientCertificateValidationCallback, int keepAliveTimeOut,
            int maximumNumberOfKeepAliveMisses, int keepAliveResponseTimeOut)
        {
            Init(loggerFactory, serializer, orderingService,
                sendIdempotencyService, receiveIdempotencyService,
                delaySequenceGenerator, millisecondsIntervalForPacketResend, cancellationToken, keepAliveTimeOut,
            maximumNumberOfKeepAliveMisses, keepAliveResponseTimeOut);
            this.serverCertificate = serverCertificate;
            this.clientCertificateRequired = clientCertificateRequired;
            this.sslProtocols = sslProtocols;
            this.clientCertificateValidationCallback = clientCertificateValidationCallback;

            if (this.loggerFactory != null)
            {
                logger = this.loggerFactory.CreateLogger<SecureServer>();
            }
        }

        public void StartListening(IPAddress localAddress, int port)
        {
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
                    await ProcessClient(client);
                }
            });
        }

        private async Task ProcessClient(TcpClient client)
        {
            logger?.LogInformation("{0} connected", client.Client.RemoteEndPoint);

            SslStream sslStream = new SslStream(client.GetStream(), false, ValidateClientCertificate);

            try
            {
                await sslStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired, sslProtocols, true);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "AuthenticateAsServerAsync threw exeception");
                sslStream.Close();
                client.Close();
                return;
            }

            TlsNetworkTransport tlsNetworkTransport = new TlsNetworkTransport(cancellationToken, client, loggerFactory, sslStream);
            switch (PerformHandshake(client, tlsNetworkTransport, out string clientId))
            {
                case HandshakeResults.NewClientConnected:
                    SecureClient secureClient = new SecureClient(tlsNetworkTransport, loggerFactory, serializer, orderingService,
                        cancellationToken, sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator, millisecondsIntervalForPacketResend, keepAliveTimeOut,
                        maximumNumberOfKeepAliveMisses, keepAliveResponseTimeOut);
                    clients.TryAdd(clientId, secureClient);
                    await tlsNetworkTransport.SendData(Encoding.Unicode.GetBytes(Id));
                    OnClientConnected?.Invoke(secureClient);
                    break;
                case HandshakeResults.ExsistingClientReconnected:
                    clients[clientId].ClientReconnected(tlsNetworkTransport);
                    await tlsNetworkTransport.SendData(Encoding.Unicode.GetBytes(Id));
                    break;
                case HandshakeResults.HandshakeFailed:
                    tlsNetworkTransport.DropConnection();
                    client.Close();
                    client.Dispose();
                    break;
            }
        }

        private bool ValidateClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                if (clientCertificateValidationCallback != null)
                {
                    return clientCertificateValidationCallback(certificate, chain, sslPolicyErrors);
                }
                else
                {
                    if (clientCertificateRequired)
                    {
                        return sslPolicyErrors == SslPolicyErrors.None;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "ValidateClientCertificate threw an exception");
                return false;
            }
        }

        private void Stop()
        {
            tcpListener?.Stop();
        }
    }
}
