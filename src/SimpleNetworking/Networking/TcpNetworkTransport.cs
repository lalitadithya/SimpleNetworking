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
        private TcpClient tcpClient; 

        public TcpNetworkTransport(CancellationToken cancellationToken, ILoggerFactory loggerFactory = null)
        {
            this.cancellationToken = cancellationToken;
            cancellationToken.Register(() => Stop());
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<TcpNetworkTransport>();
            }
        }

        internal TcpNetworkTransport(CancellationToken cancellationToken, TcpClient tcpClient)
        {
            this.cancellationToken = cancellationToken;
            this.tcpClient = tcpClient;
            Initialize();
        }

        public void Connect(string hostName, int port)
        {
            tcpClient = new TcpClient(hostName, port);
            Initialize();
        }

        private void Initialize()
        {
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
