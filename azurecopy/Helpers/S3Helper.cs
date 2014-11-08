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
using Amazon;

namespace azurecopy.Utils
{
    /// <summary>
    /// ASSUMPTION is that all S3 URLs will be in format https://bucketname.s3<region>.amazonaws.com and NOT
    /// https://s3<region>.amazonaws.com/bucketname
    /// Will need to handle both eventually.
    /// </summary>
    public static class S3Helper
    {
        static  string AmazonDetection = "amazon";
        static Dictionary<String, Amazon.RegionEndpoint> RegionDict;

        // hardcoded region information
        // convert to app config at a later stage maybe? For now the data seems fairly static
        // that I'll keep it hardcoded here.
        static S3Helper()
        {
            RegionDict = GenerateRegionDict();
        }

        static private Dictionary<string, Amazon.RegionEndpoint> GenerateRegionDict()
        {
            var rd = new Dictionary<string, Amazon.RegionEndpoint>();

            rd["us-west-1"] = Amazon.RegionEndpoint.USWest1;
            rd["us-west-2"] = Amazon.RegionEndpoint.USWest2;
            rd["us-east-1"] = Amazon.RegionEndpoint.USEast1;
            rd["eu-west-1"] = Amazon.RegionEndpoint.EUWest1;
            rd["ap-southeast-1"] = Amazon.RegionEndpoint.APSoutheast1;
            rd["ap-southeast-1"] = Amazon.RegionEndpoint.APSoutheast2;
            rd["ap-northeast-1"] = Amazon.RegionEndpoint.APNortheast1;
            rd["sa-east-1"] = Amazon.RegionEndpoint.SAEast1;
            rd[""] = Amazon.RegionEndpoint.USEast1;   // default...
          
            return rd;
        }

        // assuming URL is in form :https://bucketname.s3.amazonaws.com
        public static string GetBucketFromUrl(string url)
        {
            var u = new Uri( url );
            
            // used for https://bucketname.s3.amazonaws.com/  format.
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

        // Assumption that we only need this when the source is S3.
        // Therefore use SourceAWS.
        public static string GeneratePreSignedUrl( string bucket, string key, int timeout=30 )
        {

            // set for 5 hours... just incase.
            GetPreSignedUrlRequest request = new GetPreSignedUrlRequest()
            {
                BucketName = bucket,
                Key = key,
                Expires = DateTime.Now.AddMinutes( ConfigHelper.SharedAccessSignatureDurationInSeconds /60),
                Protocol = Protocol.HTTPS
            };

            using (IAmazonS3 client = GenerateS3Client(ConfigHelper.SrcAWSAccessKeyID, ConfigHelper.SrcAWSSecretAccessKeyID, bucket))
            {
                string url = client.GetPreSignedURL(request);
                return url;
            }
        }

        // Create one client to get region.. then create real one?
        // Seems dumb, but will see how it goes.
        public static IAmazonS3 GenerateS3Client( string accessKey, string secretKey, string bucketName = null)
        {
            IAmazonS3 client = Amazon.AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey);

            if (!string.IsNullOrEmpty(bucketName))
            {
                var regionForBucket = client.GetBucketLocation(bucketName);

                // if default US region then location will be null. Replace it with string.Empty for region lookup.
                client = Amazon.AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey, RegionDict[regionForBucket.Location ?? string.Empty]);
            }
            
            return client;
        }

        internal static string GetPrefixFromUrl(string baseUrl)
        {
            var url = new Uri(baseUrl);
            var u = url.Segments;
            var prefix = string.Join("",url.Segments.Skip(1));
            return prefix;
        }
    }
}
