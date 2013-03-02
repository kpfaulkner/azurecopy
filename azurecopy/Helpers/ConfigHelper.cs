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
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy.Helpers
{
    // reads the app.config for us.

    public static class ConfigHelper
    {
        public static string AzureAccountKey { get; set; }
        public static string AWSAccessKeyID {get;set;}
        public static string AWSSecretAccessKeyID { get; set; }

        static ConfigHelper()
        {
            ReadConfig();
        }


        private static  T GetConfigValue<T>(string key, T defaultValue)
        {
            if (ConfigurationManager.AppSettings.AllKeys.Contains(key))
            {
                TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)converter.ConvertFromString(ConfigurationManager.AppSettings.Get(key));
            }
            return defaultValue;
        }

        public static void ReadConfig()
        {
            AzureAccountKey = GetConfigValue<string>("AzureAccountKey", "");
            AWSAccessKeyID = GetConfigValue<string>("AWSAccessKeyID", "");
            AWSSecretAccessKeyID = GetConfigValue<string>("AWSSecretAccessKeyID", "");
        }

    }
}
