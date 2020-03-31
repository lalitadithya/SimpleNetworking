using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SimpleNetworking.Networking
{
    public class TcpNetworkTransport : NetworkTransport, ITcpNetworkTransport
    {
        private TcpClient tcpClient; 

        public TcpNetworkTransport(ILoggerFactory loggerFactory = null)
        {
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<TcpNetworkTransport>();
            }
        }

        internal TcpNetworkTransport(TcpClient tcpClient)
        {
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
    }
}
