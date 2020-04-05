using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Exceptions
{

    [Serializable]
    public class EndOfStreamReachedException : Exception
    {
        public EndOfStreamReachedException() { }
        public EndOfStreamReachedException(string message) : base(message) { }
        public EndOfStreamReachedException(string message, Exception inner) : base(message, inner) { }
        protected EndOfStreamReachedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
