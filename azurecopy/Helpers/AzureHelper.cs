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
 
 using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using azurecopy.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace azurecopy.Utils
{
    public static class AzureHelper
    {

        public static string AzureStorageConnectionString { get; set; }

        static string AzureDetection = "windows.net";
        static string DevAzureDetection = "127.0.0.1";
        static CloudBlobClient BlobClient { get; set; }

        static AzureHelper()
        {
            BlobClient = null;
        }


        public static CloudBlobClient GetCloudBlobClient(string accountName, string accountKey)
        {
            if (BlobClient == null)
            {

                var credentials = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accountKey);
                CloudStorageAccount azureStorageAccount = new CloudStorageAccount(credentials, true);
                BlobClient = azureStorageAccount.CreateCloudBlobClient();
            }

            return BlobClient;
        }

        public static CloudBlobClient GetCloudBlobClient(string url )
        {
            if (BlobClient == null)
            {
                var accountName = GetAccountNameFromUrl(url);
                var accountKey = ConfigHelper.AzureAccountKey;

                var credentials = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accountKey);
                CloudStorageAccount azureStorageAccount = new CloudStorageAccount(credentials, true);
                BlobClient = azureStorageAccount.CreateCloudBlobClient();
            }

            return BlobClient;
        }


        // container lives in different part of url depending on dev or real.
        // if url ends in / then we assume its 
        public static string GetContainerFromUrl(string blobUrl)
        {
            var url = new Uri( blobUrl );
            string container = "";  // there may be no container.

            if (blobUrl.EndsWith("/"))
            {
                container = url.Segments[url.Segments.Length - 1];
            }
            else
            {

                // container will be second last segment of url.
                // length == 4 means BASE + ACCOUNT + CONTAINER + BLOB
                // length == 3 means BASE + CONTAINER + BLOB

                container = url.Segments[url.Segments.Length - 2];
            }

            container = container.TrimEnd('/');
            return container;
        }

        public static string GetBlobFromUrl(string blobUrl)
        {
            var url = new Uri(blobUrl);
            var blobName = "";

            blobName = url.Segments[url.Segments.Length - 1];

            return blobName;
        }

        public static string GetAccountNameFromUrl(string blobUrl)
        {
            Uri url = new Uri(blobUrl);
            var blobName = "";
            var account = url.Host.Split('.')[0];


            return account;
        }

        public static bool MatchHandler(string url)
        {
            return url.Contains(AzureDetection) || url.Contains(DevAzureDetection);
        }


    }
}
