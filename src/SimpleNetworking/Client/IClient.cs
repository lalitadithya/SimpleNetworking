using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public delegate void PacketReceivedHandler(object data);
    public delegate void PeerDeviceDisconnectedHandler();
    public delegate void PeerDeviceReconnectedHandler();
    public interface IClient
    {
        event PacketReceivedHandler OnPacketReceived;
        event PeerDeviceDisconnectedHandler OnPeerDeviceDisconnected;
        event PeerDeviceReconnectedHandler OnPeerDeviceReconnected;
        Task SendData(object packet);
    }
}
