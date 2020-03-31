using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public delegate void PacketReceivedHandler(object data);
    public interface IClient
    {
        event PacketReceivedHandler OnPacketReceived;
        Task SendData(object packet);
    }
}
