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

using azurecopy.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    public class AzureFileHandler : IBlobHandler
    {
        private string baseUrl = null;

        private CloudBlobClient client;

        /// <summary>
        /// base url HAS to be passed in.
        /// </summary>
        /// <param name="url"></param>
        public AzureFileHandler(string url)
        {
            baseUrl = url;
            client = AzureHelper.GetSourceCloudBlobClient(url);
        }

        /// <summary>
        /// Make container/directory (depending on platform).
        /// </summary>
        /// <param name="container"></param>
        public void MakeContainer(string containerName)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets container name from the full url.
        /// This is cloud specific.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetContainerNameFromUrl(string url)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets blob name from the full url.
        /// This is cloud specific.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetBlobNameFromUrl(string url)
        {
            throw new NotImplementedException();
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
            Blob blob = null;
            var container = client.GetContainerReference(containerName);
            var blobRef = container.GetBlobReferenceFromServer(blobName);

            // are we block?
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
            catch (ArgumentException ex)
            {
                // probably bad container.
                Console.WriteLine("Argument Exception " + ex.ToString());
                throw;
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
        /// Move blob
        /// </summary>
        /// <param name="originContainer"></param>
        /// <param name="destinationContainer"></param>
        /// <param name="startBlobname"></param>
        public void MoveBlob(string originContainer, string destinationContainer, string startBlobname)
        {
            throw new NotImplementedException("MoveBlob not implemented");
        }

        /// <summary>
        /// List containers/directories off the root. For storage schemes that allow real directories maybe
        /// the root will be 
        /// </summary>
        /// <returns></returns>
        public List<BasicBlobContainer> ListContainers(string root)
        {
            throw new NotImplementedException("Azure File Handler list containers not implemented");
        }

        // override configuration. 
        public void OverrideConfiguration(Dictionary<string, string> configuration)
        {
            throw new NotImplementedException("OverrideConfiguration not implemented yet");
        }

        // container can be subdirectories and NOT just at root level.
        // url is full url https://myaccount.file.core.windows.net/mydirectory/myfile OR
        //  https://myaccount.file.core.windows.net/mydirectory/ <--- note trailing forward slash!
        public CloudFileDirectory GetContainer(string url)
        {
            var uri = new Uri(url);
            var client = AzureHelper.GetCloudFileClient(url, false);

            // no idea what share reference is for. FIXME
            var share = client.GetShareReference("bar");
            share.CreateIfNotExists();
            var rootDirectory = share.GetRootDirectoryReference();

            var containerName = string.Empty;
            for( var i = 1 ; i < uri.Segments.Length - 1; i++)
            {
                containerName += uri.Segments[i];
            }

            var container = rootDirectory.GetDirectoryReference(containerName);

            return container;
        }


        public string GetBaseUrl()
        {
            return baseUrl;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private Blob ReadBlockBlob(ICloudBlob blobRef, string fileName = "" )
        {
            var blob = new Blob();
            blob.BlobSavedToFile = !string.IsNullOrEmpty(fileName);
            blob.Name = blobRef.Name;
            blob.FilePath = fileName;
            blob.BlobType = DestinationBlobType.Block;
            
            var blockBlob = blobRef as CloudBlockBlob;

            // get stream to store.
            using (var stream = CommonHelper.GetStream(fileName))
            {
                // no parallel yet.
                blockBlob.DownloadToStream(stream);

                if (!blob.BlobSavedToFile)
                {
                    var ms = stream as MemoryStream;
                    blob.Data = ms.ToArray();
                }
            }

            return blob;
        }

        private Blob ReadPageBlob(ICloudBlob blobRef, string fileName = "")
        {
            var blob = new Blob();
            blob.BlobSavedToFile = !string.IsNullOrEmpty(fileName);
            blob.Name = blobRef.Name;
            blob.FilePath = fileName;
            blob.BlobType = DestinationBlobType.Page;
            var pageBlob = blobRef as CloudPageBlob;

            // get stream to store.
            using (var stream = CommonHelper.GetStream(fileName))
            {

                // no parallel yet.
                pageBlob.DownloadToStream(stream);

                if (!blob.BlobSavedToFile)
                {
                    var ms = stream as MemoryStream;
                    blob.Data = ms.ToArray();
                }
            }

            return blob;
        }


        /// <summary>
        /// Upload in parallel.
        /// If total size of file is smaller than chunkSize, then simply split length by parallel factor.
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
                    var index = (numberOfBlocks - count -  1);

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
                taskList.RemoveAt(waitedIndex);
            }


            Task.WaitAll(taskList.ToArray());

            blob.PutBlockList(blockIdList.Where(t => t != null));
        }

        // can make this concurrent... soonish. :)
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

        private void WritePageBlob(Stream stream, Blob blob, CloudBlobContainer container,int parallelFactor=1, int chunkSizeInMB=2)
        {
            var blobRef = container.GetPageBlobReference(blob.Name);
            blobRef.UploadFromStream(stream);

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
        /// <param name="containerName"></param>
        /// <param name="blobPrefix"></param>
        /// <returns></returns>
        public IEnumerable<BasicBlobContainer> ListBlobsInContainer(string containerName = null, string blobPrefix = null, bool debug = false)
        {            
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
                    b.DisplayName = AzureHelper.GetDisplayName(cont.Name);
                    b.Container = "";
                    b.Url = cont.Uri.AbsoluteUri;
                    b.BlobType = BlobEntryType.Container;
                    yield return b;
                }
            }
            else
            {
                container = client.GetContainerReference(containerName);

                // if we were only passed the container name, then list contents of container.
                if (string.IsNullOrEmpty(blobPrefix))
                {
                    // add blobs
                    azureBlobList = container.ListBlobs(useFlatBlobListing:true);
                   
                }
                else
                {
                    throw new NotImplementedException();

                    // if passed virtual directory information, then filter based off that.
                    //var vd = container.GetDirectoryReference(virtualDirectoryName);
                    //azureBlobList = vd.ListBlobs();
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
                    yield return b;
                }
            }
        }

        // not passing url. Url will be generated behind the scenes.
        public Blob ReadBlobSimple(string container, string blobName, string filePath = "")
        {
            if (baseUrl == null)
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            var url = baseUrl + "/" + container + "/" + blobName;
            return ReadBlob(url, filePath);
        }
    }
}
