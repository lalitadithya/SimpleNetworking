using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
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
        private int millisecondsIntervalForPacketResend;

        protected NetworkTransport networkTransport;
        protected ISerializer serializer;
        protected ILogger logger;
        protected ISendIdempotencyService<Guid, Packet> sendIdempotencyService;
        protected IReceiveIdempotencyService<string> receiveIdempotencyService;
        protected ISequenceGenerator delaySequenceGenerator;
        protected CancellationToken cancellationToken;

        public event PacketReceivedHandler OnPacketReceived;

        protected abstract Task Connect();

        public async Task SendData(object payload)
        {
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

            switch(packet.PacketHeader.PacketType)
            {
                case Header.PacketTypes.Ack:
                    sendIdempotencyService.Remove(Guid.Parse(packet.PacketHeader.IdempotencyToken), out _);
                    break;
                case Header.PacketTypes.Data:
                    if (!receiveIdempotencyService.Find(packet.PacketHeader.IdempotencyToken))
                    {
                        receiveIdempotencyService.Add(packet.PacketHeader.IdempotencyToken);
                        SendAck(packet);
                        InvokeDataReceivedEvent(packet);
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
                    logger?.LogWarning(e, "Reconnection failed");
                    delayEnumerator.MoveNext();
                    await Task.Delay(delayEnumerator.Current);
                }
            }
        }

        protected void ClientDisconnected()
        {
            receiveIdempotencyService.PausePacketExpiry();
            StopPacketResend();
        }

        protected void ClientReconnected()
        {
            receiveIdempotencyService.ResumePacketExpiry();
            StartPacketResend(millisecondsIntervalForPacketResend);
        }

        protected void StartPacketResend(int millisecondsInterval)
        {
            millisecondsIntervalForPacketResend = millisecondsInterval;
            packetResendTimer = new Timer(ResendPacket, null, millisecondsInterval, millisecondsInterval);
        }

        protected void StopPacketResend()
        {
            packetResendTimer.Dispose();
        }

        private async void ResendPacket(object state)
        {
            foreach(var packet in sendIdempotencyService.GetValues())
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
    }
}
