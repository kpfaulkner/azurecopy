﻿//-----------------------------------------------------------------------
// <copyright >
//    Copyright 2013 Ken Faulkner
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

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
