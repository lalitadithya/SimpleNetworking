using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public abstract class Client : IClient
    {
        protected NetworkTransport networkTransport;
        protected ISerializer serializer;

        public event PacketReceivedHandler OnPacketReceived;

        public async Task SendData(IPayload payload)
        {
            Packet packet = new Packet
            {
                PacketHeader = new Header
                {
                    ClassType = payload.GetType().ToString(),
                    IdempotencyToken = Guid.NewGuid().ToString(),
                    SequenceNumber = 0
                },
                PacketPayload = payload
            };
            await networkTransport.SendData(serializer.Serilize(packet));
        }
    }
}
