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
        static string type;

        static void Main(string[] args)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            var serviceProvider = new ServiceCollection().AddLogging(opt => opt.AddConsole())
                                                         .BuildServiceProvider();
            ILoggerFactory factory = serviceProvider.GetService<ILoggerFactory>();
            Console.WriteLine("1 -> Client");
            Console.WriteLine("2 -> Server");
            int choice = int.Parse(Console.ReadLine());
            switch(choice)
            {
                case 1:
                    type = "client";
                    client = new InsecureClient(cts.Token, factory);
                    client.OnPacketReceived += Client_OnPacketReceived;
                    client.Connect("localhost", 9000, new JsonSerializer());
                    Console.WriteLine("Connect success");
                    break;
                case 2:
                    type = "sever";
                    InsecureServer server = new InsecureServer();
                    server.OnClientConnected += Server_OnClientConnected;
                    server.StartListening(IPAddress.Any, 9000, new JsonSerializer(), cts.Token);
                    break;
            }

            Console.ReadKey();
            cts.Cancel();
            Console.ReadKey();
        }

        private static void Client_OnPacketReceived(object data)
        {
            Task.Run(() =>
            {
                MyPacket packet = (MyPacket)data;
                Console.WriteLine("Got: " + packet.Data);
                client.SendData(new MyPacket
                {
                    Data = packet.Data.Contains("Ping") ? $"{type}: Pong" : $"{type}: Ping"
                });
            });
        }

        private static void Server_OnClientConnected(IInsecureClient client1)
        {
            Console.WriteLine("Client connected");
            client = client1;
            Task.Run(() =>
            {
                client.OnPacketReceived += Client_OnPacketReceived;
                client.SendData(new MyPacket
                {
                    Data = $"{type}: Ping"
                });
            });
        }
    }
}
