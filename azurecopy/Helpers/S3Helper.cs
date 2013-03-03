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
using Amazon.S3.Model;
using Amazon.S3;

namespace azurecopy.Utils
{
    public static class S3Helper
    {

        static  string AmazonDetection = "amazon";

        public static string GetBucketFromUrl(string url)
        {
            var u = new Uri( url );
            var bucket = u.DnsSafeHost.Split('.')[0];

            return bucket;
        }


        public static string GetKeyFromUrl(string url)
        {
            var u = new Uri(url);

            var blobName = u.PathAndQuery.Substring(1);

            return blobName;
        }

        public static bool MatchHandler(string url)
        {
            return url.Contains(AmazonDetection);
        }

        public static string GeneratePreSignedUrl( string bucket, string key, int timeout=30 )
        {

            GetPreSignedUrlRequest request = new GetPreSignedUrlRequest()
                .WithBucketName(bucket)
                .WithKey(key)
                .WithExpires(DateTime.Now.AddMinutes(32))
                .WithProtocol(Protocol.HTTPS);
            using (AmazonS3 client = Amazon.AWSClientFactory.CreateAmazonS3Client(ConfigHelper.AWSAccessKeyID, ConfigHelper.AWSSecretAccessKeyID))
            {
                string url = client.GetPreSignedURL(request);
                return url;
            }
        }
    }
}
