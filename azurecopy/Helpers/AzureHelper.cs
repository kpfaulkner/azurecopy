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
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace azurecopy.Utils
{
    public static class AzureHelper
    {

        public static string AzureStorageConnectionString { get; set; }

        const string AzureDetection = "windows.net";
        const string DevAzureDetection = "127.0.0.1";
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

        public static CloudBlobClient GetSourceCloudBlobClient(string url)
        {
            return GetCloudBlobClient(url, true);
        }


        public static CloudBlobClient GetTargetCloudBlobClient(string url)
        {
            return GetCloudBlobClient(url, false);

        }


        public static CloudBlobClient GetCloudBlobClient(string url, bool isSrc )
        {
            if (BlobClient == null)
            {
                if (IsDevUrl(url))
                {
                   
                    CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
                    BlobClient = storageAccount.CreateCloudBlobClient();
              
                }
                else
                {
                    var accountName = GetAccountNameFromUrl(url);
                    string accountKey = ConfigHelper.AzureAccountKey;

                    if (isSrc)
                    {
                        accountKey = ConfigHelper.SrcAzureAccountKey;
                    }
                    else
                    {
                        accountKey = ConfigHelper.TargetAzureAccountKey;
                    }

                    var credentials = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accountKey);
                    CloudStorageAccount azureStorageAccount = new CloudStorageAccount(credentials, true);
                    BlobClient = azureStorageAccount.CreateCloudBlobClient();

                    // retry policy.
                    // could do with a little work.
                    IRetryPolicy linearRetryPolicy = new LinearRetry( TimeSpan.FromSeconds( ConfigHelper.RetryAttemptDelayInSeconds), ConfigHelper.MaxRetryAttempts);
                    BlobClient.RetryPolicy = linearRetryPolicy;

                }

            }

            return BlobClient;
        }


        private static bool IsDevUrl(string url)
        {
            return (url.Contains(DevAzureDetection));
        }

        // container lives in different part of url depending on dev or real.
        // if the url ends with a /  then assuming the url doesn't mention the blob.
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


        public static IEnumerable<IListBlobItem> ListBlobsInContainer(string containerUrl)
        {
            var client = AzureHelper.GetSourceCloudBlobClient(containerUrl);
            var containerName = AzureHelper.GetContainerFromUrl(containerUrl);

            var container = client.GetContainerReference(containerName);
            var blobList = container.ListBlobs( useFlatBlobListing:true, blobListingDetails:BlobListingDetails.Copy);
            return blobList;
        }

        public static List<string> ListBlobsInContainer(string containerUrl, CopyStatus copyStatusFilter)
        {   
            var blobList = ListBlobsInContainer(containerUrl);

            var filteredBlobList = (from b in blobList where (((ICloudBlob)b).CopyState != null) && (((ICloudBlob)b).CopyState.Status == copyStatusFilter) select b.Uri.AbsolutePath).ToList<string>();
            return filteredBlobList;
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


        public static BasicBlobContainer AzureContainerToBasicBlobContainer(CloudBlobContainer container)
        {
            var basicBlob = new BasicBlobContainer()
            {
                Name = container.Name,
                Url = container.Uri.AbsoluteUri,
                Container = null, 
                BlobType = BlobEntryType.Container
            };

            return basicBlob;
        }
    }
}
