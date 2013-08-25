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
        // default values read from config.
        public static string AzureAccountKey { get; set; }
        public static string AWSAccessKeyID {get;set;}
        public static string AWSSecretAccessKeyID { get; set; }

        public static string SrcAzureAccountKey { get; set; }
        public static string SrcAWSAccessKeyID { get; set; }
        public static string SrcAWSSecretAccessKeyID { get; set; }

        public static string TargetAzureAccountKey { get; set; }
        public static string TargetAWSAccessKeyID { get; set; }
        public static string TargetAWSSecretAccessKeyID { get; set; }

        // retry attempt details
        public static int RetryAttemptDelayInSeconds {get;set;}
        public static int MaxRetryAttempts { get; set; }

        // misc params
        public static string DownloadDirectory  { get; set; }
        public static bool Verbose  { get; set; }
        public static bool AmDownloading  { get; set; }
        public static bool UseBlobCopy  { get; set; }
        public static bool ListContainer  { get; set; }
        public static bool MonitorBlobCopy  { get; set; }
        public static int ParallelFactor  { get; set; }
        public static int ChunkSizeInMB  { get; set; }

        // destination blob...  can only assign if source is NOT azure and destination IS azure.
        public static DestinationBlobType DestinationBlobTypeSelected {get;set;}

        public static string SkyDriveCode { get; set; }
        public static string SkyDriveRefreshToken { get; set; }
        public static string SkyDriveAccessToken { get; set; }

        // dropbox
        public static string DropBoxAPIKey { get; set; }
        public static string DropBoxAPISecret { get; set; }

        // sharepoint
        public static string SharepointUsername { get; set; }
        public static string SharepointPassword { get; set; }


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

            SrcAzureAccountKey = GetConfigValue<string>("SrcAzureAccountKey", AzureAccountKey);
            SrcAWSAccessKeyID = GetConfigValue<string>("SrcAWSAccessKeyID", AWSAccessKeyID);
            SrcAWSSecretAccessKeyID = GetConfigValue<string>("SrcAWSSecretAccessKeyID", AWSSecretAccessKeyID);

            TargetAzureAccountKey = GetConfigValue<string>("TargetAzureAccountKey", AzureAccountKey);
            TargetAWSAccessKeyID = GetConfigValue<string>("TargetAWSAccessKeyID", AWSAccessKeyID);
            TargetAWSSecretAccessKeyID = GetConfigValue<string>("TargetAWSSecretAccessKeyID", AWSSecretAccessKeyID);

            // retry policies.
            // can be used in both Azure and AWS (eventually).
            RetryAttemptDelayInSeconds = GetConfigValue<int>("RetryAttemptDelayInSeconds", 2);
            MaxRetryAttempts = GetConfigValue<int>("MaxRetryAttempts", 10);


            DownloadDirectory = GetConfigValue<string>("DownloadDirectory", "c:\\temp");
            Verbose = GetConfigValue<bool>("Verbose", false);
            AmDownloading = GetConfigValue<bool>("AmDownloading", false);
            UseBlobCopy = GetConfigValue<bool>("UseBlobCopy", false);
            ListContainer = GetConfigValue<bool>("ListContainer", false);
            MonitorBlobCopy = GetConfigValue<bool>("MonitorBlobCopy", false);
            ParallelFactor = GetConfigValue<int>("ParallelFactor", 1);
            ChunkSizeInMB = GetConfigValue<int>("ChunkSizeInMB", 2);

            DestinationBlobTypeSelected = GetConfigValue<DestinationBlobType>("DestinationBlobTypeSelected", DestinationBlobType.Unknown);

            SkyDriveCode = GetConfigValue<string>("SkyDriveCode", "");
            SkyDriveRefreshToken = GetConfigValue<string>("SkyDriveRefreshToken", "");

            // dropbox
            DropBoxAPIKey = GetConfigValue<string>("DropBoxAPIKey", "");
            DropBoxAPISecret = GetConfigValue<string>("DropBoxAPISecret", "");

            // sharepoint
            SharepointUsername = GetConfigValue<string>("SharepointUsername", "");
            SharepointPassword = GetConfigValue<string>("SharepointPassword", "");


        }
    }
}
