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
using Microsoft.WindowsAzure.Storage.File;

namespace azurecopy.Utils
{
    public static class AzureHelper
    {

        public static string AzureStorageConnectionString { get; set; }

        const string AzureDetection = "blob.core.windows.net";
        const string AzureFileDetection = "file.core.windows.net";
        const string DevAzureDetection = "127.0.0.1";
        static CloudBlobClient SrcBlobClient { get; set; }
        static CloudBlobClient TargetBlobClient { get; set; }

        static CloudFileClient SrcFileClient { get; set; }
        static CloudFileClient TargetFileClient { get; set; }


        static AzureHelper()
        {
            SrcBlobClient = null;
            TargetBlobClient = null;
            SrcFileClient = null;
            TargetFileClient = null;
        }

        public static CloudBlobClient GetSourceCloudBlobClient(string url)
        {
            return GetCloudBlobClient(url, true);
        }


        public static CloudBlobClient GetTargetCloudBlobClient(string url)
        {
            return GetCloudBlobClient(url, false);
        }

        public static CloudStorageAccount GetCloudStorageAccount( string url, string accountKey, string accountName)
        {
            CloudStorageAccount storageAccount;

            if (IsDevUrl(url))
            {
                storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            }
            else
            {
                var credentials = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accountKey);
                storageAccount = new CloudStorageAccount(credentials, true);
            }

            return storageAccount;

        }

        public static CloudBlobClient GetCloudBlobClient(string url, bool isSrc )
        {
            CloudBlobClient blobClient = null;

            if (isSrc)
            {
                blobClient = SrcBlobClient;
            }
            else
            {
                blobClient = TargetBlobClient;
            }

            if (blobClient == null)
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

                var storageAccount = GetCloudStorageAccount(url, accountKey, accountName);
                blobClient = storageAccount.CreateCloudBlobClient();

                // retry policy.
                // could do with a little work.
                IRetryPolicy linearRetryPolicy = new LinearRetry( TimeSpan.FromSeconds( ConfigHelper.RetryAttemptDelayInSeconds), ConfigHelper.MaxRetryAttempts);
                blobClient.RetryPolicy = linearRetryPolicy;
                
            }

            return blobClient;
        }

        public static CloudFileClient GetCloudFileClient(string url, bool isSrc)
        {
            CloudFileClient fileClient = null;

            if (isSrc)
            {
                fileClient = SrcFileClient;
            }
            else
            {
                fileClient = TargetFileClient;
            }

            if (fileClient == null)
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

                var storageAccount = GetCloudStorageAccount(url, accountKey, accountName);
                fileClient = storageAccount.CreateCloudFileClient();
            }

            return fileClient;
        }

        public static bool IsDevUrl(string url)
        {
            return (url.Contains(DevAzureDetection));
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

        // currently just use the virtual directory code to get the blob url.
        // same concept, basically everything after the container.
        // This will obviously need to change.
        public static string GetBlobFromUrl(string blobUrl)
        {
            var blobName = GetVirtualDirectoryFromUrl(blobUrl);

            return blobName;
        }

        public static string GetAccountNameFromUrl(string blobUrl)
        {
            var account = "";

            if (!string.IsNullOrEmpty(blobUrl))
            {
                Uri url = new Uri(blobUrl);
                account = url.Host.Split('.')[0];
            }

            return account;
        }

        public static bool MatchHandler(string url)
        {
            return url.Contains(AzureDetection) || url.Contains(DevAzureDetection);
        }

        public static bool MatchFileHandler(string url)
        {
            return url.Contains(AzureFileDetection);
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

        public static string GetDisplayName(string fullBlobName)
        {
            var sp = fullBlobName.Split('/');
            var displayName = sp[sp.Length - 1];
            return displayName;        
        }

        //// Want to get everything after the container.
        //// eg if we have container name "mycontainer", and the blobUrl passed in is
        //// "http://xxxx/mycontainer/dira/dirb" then we need to return "dira/dirb"
        public static string GetVirtualDirectoryFromUrl(string blobUrl)
        {
            var url = new Uri(blobUrl);
            string virtualDir = "";

            if (IsDevUrl(blobUrl))
            {
                virtualDir = string.Join("/", url.Segments.Skip(3));
            }
            else
            {
                virtualDir = string.Join("/", url.Segments.Skip(2));
            }

            virtualDir = virtualDir.TrimEnd('/');
            return virtualDir;

        }

        // container lives in different part of url depending on dev or real.
        // if the url ends with a /  then assuming the url doesn't mention the blob.
        // blobUrl can contain multiple levels of / due to virtual directories 
        // may be referenced.
        public static string GetContainerFromUrl(string blobUrl, bool assumeNoBlob = false)
        { 
            var url = new Uri(blobUrl);
            string container = "";  // there may be no container.

            if (IsDevUrl(blobUrl))
            {
                container = url.Segments[2];
            }
            else
            {
                if (url.Segments.Length > 1)
                {
                    container = url.Segments[1];
                }
            }

            container = container.TrimEnd('/');
            return container;
        }
    }
}
