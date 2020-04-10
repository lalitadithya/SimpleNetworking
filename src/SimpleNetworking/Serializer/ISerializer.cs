using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Serializer
{
    public interface ISerializer
    {
        byte[] Serilize(Packet packet);
        Packet Deserilize(byte[] data);
    }
}
