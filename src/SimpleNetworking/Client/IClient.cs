using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Client
{
    public delegate void PacketReceivedHandler(Packet packet);
    public interface IClient
    {
        event PacketReceivedHandler OnPacketReceived;
        void SendData(Packet packet);
    }
}
