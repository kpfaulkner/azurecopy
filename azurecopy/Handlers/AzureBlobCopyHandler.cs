//-----------------------------------------------------------------------
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
using Microsoft.WindowsAzure.Storage.Blob;
using azurecopy.Utils;
using System.Threading;
using azurecopy.Helpers;

namespace azurecopy
{
    public class BlobCopyData
    {
        public string CopyID { get; set; }
        public ICloudBlob Blob { get; set; }
    }
    
    public class AzureBlobCopyHandler
    {
        // used to tweak blobcopy timeouts.
        static int maxExecutionTimeInMins;
        static int maxServerTimeoutInMins;
        static int blobCopyBatchSize;

        static AzureBlobCopyHandler()
        {
            maxExecutionTimeInMins = ConfigHelper.MaxExecutionTimeInMins;
            maxServerTimeoutInMins = ConfigHelper.MaxServerTimeoutInMins;
            blobCopyBatchSize = ConfigHelper.BlobCopyBatchSize;

        }

        /// <summary>
        /// Makes a usable URL for a blob. This will need to handle security on the source blob.
        /// Each cloud provider is different.
        /// Cloud providers developed:
        ///     Azure
        ///     S3xx
        ///     
        /// Cloud providers soon:
        ///     Dropbox
        ///     Onedrive
        /// </summary>
        /// <param name="origBlob"></param>
        /// <returns></returns>
        private static string GeneratedAccessibleUrl( BasicBlobContainer origBlob)
        {
            var sourceUrl = origBlob.Url; // +"/" + origBlob.Name;
            string url = "";

            // if S3, then generate signed url.
            if (S3Helper.MatchHandler(sourceUrl))
            {
                var bucket = S3Helper.GetBucketFromUrl(sourceUrl);
                var key = origBlob.Name;
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
                // need shorter url. (no base url);
                var uri = new Uri(sourceUrl);
                var shortUrl = uri.PathAndQuery;
                var client = DropboxHelper.GetClient();
                var media = client.GetMedia(shortUrl);
                return media.Url;
            } else if (SkyDriveHelper.MatchHandler( sourceUrl))
            {
                throw new NotImplementedException("Blobcopy against onedrive is not implemented yet");
            }

            return url;

        }

        /// <summary>
        /// Start copying all the blobs using BlobCopy API.
        /// Will break it into batches.
        /// </summary>
        /// <param name="origBlobList"></param>
        /// <param name="destinationUrl"></param>
        /// <param name="destBlobType"></param>
        public static void StartCopyList(IEnumerable<BasicBlobContainer> origBlobList, string destinationUrl, DestinationBlobType destBlobType, bool debugMode)
        {
            var blobCopyDataList = new List<BlobCopyData>();

            var count = 0;

            // break into batches
            foreach (var blob in origBlobList)
            {
                try
                {
                    Console.WriteLine("Copy blob " + blob.DisplayName);
                    var bcd = AzureBlobCopyHandler.StartCopy(blob, destinationUrl, destBlobType);
                    Console.WriteLine("BlobCopy ID " + bcd.CopyID);
                    blobCopyDataList.Add(bcd);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to start copying " + blob.DisplayName);
                    if (debugMode)
                    {
                        Console.WriteLine("Exception " + ex.ToString());
                    }
                }

                count++;

                if (count > blobCopyBatchSize)
                {
                    Console.WriteLine("New Batch");
                    // if blob copy and monitoring
                    if (ConfigHelper.MonitorBlobCopy)
                    {
                        AzureBlobCopyHandler.MonitorBlobCopy(destinationUrl);
                    }
                    
                    count = 0;
                }

            }

            if (count > 0)
            {
                // if blob copy and monitoring
                if (ConfigHelper.MonitorBlobCopy)
                {
                    Console.WriteLine("New Batch");
                    AzureBlobCopyHandler.MonitorBlobCopy(destinationUrl);
                }
            }            
        }

        // have destination location.
        // have original blob name and prefix
        // new name should be destination name + (blob.name - blob.prefix) 
        public static BlobCopyData StartCopy(BasicBlobContainer origBlob, string DestinationUrl, DestinationBlobType destBlobType)
        {
            
            var client = AzureHelper.GetTargetCloudBlobClient(DestinationUrl);
            var opt = client.GetServiceProperties();

            var containerName = AzureHelper.GetContainerFromUrl( DestinationUrl);
            var destBlobPrefix = AzureHelper.GetBlobFromUrl( DestinationUrl );

            var container = client.GetContainerReference( containerName );
            container.CreateIfNotExists();

            ICloudBlob blob = null;
            var url = GeneratedAccessibleUrl(origBlob);

            string blobName;

            string blobWithoutPrefix = string.Empty;

            if (!string.IsNullOrWhiteSpace(origBlob.BlobPrefix) && origBlob.Name.Length > origBlob.BlobPrefix.Length)
            {
                blobWithoutPrefix = origBlob.Name.Substring(origBlob.BlobPrefix.Length);
            }

            if (!string.IsNullOrWhiteSpace(blobWithoutPrefix))
            {
                blobName = string.Format("{0}/{1}", destBlobPrefix, blobWithoutPrefix);
            }
            else
            {
                // need to get just filename. ie last element of /
                var actualBlobName = origBlob.Name.Split('/').Last();

                if (string.IsNullOrWhiteSpace(destBlobPrefix))
                {
                    blobName = actualBlobName;
                }
                else
                {
                    blobName = string.Format("{0}/{1}", destBlobPrefix, actualBlobName);
                }
            }

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
                try
                {
                    // crazy large values, want to try and debug an issue.
                    var brOptions = new BlobRequestOptions();
                    brOptions.MaximumExecutionTime = new TimeSpan(0, maxExecutionTimeInMins, 0);
                    brOptions.ServerTimeout = new TimeSpan(0, maxServerTimeoutInMins, 0);

                    // return copyID incase user wants to kill the process later.
                    var copyID = blob.StartCopyFromBlob(new Uri(url), options: brOptions);

                    var bcd = new BlobCopyData { CopyID = copyID, Blob = blob };
                    return bcd;
                }
                catch(Exception ex)
                {
                    Console.WriteLine("StartCopyFromBlob error msg " + ex.Message);
                    Console.WriteLine("StartCopyFromBlob error stack " + ex.StackTrace);
                    throw;

                }
            }
            else
            {
                throw new NotImplementedException("Cannot copy blobs that are not block or page");
            }

        }

        public static void AbortCopy(string copyID)
        {

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
