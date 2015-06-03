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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using azurecopy.Utils;
using System.Threading;
using azurecopy.Helpers;

namespace azurecopy
{

    public class AzureBlobCopyHandler
    {
        /// <summary>
        /// Makes a usable URL for a blob. This will need to handle security on the source blob.
        /// Each cloud provider is different.
        /// Cloud providers developed:
        ///     Azure
        ///     S3
        ///     
        /// Cloud providers soon:
        ///     Dropbox
        ///     Onedrive
        /// </summary>
        /// <param name="origBlob"></param>
        /// <returns></returns>
        private static string GeneratedAccessibleUrl( BasicBlobContainer origBlob)
        {
            var sourceUrl = origBlob.Url + "/" + origBlob.Name;
            string url = "";

            // if S3, then generate signed url.
            if (S3Helper.MatchHandler(sourceUrl))
            {
                var bucket = S3Helper.GetBucketFromUrl(sourceUrl);
                var key = S3Helper.GetKeyFromUrl(sourceUrl);
                url = S3Helper.GeneratePreSignedUrl(bucket, key);
            } else if (AzureHelper.MatchHandler( sourceUrl))
            {
                // generate Azure signed url.
                var client = AzureHelper.GetSourceCloudBlobClient(sourceUrl);
                var policy = new SharedAccessBlobPolicy();
                policy.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-1);
                policy.SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes( ConfigHelper.SharedAccessSignatureDurationInSeconds / 60 );
                policy.Permissions = SharedAccessBlobPermissions.Read;
                var blob = client.GetBlobReferenceFromServer( new Uri(sourceUrl));
                url = sourceUrl+ blob.GetSharedAccessSignature(policy);
            } else if (DropboxHelper.MatchHandler(sourceUrl))
            {
                throw new NotImplementedException("Blobcopy against dropbox is not implemented yet");
            }

            return url;

        }


        // Copy from complete URL (assume URL is complete at this stage) to destination blob.
        public static void StartCopy(BasicBlobContainer origBlob, string DestinationUrl, DestinationBlobType destBlobType)
        {
            var client = AzureHelper.GetTargetCloudBlobClient(DestinationUrl);
            var opt = client.GetServiceProperties();

            var containerName = AzureHelper.GetContainerFromUrl( DestinationUrl);
            var blobName = AzureHelper.GetBlobFromUrl( DestinationUrl );

            var container = client.GetContainerReference( containerName );
            container.CreateIfNotExists();

            ICloudBlob blob = null;
            var url = GeneratedAccessibleUrl(origBlob);

            // include unknown for now. Unsure.
            if (destBlobType == DestinationBlobType.Block || destBlobType == DestinationBlobType.Unknown)
            {
                blob = container.GetBlockBlobReference(blobName);
                
            } else if (destBlobType == DestinationBlobType.Page)
            {
                blob = container.GetPageBlobReference(blobName);
            }

            if (blob != null)
            {
                var res = blob.StartCopyFromBlob(new Uri(url));
            }
            else
            {
                throw new NotImplementedException("Cannot copy blobs that are not block or page");
            }

        }

        public static void MonitorBlobCopy(string destinationUrl)
        {
            var copyComplete = false;
            while (!copyComplete)
            {

                var failedBlobList = AzureHelper.ListBlobsInContainer(destinationUrl, Microsoft.WindowsAzure.Storage.Blob.CopyStatus.Failed);
                var abortedBlobList = AzureHelper.ListBlobsInContainer(destinationUrl, Microsoft.WindowsAzure.Storage.Blob.CopyStatus.Aborted);
                var pendingBlobList = AzureHelper.ListBlobsInContainer(destinationUrl, Microsoft.WindowsAzure.Storage.Blob.CopyStatus.Pending);

                Console.WriteLine("\n\nFailed:");
                foreach (var b in failedBlobList) { Console.WriteLine(b); }

                Console.WriteLine("Aborted:");
                foreach (var b in abortedBlobList) { Console.WriteLine(b); }

                Console.WriteLine("Pending:");
                foreach (var b in pendingBlobList) { Console.WriteLine(b); }

                if (pendingBlobList.Count == 0)
                {
                    copyComplete = true;
                }
                else
                {
                    Thread.Sleep(1000);
                }

            };

            Console.WriteLine("Copy complete");
        }

    }
}
