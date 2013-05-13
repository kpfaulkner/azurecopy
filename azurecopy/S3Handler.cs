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

        public S3Handler(string url = null)
        {
            baseUrl = url;
        }

        private void Authenticate()
        {

        }

        public string GetBaseUrl()
        {
            return baseUrl;
        }

        /// <summary>
        /// Basic in memory copy...   just a starting point.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public Blob ReadBlob(string url, string fileName = "")
        {

            var bucket = S3Helper.GetBucketFromUrl(url);
            var key = S3Helper.GetKeyFromUrl(url);
            var blob = new Blob();

            blob.BlobSavedToFile = !string.IsNullOrEmpty(fileName);
            blob.FilePath = fileName;
            blob.BlobOriginType = UrlType.S3;

            using (AmazonS3 client = Amazon.AWSClientFactory.CreateAmazonS3Client(ConfigHelper.SrcAWSAccessKeyID, ConfigHelper.SrcAWSSecretAccessKeyID))
            {
                GetObjectRequest getObjectRequest = new GetObjectRequest().WithBucketName(bucket).WithKey(key);

                using (S3Response getObjectResponse = client.GetObject(getObjectRequest))
                {

                    using (Stream s = getObjectResponse.ResponseStream)
                    {
                        // get stream to store.
                        using (var stream = CommonHelper.GetStream(fileName))
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

            // need to rename somehow...  hopefully this will do.
            blob.Name = key.Replace("/", "_");

            return blob;

        }

        public void WriteBlob(string url, Blob blob,   int parallelUploadFactor=1, int chunkSizeInMB=4)
        {
            var bucket = S3Helper.GetBucketFromUrl(url);
            //var key = S3Helper.GetKeyFromUrl(url);
            var key = blob.Name;

            using (AmazonS3 client = Amazon.AWSClientFactory.CreateAmazonS3Client(ConfigHelper.TargetAWSAccessKeyID, ConfigHelper.TargetAWSSecretAccessKeyID))
            {

                using (var ms = new MemoryStream( blob.Data) )
                {
                   
                    var putObjectRequest = new PutObjectRequest {
                        BucketName            = bucket,
                        Key                   = key,
                        GenerateMD5Digest     = true,
                        Timeout               = -1,
                        InputStream = ms,
                        ReadWriteTimeout      = 300000     // 5 minutes in milliseconds

                    };

                    client.PutObject( putObjectRequest);
                }
            }
        }

        // lists all blobs (keys) in a bucket.
        // baseUrl for S3 would be something like https://testken123.s3-us-west-2.amazonaws.com/
        // and then we get all blobs in that bucket.
        // Am NOT going to return "fake" directories etc as some apps do. Will be returning the real results and
        // will be relying on the caller to interpret as they see fit.
        public List<BasicBlobContainer> ListBlobsInContainer(string baseUrl)
        {
            var bucket = S3Helper.GetBucketFromUrl( baseUrl );
            var blobList = new List<BasicBlobContainer>();

            using (AmazonS3 client = Amazon.AWSClientFactory.CreateAmazonS3Client(ConfigHelper.SrcAWSAccessKeyID, ConfigHelper.SrcAWSSecretAccessKeyID))
            {
                ListObjectsRequest listObjectRequest = new ListObjectsRequest();
                var request = new ListObjectsRequest();
                request.BucketName = bucket;
                do
                {
                    ListObjectsResponse response = client.ListObjects(request);

                    foreach (var obj in response.S3Objects)
                    {
                        var fullPath = Path.Combine(baseUrl, obj.Key);
                        var blob = new BasicBlobContainer();
                        blob.Name = obj.Key;
                        blob.Url = fullPath;
                        blob.Container = bucket;
                        blob.BlobType = BlobEntryType.Blob;
                        blobList.Add(blob);
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

        // not passing url. Url will be generated behind the scenes.
        // S3 doesn't really have containers. Do I just concat these together still?
        public Blob ReadBlobSimple(string container, string blobName, string filePath = "")
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            var url = baseUrl + "/" + container + "/" + blobName;
            return ReadBlob(url, filePath);
        }

        // not passing url.
        public void WriteBlobSimple(string container, Blob blob, int parallelUploadFactor = 1, int chunkSizeInMB = 4)
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            var url = baseUrl + "/" + container + "/";
            WriteBlob(url, blob, parallelUploadFactor, chunkSizeInMB);
        }

        // not required to pass full url.
        public List<BasicBlobContainer> ListBlobsInContainerSimple(string container)
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            var url = baseUrl + "/" + container + "/";
            return ListBlobsInContainer(url);
        }



    }

}
