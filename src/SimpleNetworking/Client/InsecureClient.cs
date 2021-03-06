﻿using Microsoft.Extensions.Logging;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.OrderingService;
using SimpleNetworking.SequenceGenerator;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public class InsecureClient : Client
    {
        public InsecureClient(ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend, int keepAliveTimeOut,
            int maximumNumberOfKeepAliveMisses, int keepAliveResponseTimeOut)
        {
            Init(loggerFactory, serializer, orderingService, cancellationToken, 
                sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator, 
                millisecondsIntervalForPacketResend, keepAliveTimeOut, maximumNumberOfKeepAliveMisses, keepAliveResponseTimeOut);
        }

        internal InsecureClient(TcpNetworkTransport tcpNetworkTransport, ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend, int keepAliveTimeOut,
            int maximumNumberOfKeepAliveMisses, int keepAliveResponseTimeOut)
        {
            Init(loggerFactory, serializer, orderingService, cancellationToken,
                sendIdempotencyService, receiveIdempotencyService, delaySequenceGenerator,
                millisecondsIntervalForPacketResend, tcpNetworkTransport,
                keepAliveTimeOut, maximumNumberOfKeepAliveMisses, keepAliveResponseTimeOut);
        }

        protected override NetworkTransport GetNetworkTransport()
        {
            return new TcpNetworkTransport(CancellationToken, LoggerFactory);
        }
    }
}
