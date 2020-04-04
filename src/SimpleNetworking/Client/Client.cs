using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SimpleNetworking.IdempotencyService;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
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
        protected ISimpleIdempotencyService<Guid, Packet> idempotencyService; 

        public event PacketReceivedHandler OnPacketReceived;

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
                await idempotencyService.Add(idempotencyToken, packet);
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
                    idempotencyService.Remove(Guid.Parse(packet.PacketHeader.IdempotencyToken), out _);
                    break;
                case Header.PacketTypes.Data:
                    SendAck(packet);
                    InvokeDataReceivedEvent(packet);
                    break;
            }
        }

        protected void StartPacketResend(int millisecondsInterval)
        {
            packetResendTimer = new Timer(ResendPacket, null, millisecondsInterval, millisecondsInterval);
        }

        protected void StopPacketResend()
        {
            packetResendTimer.Dispose();
        }

        private async void ResendPacket(object state)
        {
            foreach(var packet in idempotencyService.GetValues())
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
