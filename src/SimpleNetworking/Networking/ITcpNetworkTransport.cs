using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Networking
{
    public interface ITcpNetworkTransport 
    {
        void Connect(string hostName, int port);
    }
}
