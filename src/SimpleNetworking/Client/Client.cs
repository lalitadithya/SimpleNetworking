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
        private long sendSequenceNumber = 0;
        private readonly SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);
        private Timer packetResendTimer;

        protected NetworkTransport networkTransport;
        protected ISerializer serializer;
        protected ILogger logger;
        protected ISendIdempotencyService<Guid, Packet> sendIdempotencyService;
        protected IReceiveIdempotencyService<string> receiveIdempotencyService;
        protected ISequenceGenerator delaySequenceGenerator;
        protected CancellationToken cancellationToken;
        protected IOrderingService orderingService;
        protected int millisecondsIntervalForPacketResend;

        public event PacketReceivedHandler OnPacketReceived;
        public abstract event PeerDeviceDisconnectedHandler OnPeerDeviceDisconnected;
        public abstract event PeerDeviceReconnectedHandler OnPeerDeviceReconnected;

        protected abstract Task Connect();
        protected abstract void RaisePeerDeviceReconnected();
        protected abstract void RaisePeerDeviceDisconnected();

        public async Task SendData(object payload)
        {
            if(cancellationToken.IsCancellationRequested)
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
                        SendAck(packet);
                        List<Packet> orderedPackets = orderingService.GetNextPacket(packet).Result;
                        foreach (var orderedPacket in orderedPackets)
                        {
                            InvokeDataReceivedEvent(orderedPacket);
                        }
                    }
                    break;
            }
        }

        protected async Task Reconnect()
        {
            IEnumerator<int> delayEnumerator = delaySequenceGenerator.GetEnumerator();
            ClientDisconnected();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Connect();
                    ClientReconnected();
                    break;
                }
                catch (Exception e)
                {
                    logger?.LogWarning("Reconnection failed - {0}", e.Message);
                    delayEnumerator.MoveNext();
                    int millisecondsDelay = delayEnumerator.Current * 1000;
                    logger?.LogInformation("Will reconnect in {0} milliseconds", millisecondsDelay);
                    await Task.Delay(millisecondsDelay);
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
