using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Networking
{
    public class TlsNetworkTransport : NetworkTransport, ITlsNetworkTransport
    {
        private ILoggerFactory loggerFactory;

        private TcpClient tcpClient;
        private ServerCertificateValidationCallback serverCertificateValidationCallback;
        private SslProtocols sslProtocols;

        public TlsNetworkTransport(CancellationToken cancellationToken, ILoggerFactory loggerFactory, ServerCertificateValidationCallback serverCertificateValidationCallback, SslProtocols sslProtocols)
        {
            this.cancellationToken = cancellationToken;
            this.loggerFactory = loggerFactory;
            this.serverCertificateValidationCallback = serverCertificateValidationCallback;
            this.sslProtocols = sslProtocols;

            cancellationToken.Register(() => Stop());
        }

        internal TlsNetworkTransport(CancellationToken cancellationToken, TcpClient tcpClient, ILoggerFactory loggerFactory, SslStream sslStream)
        {
            this.cancellationToken = cancellationToken;
            this.tcpClient = tcpClient;
            Initialize(loggerFactory);

            stream = sslStream;
            StartReading();
        }

        public async Task Connect(string hostname, int port)
        {
            Initialize(loggerFactory);
            tcpClient = new TcpClient(hostname, port);

            SslStream sslStream = new SslStream(tcpClient.GetStream(), false, ValidateServerCertificate, null, EncryptionPolicy.RequireEncryption);
            try
            {
                await sslStream.AuthenticateAsClientAsync(hostname, null, sslProtocols, true);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Exception in AuthenticateAsClientAsync");
                tcpClient.Close();
                throw;
            }

            stream = sslStream;
            StartReading();
        }

        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                serverCertificateValidationCallback(certificate, chain, sslPolicyErrors);
                return true;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Exception in ValidateServerCertificate");
                return false;
            }
        }

        private void Initialize(ILoggerFactory loggerFactory)
        {
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger<TcpNetworkTransport>();
            }
        }

        private void Stop()
        {
            DropConnection();
            tcpClient?.Close();
        }
    }
}
