using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleNetworking.Client;
using SimpleNetworking.Server;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace SuperSecureSample
{
    public class MyPacket
    {
        public string Data { get; set; }
    }

    class Program
    {
        static Client client;

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
                    X509Certificate2Collection clientCollection = new X509Certificate2Collection();
                    clientCollection.Import("ClientCertificate.pfx", "password", X509KeyStorageFlags.PersistKeySet);

                    client = SimpleNetworking.Builder.Builder.SecureClient
                        .WithLogger(factory)
                        .WithCancellationToken(cts.Token)
                        .Build(ServerCertificateValidationCallback, clientCollection);

                    client.OnPacketReceived += Client_OnPacketReceived;
                    client.OnPeerDeviceDisconnected += Client_OnPeerDeviceDisconnected;
                    client.OnPeerDeviceReconnected += Client_OnPeerDeviceReconnected;
                    string ip = Console.ReadLine();
                    client.Connect(ip, 9000).Wait();
                    Console.WriteLine("Connect success");
                    Task.Run(async () => await SendNumbers());
                    break;
                case 2:
                    X509Certificate2Collection serverCollection = new X509Certificate2Collection();
                    serverCollection.Import("ServerCertificate.pfx", "password", X509KeyStorageFlags.PersistKeySet);

                    SecureServer server = SimpleNetworking.Builder.Builder.SecureServer
                        .WithLogger(factory)
                        .WithCancellationToken(cts.Token)
                        .Build(serverCollection[0], true, ClientCertificateValidationCallback);

                    server.OnClientConnected += Server_OnClientConnected;
                    server.StartListening(IPAddress.Any, 9000);
                    Console.WriteLine("Server started");
                    break;
            }

            Console.ReadKey();
            cts.Cancel();
            Console.ReadKey();
        }

        private static bool ServerCertificateValidationCallback(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static bool ClientCertificateValidationCallback(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static void Client_OnPeerDeviceReconnected()
        {
            Console.WriteLine("Peer device reconnected");
        }

        private static void Client_OnPeerDeviceDisconnected()
        {
            Console.WriteLine("Peer device disconnected");
        }

        private static async Task SendNumbers()
        {
            int i = 0;
            while (true)
            {
                await client.SendData(new MyPacket
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

        private static async void Client_OnPacketReceived1(object data)
        {
            MyPacket packet = (MyPacket)data;
            Console.WriteLine("Got: " + packet.Data);
            await client.SendData(new MyPacket
            {
                Data = $"{packet.Data}"
            });
        }

        private static void Server_OnClientConnected(Client client1)
        {
            Console.WriteLine("Client connected");
            client = client1;
            client.OnPacketReceived += Client_OnPacketReceived1;
            client.OnPeerDeviceDisconnected += Client_OnPeerDeviceDisconnected;
            client.OnPeerDeviceReconnected += Client_OnPeerDeviceReconnected;
        }
    }
}
