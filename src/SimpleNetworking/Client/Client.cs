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
        private SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);

        protected NetworkTransport networkTransport;
        protected ISerializer serializer;

        public event PacketReceivedHandler OnPacketReceived;

        public async Task SendData(object payload)
        {
            await sendSemaphore.WaitAsync();
            try
            {
                Packet packet = new Packet
                {
                    PacketHeader = new Header
                    {
                        ClassType = payload.GetType().ToString(),
                        IdempotencyToken = Guid.NewGuid().ToString(),
                        SequenceNumber = Interlocked.Increment(ref sendSequenceNumber)
                    },
                    PacketPayload = payload
                };
                await networkTransport.SendData(serializer.Serilize(packet));
            }
            finally
            {
                sendSemaphore.Release();
            }
        }

        protected void DataReceived(byte[] data)
        {
            Packet packet = (Packet)serializer.Deserilize(data, typeof(Packet));
            OnPacketReceived?.Invoke(packet.PacketPayload);
        }
    }
}
