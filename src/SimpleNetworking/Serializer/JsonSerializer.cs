using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SimpleNetworking.Serializer
{
    [ExcludeFromCodeCoverage]
    public class JsonSerializer : ISerializer
    {
        public Packet Deserilize(byte[] data)
        {
            Packet packet = JsonConvert.DeserializeObject<Packet>(Encoding.Unicode.GetString(data));
            if (!string.IsNullOrWhiteSpace(packet.PacketHeader.ClassType))
            {
                packet.PacketPayload = (packet.PacketPayload as JObject).ToObject(Type.GetType(packet.PacketHeader.ClassType));
            }
            return packet;
        }

        public byte[] Serilize(Packet packet)
        {
            return Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(packet));
        }
    }
}
