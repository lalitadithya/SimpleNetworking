﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SimpleNetworking.Exceptions
{

    [Serializable]
    [ExcludeFromCodeCoverage]
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
