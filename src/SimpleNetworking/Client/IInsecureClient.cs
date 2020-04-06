using SimpleNetworking.Networking;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Client
{
    public interface IInsecureClient : IClient
    {
        void Connect(string hostName, int port);
    }
}
