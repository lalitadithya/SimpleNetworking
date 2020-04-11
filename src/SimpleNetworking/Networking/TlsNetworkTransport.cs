using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Networking
{
    public delegate bool ServerCertificateValidationCallback(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);

    public class TlsNetworkTransport : NetworkTransport
    {
        private readonly ServerCertificateValidationCallback serverCertificateValidationCallback;
        private readonly SslProtocols sslProtocols;
        private X509CertificateCollection clientCertificateCollection;

        public TlsNetworkTransport(CancellationToken cancellationToken, ILoggerFactory loggerFactory, 
            ServerCertificateValidationCallback serverCertificateValidationCallback, SslProtocols sslProtocols,
            X509CertificateCollection clientCertificateCollection)
        {
            Init(loggerFactory, cancellationToken);

            this.serverCertificateValidationCallback = serverCertificateValidationCallback;
            this.sslProtocols = sslProtocols;
            this.clientCertificateCollection = clientCertificateCollection;
        }

        internal TlsNetworkTransport(CancellationToken cancellationToken, TcpClient tcpClient, ILoggerFactory loggerFactory, SslStream sslStream)
        {
            Init(loggerFactory, cancellationToken, tcpClient, sslStream);
            StartReading();
        }

        [ExcludeFromCodeCoverage]
        public override void Connect(string hostname, int port)
        {
            TcpClient tcpClient = new TcpClient(hostname, port);

            SslStream sslStream = new SslStream(tcpClient.GetStream(), false, ValidateServerCertificate, null, EncryptionPolicy.RequireEncryption);
            try
            {
                sslStream.AuthenticateAsClient(hostname, clientCertificateCollection, sslProtocols, true);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Exception in AuthenticateAsClientAsync");
                tcpClient.Close();
                throw;
            }

            Init(tcpClient);
            SetStream(sslStream);
            StartReading();
        }

        [ExcludeFromCodeCoverage]
        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                if (serverCertificateValidationCallback != null)
                {
                    return serverCertificateValidationCallback(certificate, chain, sslPolicyErrors);
                }
                else
                {
                    return sslPolicyErrors == SslPolicyErrors.None;
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Exception in ValidateServerCertificate");
                return false;
            }
        }

    }
}
