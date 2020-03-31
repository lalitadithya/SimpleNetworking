using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Serializer
{
    public class JsonSerializer : ISerializer
    {
        public object Deserilize(byte[] data, Type objectType)
        {
            return (JsonConvert.DeserializeObject(Encoding.Unicode.GetString(data)) as JObject).ToObject(objectType);
        }

        public byte[] Serilize(object data)
        {
            return Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(data));
        }
    }
}
