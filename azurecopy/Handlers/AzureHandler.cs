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

using azurecopy.Exceptions;
using azurecopy.Helpers;
using azurecopy.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    public class AzureHandler : IBlobHandler
    {
        private string baseUrl = null;
        public static readonly string AzureAccountKey = "AzureAccountKey";
  
        public AzureHandler(string url = null)
        {
            baseUrl = url;
        }

        public void MoveBlob( string startUrl, string finishUrl)
        {
            throw new NotImplementedException("MoveBlob not implemented");
        }

        /// <summary>
        /// Make new Azure container. 
        /// Assumption being last part of url is the new container.
        /// </summary>
        /// <param name="url"></param>
        public void MakeContainer(string url)
        {
            var uri = new Uri(url);
            var containerName = uri.Segments[uri.Segments.Length - 1];
            var client = AzureHelper.GetSourceCloudBlobClient(url);
            var container = client.GetContainerReference(containerName);
            container.CreateIfNotExists();
        }

        public string GetBaseUrl()
        {
            return baseUrl;
        }

        /// <summary>
        /// Override the configuration file.
        /// </summary>
        /// <param name="configuration"></param>
        public void OverrideConfiguration(Dictionary<string, string> configuration)
        {
            string azureAccountKey;
            if (configuration.TryGetValue(AzureAccountKey, out azureAccountKey))
            {
                ConfigHelper.AzureAccountKey = azureAccountKey;
                ConfigHelper.SrcAzureAccountKey = azureAccountKey;
                ConfigHelper.TargetAzureAccountKey = azureAccountKey;
            }
        }

        /// <summary>
        /// Read blob based on URL.
        /// Passes off to block or page blob specific code.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public Blob ReadBlob(string url, string filePath = "")
        {
            try
            {
                Blob blob = null;
                var client = AzureHelper.GetSourceCloudBlobClient(url);
                var containerName = AzureHelper.GetContainerFromUrl(url);
                var container = client.GetContainerReference(containerName);
                var blobRef = client.GetBlobReferenceFromServer(new Uri(url));
                var isBlockBlob = (blobRef.BlobType == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);

                if (isBlockBlob)
                {
                    blob = ReadBlockBlob(blobRef, filePath);
                }
                else
                {
                    blob = ReadPageBlob(blobRef, filePath);
                }

                blob.BlobOriginType = UrlType.Azure;
                return blob;
            }
            catch(Exception ex)
            {
                throw new CloudReadException("AzureHandler:ReadBlob unable to read blob", ex);
            }
        }

        /// <summary>
        /// Read block blob.
        /// </summary>
        /// <param name="blobRef"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private Blob ReadBlockBlob(ICloudBlob blobRef, string fileName = "" )
        {
            try
            {
                var blob = new Blob();
                blob.BlobSavedToFile = !string.IsNullOrEmpty(fileName); // if filename provided then the blob should be cached to file.
                blob.Name = blobRef.Name;
                blob.FilePath = fileName;
                blob.BlobType = DestinationBlobType.Block;

                // get stream to store.
                using (var stream = CommonHelper.GetStream(fileName))
                {
                    var blockBlob = blobRef as CloudBlockBlob;
                    blockBlob.DownloadToStream(stream);
                    if (!blob.BlobSavedToFile)
                    {
                        var ms = stream as MemoryStream;
                        blob.Data = ms.ToArray();
                    }
                }
                return blob;
            }
            catch( Exception ex)
            {
                throw new CloudReadException("AzureHandler:ReadBlockBlob unable to read blob", ex);
            }
        }

        /// <summary>
        /// Read page blob.
        /// </summary>
        /// <param name="blobRef"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private Blob ReadPageBlob(ICloudBlob blobRef, string fileName = "")
        {
            try
            {
                var blob = new Blob();
                blob.BlobSavedToFile = !string.IsNullOrEmpty(fileName);
                blob.Name = blobRef.Name;
                blob.FilePath = fileName;
                blob.BlobType = DestinationBlobType.Page;
         
                // get stream to store.
                using (var stream = CommonHelper.GetStream(fileName))
                {
                    var pageBlob = blobRef as CloudPageBlob;
                    pageBlob.DownloadToStream(stream);

                    if (!blob.BlobSavedToFile)
                    {
                        var ms = stream as MemoryStream;
                        blob.Data = ms.ToArray();
                    }
                }
                return blob;
            }
            catch (Exception ex)
            {
                throw new CloudReadException("AzureHandler:ReadPageBlob unable to read blob", ex);
            }
        }

        /// <summary>
        /// Write blob. 
        /// Can write in parallel based on parallelUploadFactor
        /// </summary>
        /// <param name="url"></param>
        /// <param name="blob"></param>
        /// <param name="parallelUploadFactor"></param>
        /// <param name="chunkSizeInMB"></param>
        public void WriteBlob(string url, Blob blob, int parallelUploadFactor=1, int chunkSizeInMB=4)
        {
            Stream stream = null;

            try
            {
                var client = AzureHelper.GetTargetCloudBlobClient(url);
                var containerName = AzureHelper.GetContainerFromUrl(url);
                var blobName = blob.Name;

                var container = client.GetContainerReference(containerName);
                container.CreateIfNotExists();

                // get stream to data.
                if (blob.BlobSavedToFile)
                {
                    stream = new FileStream(blob.FilePath, FileMode.Open);
                }
                else
                {
                    stream = new MemoryStream(blob.Data);
                }

                // if unknown type, then will assume Block... for better or for worse.
                if (blob.BlobType == DestinationBlobType.Block || blob.BlobType == DestinationBlobType.Unknown)
                {
                    WriteBlockBlob(stream, blob, container, parallelUploadFactor, chunkSizeInMB);   
                }
                else if (blob.BlobType == DestinationBlobType.Page)
                {
                    WritePageBlob(stream, blob, container);
                }
                else
                {
                    throw new NotImplementedException("Have not implemented page type other than block or page");
                }
            }
            catch (Exception ex)
            {
                throw new CloudWriteException("AzureHandler::WriteBlob cannot write", ex);
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
        /// Upload in parallel.
        /// If total size of file is smaller than chunkSize, then simply split length by parallel factor.
        /// FIXME: Need to retest this!!!
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="blob"></param>
        /// <param name="parallelFactor"></param>
        /// <param name="chunkSizeInMB"></param>
        private void ParallelWriteBlockBlob(Stream stream, CloudBlockBlob blob, int parallelFactor, int chunkSizeInMB)
        {
            long chunkSize = chunkSizeInMB * 1024*1024;
            var length = stream.Length;

            if (chunkSize > length)
            {
                chunkSize = length / parallelFactor;
            }

            var numberOfBlocks = (length / chunkSize ) +1 ;
            var blockIdList = new string[numberOfBlocks];
            var chunkSizeList = new int[numberOfBlocks];
            var taskList = new List<Task>();

            var count = numberOfBlocks - 1;

            // read the data...  spawn a task to launch... then wait for all.
            while (count >= 0)
            {
                while (count >= 0 && taskList.Count < parallelFactor)
                {
                    var index = (numberOfBlocks - count - 1);
                    var chunkSizeToUpload = (int)Math.Min(chunkSize, length - (index * chunkSize));

                    // only upload if we have data to give.
                    // edge case where we already have uploaded all the data.
                    if (chunkSizeToUpload > 0)
                    {
                        chunkSizeList[index] = chunkSizeToUpload;
                        var dataBuffer = new byte[chunkSizeToUpload];
                        stream.Seek(index * chunkSize, SeekOrigin.Begin);
                        stream.Read(dataBuffer, 0, chunkSizeToUpload);

                        var t = Task.Factory.StartNew(() =>
                        {
                            var tempCount = index;
                            var uploadSize = chunkSizeList[tempCount];

                            var newBuffer = new byte[uploadSize];
                            Array.Copy(dataBuffer, newBuffer, dataBuffer.Length);

                            var blockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                            using (var memStream = new MemoryStream(newBuffer, 0, uploadSize))
                            {
                                blob.PutBlock(blockId, memStream, null);
                            }
                            blockIdList[tempCount] = blockId;
                        });
                        taskList.Add(t);
                    }
                    count--;
                }

                var waitedIndex = Task.WaitAny(taskList.ToArray());
                if (waitedIndex >= 0)
                {
                    taskList.RemoveAt(waitedIndex);
                }
            }
            Task.WaitAll(taskList.ToArray());
            blob.PutBlockList(blockIdList.Where(t => t != null));
        }

        /// <summary>
        /// Write a block blob. Determines if should be parallel or not then calls "real" method.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="blob"></param>
        /// <param name="container"></param>
        /// <param name="parallelFactor"></param>
        /// <param name="chunkSizeInMB"></param>
        private void WriteBlockBlob(Stream stream, Blob blob, CloudBlobContainer container,int parallelFactor=1, int chunkSizeInMB=2)
        {
            var blobRef = container.GetBlockBlobReference(blob.Name);
            blobRef.DeleteIfExists();

            // use "parallel" option even if parallelfactor == 1.
            // This is because I've found that blobRef.UploadFromStream to be unreliable.
            // Unsure if its a timeout issue or some other cause. (huge stacktrace/exception thrown from within
            // the client lib code.
            if (parallelFactor > 0)
            {
                ParallelWriteBlockBlob(stream, blobRef, parallelFactor, chunkSizeInMB);
            }
            else
            {
                blobRef.UploadFromStream(stream);
            }
        }

        /// <summary>
        /// Write page blob. Although concurrency params exist, does NOT do concurrent uploading yet.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="blob"></param>
        /// <param name="container"></param>
        /// <param name="parallelFactor"></param>
        /// <param name="chunkSizeInMB"></param>
        private void WritePageBlob(Stream stream, Blob blob, CloudBlobContainer container,int parallelFactor=1, int chunkSizeInMB=2)
        {
            var blobRef = container.GetPageBlobReference(blob.Name);
            blobRef.UploadFromStream(stream);
        }

        // assumption is that baseurl can include items PAST the container level.
        // ie a url such as: https://....../mycontainer/virtualdir1/virtualdir2   could be used.
        // Now, we know the first directory listed is the container (assumption?) but
        // virtualdir1/virtualdir2 are just blob name prefixes that are used to fake
        // a filesystem like structure.
        public List<BasicBlobContainer> ListBlobsInContainer(string baseUrl)
        {
            var blobList = new List<BasicBlobContainer>();
            var client = AzureHelper.GetSourceCloudBlobClient(baseUrl);
            var containerName = AzureHelper.GetContainerFromUrl(baseUrl, true);
            var virtualDirectoryName = AzureHelper.GetVirtualDirectoryFromUrl(baseUrl);
            var blobName = AzureHelper.GetBlobFromUrl(baseUrl);
                
            IEnumerable<IListBlobItem> azureBlobList;
            CloudBlobContainer container;

            if (string.IsNullOrEmpty(containerName))
            {
                container = client.GetRootContainerReference();

                // add container.
                // Assuming no blobs at root level.
                // incorrect assumption. FIXME!
                var containerList = client.ListContainers();
                foreach (var cont in containerList)
                {
                    var b = new BasicBlobContainer();
                    b.Name = cont.Name;
                    b.Container = "";
                    b.Url = cont.Uri.AbsoluteUri;
                    b.BlobType = BlobEntryType.Container;
                    blobList.Add(b);
                }
            }
            else
            {
                container = client.GetContainerReference(containerName);

                // if we were only passed the container name, then list contents of container.
                if (string.IsNullOrEmpty(virtualDirectoryName))
                {
                    // add blobs
                    azureBlobList = container.ListBlobs(useFlatBlobListing:true);  
                }
                else
                {
                    // if passed virtual directory information, then filter based off that.
                    var vd = container.GetDirectoryReference(virtualDirectoryName);
                    azureBlobList = vd.ListBlobs();
                }

                foreach (var blob in azureBlobList)
                {
                    var b = new BasicBlobContainer();
                    var bn = AzureHelper.GetBlobFromUrl(blob.Uri.AbsoluteUri);
                    b.Name = bn;
                    var sp = bn.Split('/');
                    var displayName = sp[ sp.Length -1];
                    b.DisplayName = displayName;
                    b.Container = blob.Container.Name;
                    b.Url = blob.Uri.AbsoluteUri;
                    b.BlobType = BlobEntryType.Blob;
                    blobList.Add(b);
                }
            }

            return blobList;
        }

        /// <summary>
        /// List containers from url.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        public List<BasicBlobContainer> ListContainers(string baseUrl)
        {
            var client = AzureHelper.GetSourceCloudBlobClient(baseUrl);
            var containers = client.ListContainers();
            var containerList = containers.Select(container => new BasicBlobContainer { BlobType = BlobEntryType.Container, Container = "", DisplayName = container.Name, Name = container.Name }).ToList();
            return containerList;
        }

        /// <summary>
        /// Read blob without passing urls. Will construct based on container and blob name.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
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
        // container is possibly a container/bucket in the azure/s3 sense in addition to other directories added
        // on.
        public List<BasicBlobContainer> ListBlobsInContainerSimple(string container)
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            var url = baseUrl + "/" + container + "/";
            return ListBlobsInContainer(url);
        }

        public void MakeContainerSimple(string container)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

           if (string.IsNullOrEmpty(container))
            {
                throw new ArgumentNullException("container name empty");
            }

            var fullUrl = baseUrl +"/" + container;
            MakeContainer(fullUrl);

        }

    }
}
