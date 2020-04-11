using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleNetworking.Networking
{
    public class TcpNetworkTransport : NetworkTransport
    {
        public TcpNetworkTransport(CancellationToken cancellationToken, ILoggerFactory loggerFactory)
        {
            Init(loggerFactory, cancellationToken);
        }

        internal TcpNetworkTransport(CancellationToken cancellationToken, TcpClient tcpClient, ILoggerFactory loggerFactory)
        {
            Init(loggerFactory, cancellationToken, tcpClient);
            StartReading();
        }

        [ExcludeFromCodeCoverage]
        public override void Connect(string hostName, int port)
        {
            Init(new TcpClient(hostName, port));
            SetStream();
            StartReading();
        }

    }
}
