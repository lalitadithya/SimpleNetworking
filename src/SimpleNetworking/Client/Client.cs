using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Client
{
    public abstract class Client : IClient
    {
        protected NetworkTransport networkTransport;

        public event PacketReceivedHandler OnPacketReceived;

        public void SendData(Packet packet)
        {
            throw new NotImplementedException();
        }
    }
}
