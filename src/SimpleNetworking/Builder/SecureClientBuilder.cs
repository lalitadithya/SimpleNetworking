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
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace SimpleNetworking.Builder
{
    public class SecureClientBuilder
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

        public SecureClientBuilder WithLogger(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            return this;
        }

        public SecureClientBuilder WithSerilizer(ISerializer serializer)
        {
            this.serializer = serializer;
            return this;
        }

        public SecureClientBuilder WithCancellationToken(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            return this;
        }

        public SecureClientBuilder WithPacketOrderingService(IOrderingService orderingService)
        {
            this.orderingService = orderingService;
            return this;
        }

        public SecureClientBuilder WithSendIdempotencyService(ISendIdempotencyService<Guid, Packet> sendIdempotencyService)
        {
            this.sendIdempotencyService = sendIdempotencyService;
            return this;
        }

        public SecureClientBuilder WithReceiveIdempotencyService(IReceiveIdempotencyService<string> receiveIdempotencyService)
        {
            this.receiveIdempotencyService = receiveIdempotencyService;
            return this;
        }

        public SecureClientBuilder WithSequenceGenerator(ISequenceGenerator delaySequenceGenerator)
        {
            this.delaySequenceGenerator = delaySequenceGenerator;
            return this;
        }

        public SecureClientBuilder WithIntervalForPacketResend(TimeSpan packetResendInterval)
        {
            this.millisecondsIntervalForPacketResend = (int)packetResendInterval.TotalMilliseconds;
            return this;
        }

        public SecureClientBuilder WithKeepAliveTimeOut(TimeSpan keepAliveTimeOut)
        {
            this.keepAliveTimeOut = (int)keepAliveTimeOut.TotalMilliseconds;
            return this;
        }

        public SecureClientBuilder WithKeepAliveResponseTimeOut(TimeSpan keepAliveResponseTimeOut)
        {
            this.keepAliveResponseTimeOut = (int)keepAliveResponseTimeOut.TotalMilliseconds;
            return this;
        }

        public SecureClientBuilder WithMaximumNumberOfKeepAliveMisses(int maximumNumberOfKeepAliveMisses)
        {
            this.maximumNumberOfKeepAliveMisses = maximumNumberOfKeepAliveMisses;
            return this;
        }

        public SecureClient Build(ServerCertificateValidationCallback serverCertificateValidationCallback = null,
            X509CertificateCollection clientCertificateCollection = null, SslProtocols sslProtocols = SslProtocols.Tls12)
        {
            return new SecureClient(loggerFactory, serializer, orderingService, cancellationToken, sendIdempotencyService,
                receiveIdempotencyService, delaySequenceGenerator, millisecondsIntervalForPacketResend, serverCertificateValidationCallback,
                sslProtocols, clientCertificateCollection, keepAliveTimeOut, maximumNumberOfKeepAliveMisses, keepAliveResponseTimeOut);
        }
    }
}
