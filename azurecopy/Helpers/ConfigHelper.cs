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
        public static string AzureConnectionString { get; set; }
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
            AzureConnectionString = GetConfigValue<string>("AzureConnectionString", "UseDevelopmentStorage=true");
            AWSAccessKeyID = GetConfigValue<string>("AWSAccessKeyID", "");
            AWSSecretAccessKeyID = GetConfigValue<string>("AWSSecretAccessKeyID", "");
        }

    }
}
