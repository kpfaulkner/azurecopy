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

namespace TestUtils
{
    // reads the app.config for us.

    public static class ConfigHelper
    {
        // default values read from config.
        public static string AzureAccountKey { get; set; }
        public static string AWSAccessKeyID {get;set;}
        public static string AWSSecretAccessKeyID { get; set; }

        public static string AzureBaseUrl { get; set; }
        public static string S3BaseUrl { get; set; }

        // dropbox
        public static string DropBoxAPIKey { get; set; }
        public static string DropBoxAPISecret { get; set; }

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

        // populates src and target values IF there is a default set.
        public static void ReadConfig()
        {
            AzureAccountKey = GetConfigValue<string>("AzureAccountKey", "");
            AWSAccessKeyID = GetConfigValue<string>("AWSAccessKeyID", "");
            AWSSecretAccessKeyID = GetConfigValue<string>("AWSSecretAccessKeyID", "");

            AzureBaseUrl = GetConfigValue<string>("AzureBaseUrl", "");
            S3BaseUrl = GetConfigValue<string>("S3BaseUrl", "");

            // dropbox
            DropBoxAPIKey = GetConfigValue<string>("DropBoxAPIKey", "");
            DropBoxAPISecret = GetConfigValue<string>("DropBoxAPISecret", "");

        }
    }
}
