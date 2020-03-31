using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Serializer
{
    public class JsonSerializer : ISerializer
    {
        public object Deserilize<T>(byte[] data)
        {
            return (T)JsonConvert.DeserializeObject(Encoding.Unicode.GetString(data));
        }

        public byte[] Serilize(object data)
        {
            return Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(data));
        }
    }
}
