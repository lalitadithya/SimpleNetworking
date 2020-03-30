using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Client
{
    public abstract class Client : IClient
    {
        public event PacketReceivedHandler OnPacketReceived;

        public void SendData(Packet packet)
        {
            throw new NotImplementedException();
        }
    }
}
