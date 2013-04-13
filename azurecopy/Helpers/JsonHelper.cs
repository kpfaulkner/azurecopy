using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace azurecopy.Helpers
{
    public static class JsonHelper
    {
        public static string SerializeObjectToJson<T>(T obj)
        {
            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            ser.WriteObject(stream1, obj);

            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);
            var json = sr.ReadToEnd();
            return json;
        }

        public static T DeserializeJsonToObject<T>(string json)
        {
            json = json.Replace("\0", "");
            T obj;
            var byteArray = Encoding.Unicode.GetBytes(json);
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                var deserializer = new DataContractJsonSerializer(typeof(T));
                obj = (T)deserializer.ReadObject(stream);
            }
            return obj;

        }
    }
}
