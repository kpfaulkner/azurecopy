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
        public static bool IsEmulator { get; set; }

        // need to check overhead of creating this constantly.
        // maybe static this later.
        private CloudBlobClient client;

        private string defaultContainerName { get; set; }
        private string defaultBlobPrefix { get; set; }

        /// <summary>
        /// base url is mandatory.
        /// </summary>
        /// <param name="url"></param>
        public AzureHandler(string url)
        {
            baseUrl = url;
            defaultContainerName = GetContainerNameFromUrl(url);
            defaultBlobPrefix = GetBlobPrefixFromUrl(url);

            client = AzureHelper.GetSourceCloudBlobClient(url);

            if (AzureHelper.IsDevUrl(url))
            {
                IsEmulator = true;
            }
        }

       
        /// <summary>
        /// Make new Azure container. 
        /// Assumption being last part of url is the new container.
        /// </summary>
        /// <param name="url"></param>
        public void MakeContainer(string containerName)
        {   
            var container = client.GetContainerReference(containerName);
            container.CreateIfNotExists();
        }

        /// <summary>
        /// Read blob.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <param name="cacheFilePath"></param>
        /// <returns></returns>
        public Blob ReadBlob(string containerName, string blobName, string cacheFilePath = "")
        {
            try
            {
                Blob blob = null;
                var container = client.GetContainerReference(containerName);
                var blobRef = container.GetBlobReferenceFromServer(blobName);
                var isBlockBlob = (blobRef.BlobType == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
                if (isBlockBlob)
                {
                    blob = ReadBlockBlob(blobRef, cacheFilePath);
                }
                else
                {
                    blob = ReadPageBlob(blobRef, cacheFilePath);
                }

                blob.BlobOriginType = UrlType.Azure;
                return blob;
            }
            catch (Exception ex)
            {
                throw new CloudReadException("AzureHandler:ReadBlob unable to read blob", ex);
            }
        }

        /// <summary>
        /// Write blob
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <param name="blob"></param>
        /// <param name="parallelUploadFactor"></param>
        /// <param name="chunkSizeInMB"></param>
        public void WriteBlob(string containerName, string blobName, Blob blob,  int parallelUploadFactor=1, int chunkSizeInMB=4)
        {
            Stream stream = null;

            try
            {
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
                    WriteBlockBlob(stream, blobName, blob, container, parallelUploadFactor, chunkSizeInMB);
                }
                else if (blob.BlobType == DestinationBlobType.Page)
                {
                    WritePageBlob(stream, blobName, blob, container);
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
        /// Move blob from one container to another.
        /// </summary>
        /// <param name="startUrl"></param>
        /// <param name="finishUrl"></param>
        public void MoveBlob(string originContainer, string destinationContainer, string startBlobname)
        {
            throw new NotImplementedException("MoveBlob not implemented");
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
        /// Lists all blobs in a container.
        /// Can be supplied a blobPrefix which basically acts as virtual directory options.
        /// eg, if we have blobs called: "virt1/virt2/myblob"    and
        ///                              "virt1/virt2/myblob2"
        /// Although the blob names are the complete strings mentioned above, we might like to think that the blobs
        /// are just called myblob and myblob2. We can supply a blobPrefix of "virt1/virt2/" which we can *think* of
        /// as a directory, but again, its just really a prefix behind the scenes.
        /// 
        /// For other sytems (not Azure) the blobPrefix might be real directories....  will need to investigate
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobPrefix"></param>
        /// <returns></returns>
        public List<BasicBlobContainer> ListBlobsInContainer(string containerName= null, string blobPrefix = null)
        {
            var blobList = new List<BasicBlobContainer>();
            IEnumerable<IListBlobItem> azureBlobList;
            CloudBlobContainer container;

            if (string.IsNullOrWhiteSpace(containerName))
            {
                containerName = defaultContainerName;
            }

            if (string.IsNullOrWhiteSpace(blobPrefix))
            {
                blobPrefix = defaultBlobPrefix;
            }

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
                    b.DisplayName = AzureHelper.GetDisplayName(cont.Name);
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
                if (string.IsNullOrEmpty(blobPrefix))
                {
                    // add blobs
                    azureBlobList = container.ListBlobs(useFlatBlobListing: true);
                }
                else
                {
                    var vd = container.GetDirectoryReference(blobPrefix);
                    azureBlobList = vd.ListBlobs();
                }

                foreach (var blob in azureBlobList)
                {
                    var b = new BasicBlobContainer();
                    var bn = AzureHelper.GetBlobFromUrl(blob.Uri.AbsoluteUri);
                    b.Name = bn;
                    b.DisplayName = AzureHelper.GetDisplayName(bn);
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
        public List<BasicBlobContainer> ListContainers(string root)
        {
            var containers = client.ListContainers();
            var containerList = containers.Select(container => new BasicBlobContainer { BlobType = BlobEntryType.Container, Container = "", DisplayName = container.Name, Name = container.Name }).ToList();
            return containerList;
        }

        /// <summary>
        /// Gets container name from the full url.
        /// URL format for Azure is:
        ///   https://accountname.blob.core.windows.net/containername/blobname
        ///   OR
        ///   https://accountname.blob.core.windows.net/containername/virt1/virt2/blobname
        ///   
        /// In the second case the TRUE blob name is still "virt1/virt2/blobname" but we'll be handling virtual directories
        /// in some fashion.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetContainerNameFromUrl(string url)
        {
            if (IsEmulator)
            {
                var sp = url.Split('/');
                return sp[4];
            }
            else
            {
                var sp = url.Split('/');
                return sp[3];
            }
        }

        private string GetBlobPrefixFromUrl(string url)
        {
            if (IsEmulator)
            {
                var sp = url.Split('/');
                return string.Join("/", sp.Skip(5));
            }
            else
            {
                var sp = url.Split('/');
                return string.Join("/", sp.Skip(4));
            }
        }




        /// <summary>
        /// Gets blob name from the full url.
        /// URL format for Azure is:
        ///   https://accountname.blob.core.windows.net/containername/blobname
        ///   OR
        ///   https://accountname.blob.core.windows.net/containername/virt1/virt2/blobname
        ///   
        /// In the second case the TRUE blob name is still "virt1/virt2/blobname" but we'll be handling virtual directories
        /// in some fashion.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetBlobNameFromUrl(string url)
        {
            var sp = url.Split('/');
            if (IsEmulator)
            {
                var blobNameElements = sp.Skip(5);
                var blobName = string.Join("/", blobNameElements);
                return blobName;

            }
            else
            {

                var blobNameElements = sp.Skip(4);
                var blobName = string.Join("/", blobNameElements);
                return blobName;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Read block blob.
        /// </summary>
        /// <param name="blobRef"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private Blob ReadBlockBlob(ICloudBlob blobRef, string fileName = "")
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
            catch (Exception ex)
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
            long chunkSize = chunkSizeInMB * 1024 * 1024;
            var length = stream.Length;

            if (chunkSize > length)
            {
                chunkSize = length / parallelFactor;
            }

            var numberOfBlocks = (length / chunkSize) + 1;
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
        private void WriteBlockBlob(Stream stream, string blobName, Blob blob, CloudBlobContainer container, int parallelFactor = 1, int chunkSizeInMB = 2)
        {
            var blobRef = container.GetBlockBlobReference(blobName);
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
        private void WritePageBlob(Stream stream, string blobName, Blob blob, CloudBlobContainer container, int parallelFactor = 1, int chunkSizeInMB = 2)
        {
            var blobRef = container.GetPageBlobReference(blobName);
            blobRef.UploadFromStream(stream);
        }
    }
}
