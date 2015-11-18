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
 
using Amazon.S3;
using Amazon.S3.Model;
using azurecopy.Helpers;
using azurecopy.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace azurecopy
{
    public class S3Handler : IBlobHandler
    {
        private string baseUrl = null;
        public static readonly string AwsRegionIdentifier = "region";
        public static readonly string AwsKeyIdentifier = "key";
        public static readonly string AwsSecretKeyIdentifier = "secret";

        private string defaultKey { get; set; }
        private string defaultBlobPrefix { get; set; }

        // Base url.
        // We want to store the URLs in format of s3.aws.com/bucketname
        // if the passed url is bucketname.s3.aws.com then we need to modify
        // before storing it in the baseUrl.
        public S3Handler(string url)
        {
            baseUrl = S3Helper.FormatUrl(url);
            defaultKey = GetDefaultKey(baseUrl);
            defaultBlobPrefix = GetBlobPrefixFromUrl(baseUrl);
        }

        private string GetBlobPrefixFromUrl(string url)
        {
            var sp = url.Split('/');
            return string.Join("/", sp.Skip(4));
        }






        /// <summary>
        /// Gets container name from the full url.
        /// The bucket is part of the domain name.
        /// Do I want to return virtual dirs here... or just bucket?
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetContainerNameFromUrl(string url)
        {
            var sp = url.Split('/');
            return sp[3];
        }

        /// <summary>
        /// Gets blob name from the full url.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetBlobNameFromUrl(string url)
        {
            var sp = url.Split('/');
            return string.Join("/",sp.Skip(4));
        }

        // override configuration. 
        public void OverrideConfiguration( Dictionary<string,string> configuration)
        {
            // assumptions on configuration values are made per cloud type.
            // ie S3 will have diff values to Azure etc.
            string awsRegion;
            if (configuration.TryGetValue( AwsRegionIdentifier, out awsRegion ))
            {
                // reassign the 3 bucket variables for S3. The global, src and target values in ConfigHelper.
                ConfigHelper.AWSRegion = awsRegion;
                ConfigHelper.SrcAWSRegion = awsRegion;
                ConfigHelper.TargetAWSRegion = awsRegion;
            }

            string awsKey;
            if (configuration.TryGetValue(AwsKeyIdentifier, out awsKey))
            {
                // reassign the 3 bucket variables for S3. The global, src and target values in ConfigHelper.
                ConfigHelper.AWSAccessKeyID = awsKey;
                ConfigHelper.SrcAWSAccessKeyID = awsKey;
                ConfigHelper.TargetAWSAccessKeyID = awsKey;
            }

            string awsSecret;
            if (configuration.TryGetValue(AwsSecretKeyIdentifier, out awsSecret))
            {
                // reassign the 3 bucket variables for S3. The global, src and target values in ConfigHelper.
                ConfigHelper.AWSSecretAccessKeyID = awsSecret;
                ConfigHelper.SrcAWSSecretAccessKeyID = awsSecret;
                ConfigHelper.TargetAWSSecretAccessKeyID = awsSecret;
            }
        }

        /// <summary>
        /// Move blob
        /// </summary>
        /// <param name="originContainer"></param>
        /// <param name="destinationContainer"></param>
        /// <param name="startBlobname"></param>
        public void MoveBlob(string originContainer, string destinationContainer, string startBlobname)
        {
            throw new NotImplementedException("Moving not implemented for S3 yet.");
        }

        /// <summary>
        /// Make container/directory (depending on platform).
        /// assumption being last part of url is the new container.
        /// With S3 "containers" could really be the bucket for the account
        /// 
        /// IMPORTANT NOTE:
        /// 
        /// For S3 the bucket comes from the url.
        /// The container name is just the fake virtual directory.
        /// ie blob of 0 bytes.
        /// </summary>
        /// <param name="container"></param>
        public void MakeContainer(string containerName)
        {
            using (IAmazonS3 client = S3Helper.GenerateS3Client(ConfigHelper.AWSAccessKeyID, ConfigHelper.AWSSecretAccessKeyID))
            {
                client.PutBucket(containerName);         
            }
        }

        public string GetBaseUrl()
        {
            return baseUrl;
        }

        /// <summary>
        /// Read blob.
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="cacheFilePath"></param>
        /// <returns></returns>
        public Blob ReadBlob(string containerName, string blobName, string cacheFilePath = "")
        {
            var bucket = containerName;
            var blob = new Blob();
            blob.BlobSavedToFile = !string.IsNullOrEmpty(cacheFilePath);
            blob.FilePath = cacheFilePath;
            blob.BlobOriginType = UrlType.S3;

            using (IAmazonS3 client = S3Helper.GenerateS3Client(ConfigHelper.SrcAWSAccessKeyID, ConfigHelper.SrcAWSSecretAccessKeyID, bucket))
            {
                GetObjectRequest getObjectRequest = new GetObjectRequest() { BucketName = containerName, Key = blobName };
                
                using (GetObjectResponse getObjectResponse = client.GetObject(getObjectRequest))
                {
                    using (Stream s = getObjectResponse.ResponseStream)
                    {
                        // get stream to store.
                        using (var stream = CommonHelper.GetStream(cacheFilePath))
                        {
                            byte[] data = new byte[32768];
                            int bytesRead = 0;
                            do
                            {
                                bytesRead = s.Read(data, 0, data.Length);
                                stream.Write(data, 0, bytesRead);
                            }
                            while (bytesRead > 0);

                            if (!blob.BlobSavedToFile)
                            {
                                var ms = stream as MemoryStream;
                                blob.Data = ms.ToArray();
                            }
                        }
                    }
                }
            }

            blob.Name = blobName;

            return blob;

        }

        /// <summary>
        /// Write blob
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <param name="blob"></param>
        /// <param name="parallelUploadFactor"></param>
        /// <param name="chunkSizeInMB"></param>
        public void WriteBlob(string containerName, string blobName, Blob blob, int parallelUploadFactor = 1, int chunkSizeInMB = 4)
        {
            var bucket = containerName;
            var key = blobName;
            Stream stream = null;

            try
            {             
                // get stream to data.
                if (blob.BlobSavedToFile)
                {
                    stream = new FileStream(blob.FilePath, FileMode.Open);
                }
                else
                {
                    stream = new MemoryStream(blob.Data);
                }

                using (IAmazonS3 client = S3Helper.GenerateS3Client(ConfigHelper.TargetAWSAccessKeyID, ConfigHelper.TargetAWSSecretAccessKeyID, bucket))
                {
                    var putObjectRequest = new PutObjectRequest
                        {
                            BucketName = bucket,
                            Key = blobName,
                            InputStream = stream,                   
                        };

                    client.PutObject(putObjectRequest);
                }
            }
            finally 
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Lists all blobs in a container.
        /// Can be supplied a blobPrefix which basically acts as virtual directory options.
        /// eg, if we have blobs called: "virt1/virt2/myblob"    and
        ///                              "virt1/virt2/myblob2"
        /// Although the blob names are the complete strings mentioned above, we might like to think that the blobs
        /// are just called myblob and myblob2. We can supply a blobPrefix of "virt1/virt2/" which we can *think* of
        /// as a directory, but again, its just really a prefix behind the scenes.
        /// 
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobPrefix"></param>
        /// <returns></returns>
        public List<BasicBlobContainer> ListBlobsInContainer(string containerName = null, string blobPrefix = null, bool debug = false)
        {
            var bucket = containerName;
            var blobList = new List<BasicBlobContainer>();
            using (IAmazonS3 client = S3Helper.GenerateS3Client(ConfigHelper.SrcAWSAccessKeyID, ConfigHelper.SrcAWSSecretAccessKeyID, bucket))
            {
                var request = new ListObjectsRequest();
                request.BucketName = bucket;

                if (string.IsNullOrWhiteSpace(blobPrefix))
                {
                    blobPrefix = defaultBlobPrefix;
                }

                if (!string.IsNullOrEmpty(blobPrefix))
                {
                    request.Prefix = blobPrefix;
                }
         
                // FIXME... check virtual directories workhere.
                do
                {
                    ListObjectsResponse response = client.ListObjects(request);
                    foreach (var obj in response.S3Objects)
                    {
                        //var fullPath = GenerateUrl(baseUrl, bucket, obj.Key);
                        var fullPath = baseUrl + obj.Key;

                        if (!fullPath.EndsWith("/"))
                        {
                            //var fullPath = Path.Combine(baseUrl, obj.Key);
                            var blob = new BasicBlobContainer();
                            blob.Name = obj.Key;
                            blob.Url = fullPath;
                            blob.Container = bucket;
                            blob.BlobType = BlobEntryType.Blob;
                            blob.DisplayName = S3Helper.GetDisplayName(blob.Name);
                            blob.BlobPrefix = blobPrefix;
                            blobList.Add(blob);
                        }
                    }

                    if (response.IsTruncated)
                    {
                        request.Marker = response.NextMarker;
                    }
                    else
                    {
                        request = null;
                    }

                } while (request != null);
            }

            return blobList;
        }

        // generates full url to object.
        // seems strange that I'd need to generate this and that its
        // not returned to the caller already. Will need to investigate. FIXME
        // also assumption about https.
        private string GenerateUrl(string baseUrl, string bucket, string key)
        {
            var url = new Uri(baseUrl);
            var fqdn = "https://"+url.DnsSafeHost;
            // var res = new Uri( new Uri(fqdn), bucket + "/" + key);
            var res = new Uri(new Uri(fqdn), "/" + key);

            return res.AbsoluteUri;

        }

        /// <summary>
        /// List containers/directories off the root. For storage schemes that allow real directories maybe
        /// the root will be 
        /// </summary>
        /// <returns></returns>
        public List<BasicBlobContainer> ListContainers(string root)
        {
            var containerList = new List<BasicBlobContainer>();

            using (IAmazonS3 client = S3Helper.GenerateS3Client(ConfigHelper.SrcAWSAccessKeyID, ConfigHelper.SrcAWSSecretAccessKeyID))
            {
                var buckets = client.ListBuckets();

                foreach(var bucket in buckets.Buckets)
                {
                    containerList.Add(new BasicBlobContainer { BlobType = BlobEntryType.Container, Container = "", DisplayName = bucket.BucketName, Name = bucket.BucketName });
                }
            }
            return containerList;
        }


        // url wil be something like https://s3.com/mybucket/vdir1/vdir2/myfile
        // where the keyname is vdir1/vdir2/myfile.
        private string GetDefaultKey(string url)
        {
            var sp = url.Split('/');
            var key = string.Join("/", sp.Skip(4));
            return key;
        }

   }

}
