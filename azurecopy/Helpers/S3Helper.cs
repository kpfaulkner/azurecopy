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

        // assuming URL is in form https://s3.amazonaws.com/bucketname  and
        // NOT in the form :https://bucketname.s3.amazonaws.com
        // WHY WHY WHY the above comment?
        // will eventually need to handle both URL formats, but for now I may need to 
        // focus on https://bucketname.s3.amazonaws.com format due to the AWS libs creating urls.
        public static string GetBucketFromUrl(string url)
        {
            var u = new Uri( url );
            
            // used for https://bucketname.s3.amazonaws.com/  format.
            var bucket = u.DnsSafeHost.Split('.')[0];

            /*
            var bucket = "";
            if (u.Segments.Length > 1)
            {
                bucket = u.Segments[1];
                if (bucket.EndsWith("/"))
                {
                    bucket = bucket.Substring(0, bucket.Length - 1);
                }
            }
            */
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

        // Assumption that we only need this when the source is S3.
        // Therefore use SourceAWS.
        public static string GeneratePreSignedUrl( string bucket, string key, int timeout=30 )
        {

            // set for 5 hours... just incase.
            GetPreSignedUrlRequest request = new GetPreSignedUrlRequest()
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.Now.AddMinutes(300),
                Protocol = Protocol.HTTPS
            };

            using (AmazonS3 client = Amazon.AWSClientFactory.CreateAmazonS3Client(ConfigHelper.SrcAWSAccessKeyID, ConfigHelper.SrcAWSSecretAccessKeyID))
            {
                string url = client.GetPreSignedURL(request);
                return url;
            }
        }

        internal static string GetPrefixFromUrl(string baseUrl)
        {
            var url = new Uri(baseUrl);
            var u = url.Segments;
            var prefix = string.Join("/", url.Segments.Skip(2));
            return prefix;
        }
    }
}
