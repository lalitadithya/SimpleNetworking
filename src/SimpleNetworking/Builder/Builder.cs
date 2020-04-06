﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Builder
{
    public static class Builder
    {
        public static InsecureClientBuilder InsecureClient
        {
            get
            {
                return new InsecureClientBuilder();
            }
        }

        public static InsecureServerBuilder InsecureServer
        {
            get
            {
                return new InsecureServerBuilder();
            }
        }
    }
}