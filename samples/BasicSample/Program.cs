using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using SimpleNetworking.Client;
using SimpleNetworking.Serializer;
using SimpleNetworking.Server;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BasicSample
{
    public class MyPacket
    {
        public string Data { get; set; }
    }

    class Program
    {
        static IInsecureClient client;

        static void Main(string[] args)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            var serviceProvider = new ServiceCollection().AddLogging(opt => opt.AddConsole())
                                                         .BuildServiceProvider();
            ILoggerFactory factory = serviceProvider.GetService<ILoggerFactory>();
            Console.WriteLine("1 -> Client");
            Console.WriteLine("2 -> Server");
            int choice = int.Parse(Console.ReadLine());
            switch (choice)
            {
                case 1:
                    client = SimpleNetworking.Builder.Builder.InsecureClient.Build();

                    client.OnPacketReceived += Client_OnPacketReceived;
                    client.OnPeerDeviceDisconnected += Client_OnPeerDeviceDisconnected;
                    client.OnPeerDeviceReconnected += Client_OnPeerDeviceReconnected;
                    string ip = Console.ReadLine();
                    client.Connect(ip, 9000);
                    Console.WriteLine("Connect success");
                    Task.Run(() => SendNumbers());
                    break;
                case 2:
                    InsecureServer server = SimpleNetworking.Builder.Builder.InsecureServer.Build();

                    server.OnClientConnected += Server_OnClientConnected;
                    server.StartListening(IPAddress.Any, 9000, new JsonSerializer(), cts.Token);
                    Console.WriteLine("Server started");
                    break;
            }

            Console.ReadKey();
            cts.Cancel();
            Console.ReadKey();
        }

        private static void Client_OnPeerDeviceReconnected()
        {
            Console.WriteLine("Peer device reconnected");
        }

        private static void Client_OnPeerDeviceDisconnected()
        {
            Console.WriteLine("Peer device disconnected");
        }

        private static void SendNumbers()
        {
            int i = 0;
            while (true)
            {
                client.SendData(new MyPacket
                {
                    Data = $"{i++}"
                });
                Thread.Sleep(2 * 1000);
            }
        }

        private static void Client_OnPacketReceived(object data)
        {
            MyPacket packet = (MyPacket)data;
            Console.WriteLine("Got: " + packet.Data);
        }

        private static void Client_OnPacketReceived1(object data)
        {
            MyPacket packet = (MyPacket)data;
            Console.WriteLine("Got: " + packet.Data);
            client.SendData(new MyPacket
            {
                Data = $"{packet.Data}"
            });
        }

        private static void Server_OnClientConnected(IInsecureClient client1)
        {
            Console.WriteLine("Client connected");
            client = client1;
            client.OnPacketReceived += Client_OnPacketReceived1;
            client.OnPeerDeviceDisconnected += Client_OnPeerDeviceDisconnected;
            client.OnPeerDeviceReconnected += Client_OnPeerDeviceReconnected;
        }
    }
}
