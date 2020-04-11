using Microsoft.Extensions.Logging;
using SimpleNetworking.Client;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.OrderingService;
using SimpleNetworking.SequenceGenerator;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SimpleNetworking.Builder
{
    public class InsecureClientBuilder
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

        public InsecureClientBuilder WithLogger(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            return this;
        }

        public InsecureClientBuilder WithSerilizer(ISerializer serializer)
        {
            this.serializer = serializer;
            return this;
        }

        public InsecureClientBuilder WithCancellationToken(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            return this;
        }

        public InsecureClientBuilder WithPacketOrderingService(IOrderingService orderingService)
        {
            this.orderingService = orderingService;
            return this;
        }

        public InsecureClientBuilder WithSendIdempotencyService(ISendIdempotencyService<Guid, Packet> sendIdempotencyService)
        {
            this.sendIdempotencyService = sendIdempotencyService;
            return this;
        }

        public InsecureClientBuilder WithReceiveIdempotencyService(IReceiveIdempotencyService<string> receiveIdempotencyService)
        {
            this.receiveIdempotencyService = receiveIdempotencyService;
            return this;
        }

        public InsecureClientBuilder WithSequenceGenerator(ISequenceGenerator delaySequenceGenerator)
        {
            this.delaySequenceGenerator = delaySequenceGenerator;
            return this;
        }

        public InsecureClientBuilder WithIntervalForPacketResend(TimeSpan packetResendInterval)
        {
            this.millisecondsIntervalForPacketResend = (int)packetResendInterval.TotalMilliseconds;
            return this;
        }

        public InsecureClientBuilder WithKeepAliveTimeOut(TimeSpan keepAliveTimeOut)
        {
            this.keepAliveTimeOut = (int)keepAliveTimeOut.TotalMilliseconds;
            return this;
        }

        public InsecureClientBuilder WithKeepAliveResponseTimeOut(TimeSpan keepAliveResponseTimeOut)
        {
            this.keepAliveResponseTimeOut = (int)keepAliveResponseTimeOut.TotalMilliseconds;
            return this;
        }

        public InsecureClientBuilder WithMaximumNumberOfKeepAliveMisses(int maximumNumberOfKeepAliveMisses)
        {
            this.maximumNumberOfKeepAliveMisses = maximumNumberOfKeepAliveMisses;
            return this;
        }

        public InsecureClient Build()
        {
            return new InsecureClient(loggerFactory, serializer, orderingService, cancellationToken, sendIdempotencyService,
                receiveIdempotencyService, delaySequenceGenerator, millisecondsIntervalForPacketResend,
                keepAliveTimeOut, maximumNumberOfKeepAliveMisses, keepAliveResponseTimeOut);
        }
    }
}
