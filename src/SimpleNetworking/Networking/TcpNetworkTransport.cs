using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleNetworking.Networking
{
    public class TcpNetworkTransport : NetworkTransport, ITcpNetworkTransport
    {
        private ILoggerFactory loggerFactory;

        private TcpClient tcpClient; 

        public TcpNetworkTransport(CancellationToken cancellationToken, ILoggerFactory loggerFactory)
        {
            this.cancellationToken = cancellationToken;
            this.loggerFactory = loggerFactory;

            cancellationToken.Register(() => Stop());
        }

        internal TcpNetworkTransport(CancellationToken cancellationToken, TcpClient tcpClient, ILoggerFactory loggerFactory)
        {
            this.cancellationToken = cancellationToken;
            this.tcpClient = tcpClient;
            Initialize(loggerFactory);
        }

        public void Connect(string hostName, int port)
        {
            tcpClient = new TcpClient(hostName, port);
            Initialize(loggerFactory);
        }

        private void Initialize(ILoggerFactory loggerFactory)
        {
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<TcpNetworkTransport>();
            }

            stream = tcpClient.GetStream();
            StartReading();
        }

        private void Stop()
        {
            DropConnection();
            tcpClient?.Close();
        }
    }
}
