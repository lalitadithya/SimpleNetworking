using SimpleNetworking.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Client
{
    public class SecureClient : Client, ISecureClient
    {
        private ITlsNetworkTransport networkTransport;

        public void Connect(string hostname, long port, string pfxFilePath, string pfxFilePassword)
        {
            throw new NotImplementedException();
        }

        protected override Task Connect()
        {
            throw new NotImplementedException();
        }
    }
}
