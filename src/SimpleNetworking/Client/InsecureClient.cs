﻿using SimpleNetworking.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Client
{
    public class InsecureClient : Client, IInsecureClient
    {
        private ITcpNetworkTransport networkTransport;

        public void Connect(string hostName, long port)
        {
            throw new NotImplementedException();
        }
    }
}