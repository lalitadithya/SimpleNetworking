using Microsoft.Extensions.Logging;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.OrderingService;
using SimpleNetworking.SequenceGenerator;
using SimpleNetworking.Serializer;
using SimpleNetworking.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SimpleNetworking.Builder
{
    public class InsecureServerBuilder
    {
        private int millisecondsIntervalForPacketResend = 60 * 1000;

        private ILoggerFactory loggerFactory;
        private ISerializer serializer = new JsonSerializer();
        private CancellationToken cancellationToken = CancellationToken.None;
        private IOrderingService orderingService = new SimplePacketOrderingService();
        private ISendIdempotencyService<Guid, Packet> sendIdempotencyService = new SendIdempotencyService<Guid, Packet>(maximumPacketBacklog);
        private IReceiveIdempotencyService<string> receiveIdempotencyService = new ReceiveIdempotencyService<string>(expiryTime);
        private ISequenceGenerator delaySequenceGenerator = new ExponentialSequenceGenerator(maximumBackoffTime);

        private static readonly int maximumPacketBacklog = 1000;
        private static readonly int expiryTime = 600000;
        private static readonly int maximumBackoffTime = 60 * 1000;

        public InsecureServerBuilder WithLogger(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            return this;
        }

        public InsecureServerBuilder WithSerilizer(ISerializer serializer)
        {
            this.serializer = serializer;
            return this;
        }

        public InsecureServerBuilder WithCancellationToken(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            return this;
        }

        public InsecureServerBuilder WithPacketOrderingService(IOrderingService orderingService)
        {
            this.orderingService = orderingService;
            return this;
        }

        public InsecureServerBuilder WithSendIdempotencyService(ISendIdempotencyService<Guid, Packet> sendIdempotencyService)
        {
            this.sendIdempotencyService = sendIdempotencyService;
            return this;
        }

        public InsecureServerBuilder WithReceiveIdempotencyService(IReceiveIdempotencyService<string> receiveIdempotencyService)
        {
            this.receiveIdempotencyService = receiveIdempotencyService;
            return this;
        }

        public InsecureServerBuilder WithSequenceGenerator(ISequenceGenerator delaySequenceGenerator)
        {
            this.delaySequenceGenerator = delaySequenceGenerator;
            return this;
        }

        public InsecureServerBuilder WithIntervalForPacketResend(TimeSpan packetResendInterval)
        {
            this.millisecondsIntervalForPacketResend = (int)packetResendInterval.TotalMilliseconds;
            return this;
        }

        public InsecureServer Build()
        {
            return new InsecureServer(loggerFactory, serializer, orderingService, cancellationToken, sendIdempotencyService,
                receiveIdempotencyService, delaySequenceGenerator, millisecondsIntervalForPacketResend);
        }
    }
}
