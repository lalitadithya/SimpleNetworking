using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Networking
{
    public delegate bool ServerCertificateValidationCallback(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);
    public interface ITlsNetworkTransport
    {
        Task Connect(string hostname, int port);
    }
}
