using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SimpleNetworking.Models
{
    [ExcludeFromCodeCoverage]
    public class Header
    {
        public enum PacketTypes { Data, Ack};
        public PacketTypes PacketType { get; set; }
        public long SequenceNumber { get; set; }
        public string IdempotencyToken { get; set; }
        public string ClassType { get; set; }
    }
}
