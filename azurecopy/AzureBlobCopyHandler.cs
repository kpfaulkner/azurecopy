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

namespace azurecopy
{

    public class AzureBlobCopyHandler
    {

        // Copy from complete URL (assume URL is complete at this stage) to destination blob.
        public static void StartCopy(string sourceUrl, string DestinationUrl)
        {

            var client = AzureHelper.GetTargetCloudBlobClient(DestinationUrl);

            var containerName = AzureHelper.GetContainerFromUrl( DestinationUrl);
            var blobName = AzureHelper.GetBlobFromUrl( DestinationUrl );

            var container = client.GetContainerReference( containerName );
            container.CreateIfNotExists();

            var blob = container.GetBlockBlobReference(blobName);

            var url = sourceUrl;
            // if S3, then generate signed url.
            if (S3Helper.MatchHandler(sourceUrl))
            {
                var bucket = S3Helper.GetBucketFromUrl(sourceUrl);
                var key = S3Helper.GetKeyFromUrl(sourceUrl);
                url = S3Helper.GeneratePreSignedUrl(bucket, key);
            }

            // starts the copying process....
            var res = blob.StartCopyFromBlob(new Uri(url));
            var a = res;
        }

        // Monitor progress of copy.
        public static void MonitorCopy( string DestinationUrl )
        {

        }



    }
}
