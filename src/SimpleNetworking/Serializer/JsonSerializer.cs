using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Serializer
{
    public class JsonSerializer : ISerializer
    {
        public object Deserilize<T>(byte[] data)
        {
            throw new NotImplementedException();
        }

        public byte[] Serilize(object data)
        {
            throw new NotImplementedException();
        }
    }
}
