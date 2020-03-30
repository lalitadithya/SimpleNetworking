using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Models
{
    public class Packet
    {
        public Header PacketHeader { get; set; }
        public IPayload PacketPayload { get; set; }
    }
}
