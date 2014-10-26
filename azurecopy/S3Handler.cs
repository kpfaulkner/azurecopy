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

        public void MoveBlob(string startUrl, string finishUrl)
        {


        }

        // make container
        // assumption being last part of url is the new container.
        // With S3 "containers" could really be the bucket for the account
        // OR are we just considering fake subdirectories here?
        // For now (until I decide otherwise) I'll just make it one of the
        // fake subdirectories. 
        // ie blob of 0 bytes.
        public void MakeContainer(string url)
        {
            var bucket = S3Helper.GetBucketFromUrl(url);
            var key = S3Helper.GetKeyFromUrl(url);
            
            using (IAmazonS3 client = S3Helper.GenerateS3Client(ConfigHelper.AWSAccessKeyID, ConfigHelper.AWSSecretAccessKeyID, ConfigHelper.AWSRegion))
            {
                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    ContentBody = "",
                };

                client.PutObject(putObjectRequest);
            }
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

            using (IAmazonS3 client = S3Helper.GenerateS3Client(ConfigHelper.SrcAWSAccessKeyID, ConfigHelper.SrcAWSSecretAccessKeyID, ConfigHelper.SrcAWSRegion))
            {
                GetObjectRequest getObjectRequest = new GetObjectRequest() { BucketName = bucket, Key = key };
                
                using (GetObjectResponse getObjectResponse = client.GetObject(getObjectRequest))
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

            // prune anything before the last /
            var sp = key.Split('/');
            blob.Name = sp.Last();

            return blob;

        }

        // FIXME: only coded for in memory blob.
        public void WriteBlob(string url, Blob blob,   int parallelUploadFactor=1, int chunkSizeInMB=4)
        {
            var bucket = S3Helper.GetBucketFromUrl(url);
            var key = blob.Name;

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

                using (IAmazonS3 client = S3Helper.GenerateS3Client(ConfigHelper.TargetAWSAccessKeyID, ConfigHelper.TargetAWSSecretAccessKeyID, ConfigHelper.TargetAWSRegion))
                {
                    var putObjectRequest = new PutObjectRequest
                        {
                            BucketName = bucket,
                            Key = key,
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

        // lists all blobs (keys) in a bucket.
        // baseUrl for S3 would be something like https://s3-us-west-2.amazonaws.com/mybucket/virtualdir1/virtualdir2/
        // mybucket is the real bucket, virtualdir1 and 2 are virtual directories used for faking directory structures.
        public List<BasicBlobContainer> ListBlobsInContainer(string baseUrl)
        {
            var bucket = S3Helper.GetBucketFromUrl( baseUrl );
            var blobList = new List<BasicBlobContainer>();
            var prefix = S3Helper.GetPrefixFromUrl(baseUrl);

            using (IAmazonS3 client = S3Helper.GenerateS3Client(ConfigHelper.SrcAWSAccessKeyID, ConfigHelper.SrcAWSSecretAccessKeyID, ConfigHelper.SrcAWSRegion))
            {
                var request = new ListObjectsRequest();
     
                request.BucketName = bucket;

                if (!string.IsNullOrEmpty(prefix))
                {
                    request.Prefix = prefix;
                }
                
                do
                {
                    ListObjectsResponse response = client.ListObjects(request);

                    foreach (var obj in response.S3Objects)
                    {
                       
                        var fullPath = GenerateUrl(baseUrl, bucket, obj.Key);

                        // can only go one directory deep for now.
                        // if ends in / will ignore.

                        if (!fullPath.EndsWith("/"))
                        {
                            //var fullPath = Path.Combine(baseUrl, obj.Key);
                            var blob = new BasicBlobContainer();
                            blob.Name = obj.Key.Substring(prefix.Length);
                            blob.Url = fullPath;
                            blob.Container = bucket;
                            blob.BlobType = BlobEntryType.Blob;
                            if (blob.Name.Contains('/'))
                            {
                                blob.DisplayName = blob.Name.Split('/')[1];
                            }
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

        // not passing url. Url will be generated behind the scenes.
        // S3 doesn't really have containers. Do I just concat these together still?
        public Blob ReadBlobSimple(string container, string blobName, string filePath = "")
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            var url = baseUrl;
            if (string.IsNullOrEmpty( container))
            {
                url += "/" + blobName;
            }
            else
            {
                url += "/" + container + "/" + blobName;
                
            }
            return ReadBlob(url, filePath);
        }

        // not passing url.
        // does the passing in of container even make sense here? Since there are no real containers 
        // in S3 (am NOT talking about buckets).
        // Shouldnt it just be adding the container as a prefix to the blob name?
        public void WriteBlobSimple(string container, Blob blob, int parallelUploadFactor = 1, int chunkSizeInMB = 4)
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            var url = baseUrl;

            // just add container as prefix to blobname.
            // This is due to S3 not really having containers but just "prefix" that fake them.
            if (!string.IsNullOrEmpty(container))
            {
                blob.Name = container + "/" + blob.Name;
            }

            WriteBlob(url, blob, parallelUploadFactor, chunkSizeInMB);
        }

        public List<BasicBlobContainer> ListContainers(string baseUrl)
        {
            throw new NotImplementedException("S3 list containers not implemented");
        }


        // not required to pass full url.
        public List<BasicBlobContainer> ListBlobsInContainerSimple(string container)
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            var url = baseUrl + "/" + container;
            return ListBlobsInContainer(url);
        }

        public void MakeContainerSimple(string container)
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            var url = baseUrl + "/" + container;
            MakeContainer(url);
        }



    }

}
