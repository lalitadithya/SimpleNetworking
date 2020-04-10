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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public class SecureClient : Client
    {
        private readonly ServerCertificateValidationCallback serverCertificateValidationCallback;
        private readonly SslProtocols sslProtocols;
        private X509CertificateCollection clientCertificateCollection;

        public SecureClient(ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend, ServerCertificateValidationCallback serverCertificateValidationCallback, SslProtocols sslProtocols,
            X509CertificateCollection clientCertificateCollection)
        {
            this.serverCertificateValidationCallback = serverCertificateValidationCallback;
            this.sslProtocols = sslProtocols;
            this.clientCertificateCollection = clientCertificateCollection;

            Init(loggerFactory, serializer, orderingService, cancellationToken,
                sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator,
                millisecondsIntervalForPacketResend);
        }

        internal SecureClient(TlsNetworkTransport tlsNetworkTransport, ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend)
        {
            Init(loggerFactory, serializer, orderingService, cancellationToken,
                sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator,
                millisecondsIntervalForPacketResend, tlsNetworkTransport);
        }

        protected override NetworkTransport GetNetworkTransport()
        {
            return new TlsNetworkTransport(CancellationToken, LoggerFactory, serverCertificateValidationCallback, sslProtocols, clientCertificateCollection);
        }
    }
}
