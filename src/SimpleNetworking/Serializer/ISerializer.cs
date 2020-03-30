using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Serializer
{
    public interface ISerializer
    {
        byte[] Serilize(object data);
        object Deserilize<T>(byte[] data);
    }
}
