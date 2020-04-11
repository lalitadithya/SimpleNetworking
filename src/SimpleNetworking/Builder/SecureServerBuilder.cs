using Microsoft.Extensions.Logging;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.OrderingService;
using SimpleNetworking.SequenceGenerator;
using SimpleNetworking.Serializer;
using SimpleNetworking.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace SimpleNetworking.Builder
{
    [ExcludeFromCodeCoverage]
    public class SecureServerBuilder
    {
        private int millisecondsIntervalForPacketResend = 60 * 1000;

        private ILoggerFactory loggerFactory;
        private ISerializer serializer = new JsonSerializer();
        private CancellationToken cancellationToken = CancellationToken.None;
        private IOrderingService orderingService = new SimplePacketOrderingService();
        private ISendIdempotencyService<Guid, Packet> sendIdempotencyService = new SendIdempotencyService<Guid, Packet>(maximumPacketBacklog);
        private IReceiveIdempotencyService<string> receiveIdempotencyService = new ReceiveIdempotencyService<string>(expiryTime);
        private ISequenceGenerator delaySequenceGenerator = new ExponentialSequenceGenerator(maximumBackoffTime);
        private int keepAliveTimeOut = 60 * 1000;
        private int maximumNumberOfKeepAliveMisses = 5;
        private int keepAliveResponseTimeOut = 500;

        private static readonly int maximumPacketBacklog = 1000;
        private static readonly int expiryTime = 600000;
        private static readonly int maximumBackoffTime = 60 * 1000;

        public SecureServerBuilder WithLogger(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            return this;
        }

        public SecureServerBuilder WithSerilizer(ISerializer serializer)
        {
            this.serializer = serializer;
            return this;
        }

        public SecureServerBuilder WithCancellationToken(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            return this;
        }

        public SecureServerBuilder WithPacketOrderingService(IOrderingService orderingService)
        {
            this.orderingService = orderingService;
            return this;
        }

        public SecureServerBuilder WithSendIdempotencyService(ISendIdempotencyService<Guid, Packet> sendIdempotencyService)
        {
            this.sendIdempotencyService = sendIdempotencyService;
            return this;
        }

        public SecureServerBuilder WithReceiveIdempotencyService(IReceiveIdempotencyService<string> receiveIdempotencyService)
        {
            this.receiveIdempotencyService = receiveIdempotencyService;
            return this;
        }

        public SecureServerBuilder WithSequenceGenerator(ISequenceGenerator delaySequenceGenerator)
        {
            this.delaySequenceGenerator = delaySequenceGenerator;
            return this;
        }

        public SecureServerBuilder WithIntervalForPacketResend(TimeSpan packetResendInterval)
        {
            this.millisecondsIntervalForPacketResend = (int)packetResendInterval.TotalMilliseconds;
            return this;
        }

        public SecureServerBuilder WithKeepAliveTimeOut(TimeSpan keepAliveTimeOut)
        {
            this.keepAliveTimeOut = (int)keepAliveTimeOut.TotalMilliseconds;
            return this;
        }

        public SecureServerBuilder WithKeepAliveResponseTimeOut(TimeSpan keepAliveResponseTimeOut)
        {
            this.keepAliveResponseTimeOut = (int)keepAliveResponseTimeOut.TotalMilliseconds;
            return this;
        }

        public SecureServerBuilder WithMaximumNumberOfKeepAliveMisses(int maximumNumberOfKeepAliveMisses)
        {
            this.maximumNumberOfKeepAliveMisses = maximumNumberOfKeepAliveMisses;
            return this;
        }

        public SecureServer Build(X509Certificate serverCertificate, bool clientCertificateRequired = false,
            ClientCertificateValidationCallback clientCertificateValidationCallback = null, SslProtocols sslProtocols = SslProtocols.Tls12)
        {
            return new SecureServer(loggerFactory, serializer, orderingService, cancellationToken, sendIdempotencyService,
                receiveIdempotencyService, delaySequenceGenerator, millisecondsIntervalForPacketResend, serverCertificate, clientCertificateRequired,
                sslProtocols, clientCertificateValidationCallback, keepAliveTimeOut, maximumNumberOfKeepAliveMisses, keepAliveResponseTimeOut);
        }
    }
}
