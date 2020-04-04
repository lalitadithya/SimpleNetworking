using Microsoft.Extensions.Logging;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public class InsecureClient : Client, IInsecureClient
    {
        private readonly string id;
        private string hostName;
        private int port;

        public InsecureClient(ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000)
        {
            Init(loggerFactory, maximumPacketBacklog, expiryTime);
            id = Guid.NewGuid().ToString();
        }

        internal InsecureClient(TcpNetworkTransport tcpNetworkTransport, ISerializer serializer, ILoggerFactory loggerFactory = null, int maximumPacketBacklog = 1000, int expiryTime = 600000)
        {
            this.serializer = serializer;
            this.networkTransport = tcpNetworkTransport;
            networkTransport.OnDataReceived += DataReceived;

            Init(loggerFactory, maximumPacketBacklog, expiryTime);
        }

        public async void Connect(string hostName, int port, ISerializer serializer)
        {
            this.serializer = serializer;
            this.hostName = hostName;
            this.port = port;
            networkTransport = new TcpNetworkTransport();

            await Connect(hostName, port);
        }

        private async Task Connect(string hostName, int port)
        {
            ((ITcpNetworkTransport)networkTransport).Connect(hostName, port);
            networkTransport.OnDataReceived += DataReceived;
            networkTransport.OnConnectionLost += NetworkTransport_OnConnectionLost;
            await networkTransport.SendData(Encoding.Unicode.GetBytes(id));
        }

        public void ClientReconnected(TcpNetworkTransport networkTransport)
        {
            this.networkTransport = networkTransport;
            networkTransport.OnDataReceived += DataReceived;
        }

        private void NetworkTransport_OnConnectionLost()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Connect(hostName, port);
                        break;
                    }
                    catch (Exception e)
                    {
                        logger?.LogWarning(e, "Reconnection failed");
                        await Task.Delay(10 * 1000);
                    }
                }
            });
        }

        private void Init(ILoggerFactory loggerFactory, int maximumPacketBacklog, int expiryTime)
        {
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<InsecureClient>();
            }
            this.sendIdempotencyService = new SendIdempotencyService<Guid, Packet>(maximumPacketBacklog);
            this.receiveIdempotencyService = new ReceiveIdempotencyService<string>(expiryTime);
        }
    }
}
