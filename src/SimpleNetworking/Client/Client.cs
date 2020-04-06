using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.OrderingService;
using SimpleNetworking.SequenceGenerator;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public abstract class Client : IClient
    {
        private string hostName;
        private int port;

        private long sendSequenceNumber = 0;
        private readonly SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);
        private Timer packetResendTimer;

        protected string id;
        protected NetworkTransport networkTransport;
        protected ISerializer serializer;
        protected ILogger logger;
        protected ISendIdempotencyService<Guid, Packet> sendIdempotencyService;
        protected IReceiveIdempotencyService<string> receiveIdempotencyService;
        protected ISequenceGenerator delaySequenceGenerator;
        protected CancellationToken cancellationToken;
        protected IOrderingService orderingService;
        protected int millisecondsIntervalForPacketResend;
        protected ILoggerFactory loggerFactory;

        public event PacketReceivedHandler OnPacketReceived;
        public event PeerDeviceDisconnectedHandler OnPeerDeviceDisconnected;
        public event PeerDeviceReconnectedHandler OnPeerDeviceReconnected;

        protected abstract NetworkTransport GetNetworkTransport();

        public void ClientReconnected(NetworkTransport networkTransport)
        {
            this.networkTransport.DropConnection();

            InitNetworkTransport(networkTransport, false);
            ClientReconnected();
        }

        protected void Init(ILoggerFactory loggerFactory, ISerializer serializer, IOrderingService orderingService,
            CancellationToken cancellationToken, ISendIdempotencyService<Guid, Packet> sendIdempotencyService,
            IReceiveIdempotencyService<string> receiveIdempotencyService, ISequenceGenerator delaySequenceGenerator,
            int millisecondsIntervalForPacketResend)
        {
            this.loggerFactory = loggerFactory;
            this.sendIdempotencyService = sendIdempotencyService;
            this.receiveIdempotencyService = receiveIdempotencyService;
            this.cancellationToken = cancellationToken;
            this.millisecondsIntervalForPacketResend = millisecondsIntervalForPacketResend;
            this.serializer = serializer;
            this.orderingService = orderingService;
            this.delaySequenceGenerator = delaySequenceGenerator;

            this.cancellationToken.Register(() => Cancel());

            if (this.loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger(GetType());
            }
        }

        public async Task Connect(string hostName, int port)
        {
            this.hostName = hostName;
            this.port = port;
            await Connect(false);
        }

        protected async Task Connect(bool isReconnect)
        {
            networkTransport = GetNetworkTransport();
            networkTransport.Connect(hostName, port);
            await networkTransport.SendData(Encoding.Unicode.GetBytes(id));
            StartPacketResend(isReconnect);
        }

        public async Task SendData(object payload)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            await sendSemaphore.WaitAsync();
            try
            {
                Guid idempotencyToken = Guid.NewGuid();
                Packet packet = new Packet
                {
                    PacketHeader = new Header
                    {
                        PacketType = Header.PacketTypes.Data,
                        ClassType = payload.GetType().AssemblyQualifiedName.ToString(),
                        IdempotencyToken = idempotencyToken.ToString(),
                        SequenceNumber = Interlocked.Increment(ref sendSequenceNumber)
                    },
                    PacketPayload = payload
                };
                await sendIdempotencyService.Add(idempotencyToken, packet);
                await SendDataUsingTransport(packet);
            }
            finally
            {
                sendSemaphore.Release();
            }
        }

        private async Task SendDataUsingTransport(Packet packet)
        {
            await networkTransport.SendData(serializer.Serilize(packet));
        }

        protected void DataReceived(byte[] data)
        {
            Packet packet = (Packet)serializer.Deserilize(data, typeof(Packet));

            switch (packet.PacketHeader.PacketType)
            {
                case Header.PacketTypes.Ack:
                    sendIdempotencyService.Remove(Guid.Parse(packet.PacketHeader.IdempotencyToken), out _);
                    break;
                case Header.PacketTypes.Data:
                    if (!receiveIdempotencyService.Find(packet.PacketHeader.IdempotencyToken))
                    {
                        receiveIdempotencyService.Add(packet.PacketHeader.IdempotencyToken);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        SendAck(packet);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        List<Packet> orderedPackets = orderingService.GetNextPacket(packet).Result;
                        foreach (var orderedPacket in orderedPackets)
                        {
                            InvokeDataReceivedEvent(orderedPacket);
                        }
                    }
                    break;
            }
        }

        protected void Reconnect()
        {
            IEnumerator<int> delayEnumerator = delaySequenceGenerator.GetEnumerator();
            ClientDisconnected();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Connect(true).Wait();
                    ClientReconnected();
                    break;
                }
                catch (Exception e)
                {
                    logger?.LogWarning("Reconnection failed - {0}", e.Message);
                    delayEnumerator.MoveNext();
                    int millisecondsDelay = delayEnumerator.Current * 1000;
                    logger?.LogInformation("Will reconnect in {0} milliseconds", millisecondsDelay);
                    Task.Delay(millisecondsDelay).Wait();
                }
            }
        }

        protected void ClientDisconnected()
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                receiveIdempotencyService.PausePacketExpiry();
                StopPacketResend();
                RaisePeerDeviceDisconnected();
            }
        }

        protected void ClientReconnected()
        {
            receiveIdempotencyService.ResumePacketExpiry();
            StartPacketResend(true);
            RaisePeerDeviceReconnected();
        }

        protected void StartPacketResend(bool isReconnect)
        {
            if (isReconnect)
            {
                packetResendTimer = new Timer(ResendPacket, null, 0, millisecondsIntervalForPacketResend);
            }
            else
            {
                packetResendTimer = new Timer(ResendPacket, null, millisecondsIntervalForPacketResend, millisecondsIntervalForPacketResend);
            }
        }

        protected void StopPacketResend()
        {
            packetResendTimer?.Dispose();
        }

        private async void ResendPacket(object state)
        {
            foreach (var packet in sendIdempotencyService.GetValues())
            {
                await SendDataUsingTransport(packet);
            }
        }

        private async Task SendAck(Packet packet)
        {
            Packet ackPacket = new Packet
            {
                PacketHeader = new Header
                {
                    IdempotencyToken = packet.PacketHeader.IdempotencyToken,
                    PacketType = Header.PacketTypes.Ack,
                    SequenceNumber = packet.PacketHeader.SequenceNumber
                }
            };
            await SendDataUsingTransport(ackPacket);
        }

        private void InvokeDataReceivedEvent(Packet packet)
        {
            try
            {
                OnPacketReceived?.Invoke((packet.PacketPayload as JObject).ToObject(Type.GetType(packet.PacketHeader.ClassType)));
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, "OnPacketReceived threw an exception");
            }
        }

        protected void InitNetworkTransport(NetworkTransport networkTransport, bool canReconnect)
        {
            this.networkTransport = networkTransport;

            if (canReconnect)
            {
                networkTransport.OnDataReceived += DataReceived;
                networkTransport.OnConnectionLost += Reconnect;
            }
            else
            {
                networkTransport.OnDataReceived += DataReceived;
                networkTransport.OnConnectionLost += ClientDisconnected;
            }
        }

        private  void RaisePeerDeviceReconnected()
        {
            try
            {
                OnPeerDeviceReconnected?.Invoke();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "OnPeerDeviceReconnected threw exception");
            }
        }

        private void RaisePeerDeviceDisconnected()
        {
            try
            {
                OnPeerDeviceDisconnected?.Invoke();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "OnPeerDeviceReconnected threw exception");
            }
        }

        protected void Cancel()
        {
            networkTransport?.DropConnection();
            sendIdempotencyService?.Dispose();
            receiveIdempotencyService?.Dispose();
            orderingService?.Dispose();
            sendSemaphore?.Dispose();
            packetResendTimer?.Dispose();
        }
    }
}
