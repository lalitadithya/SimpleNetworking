using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public delegate void PacketReceivedHandler(Packet packet);
    public interface IClient
    {
        event PacketReceivedHandler OnPacketReceived;
        Task SendData(IPayload packet);
    }
}
