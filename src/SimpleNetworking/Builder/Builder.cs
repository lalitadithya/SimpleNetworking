using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SimpleNetworking.Builder
{
    [ExcludeFromCodeCoverage]
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

        public static SecureClientBuilder SecureClient
        {
            get
            {
                return new SecureClientBuilder();
            }
        }

        public static SecureServerBuilder SecureServer
        {
            get
            {
                return new SecureServerBuilder();
            }
        }
    }
}
