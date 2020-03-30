using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Client
{
    public interface ISecureClient
    {
        void Connect(string hostname, long port, string pfxFilePath, string pfxFilePassword);
    }
}
