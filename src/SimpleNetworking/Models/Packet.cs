using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SimpleNetworking.Models
{
    [ExcludeFromCodeCoverage]
    public class Packet
    {
        public Header PacketHeader { get; set; }
        public object PacketPayload { get; set; }
    }
}
