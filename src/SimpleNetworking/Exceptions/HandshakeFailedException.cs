using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SimpleNetworking.Exceptions
{

    [Serializable]
    [ExcludeFromCodeCoverage]
    public class HandshakeFailedException : Exception
    {
        public HandshakeFailedException() { }
        public HandshakeFailedException(string message) : base(message) { }
        public HandshakeFailedException(string message, Exception inner) : base(message, inner) { }
        protected HandshakeFailedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
