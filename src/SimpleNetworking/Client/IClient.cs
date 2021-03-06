﻿using SimpleNetworking.Models;
using SimpleNetworking.Networking;
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

        void ClientReconnected(NetworkTransport networkTransport);
        Task SendData(object packet);
        Task Connect(string hostName, int port);
    }
}
