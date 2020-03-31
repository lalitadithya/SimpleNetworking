using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SimpleNetworking.Networking
{
    public class TcpNetworkTransport : NetworkTransport, ITcpNetworkTransport
    {
        private TcpClient tcpClient; 

        public void Connect(string hostName, int port)
        {
            tcpClient = new TcpClient(hostName, port);
            stream = tcpClient.GetStream();
            StartReading();
        }
    }
}
