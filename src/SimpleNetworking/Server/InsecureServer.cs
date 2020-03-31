using SimpleNetworking.Client;
using SimpleNetworking.Networking;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Server
{
    public class InsecureServer : IInsecureServer
    {
        private TcpListener tcpListener;
        private CancellationToken cancellationToken;
        private ISerializer serializer;

        public event ClientConnectedHandler OnClientConnected;

        public void StartListening(IPAddress localAddress, int port, ISerializer serializer, CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            this.serializer = serializer;

            tcpListener = new TcpListener(localAddress, port);
            AcceptLoop();
        }

        private void AcceptLoop()
        {
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await tcpListener.AcceptTcpClientAsync();
                    OnClientConnected?.Invoke(new InsecureClient(new TcpNetworkTransport(client), serializer));
                }
            });
        }
    }
}
