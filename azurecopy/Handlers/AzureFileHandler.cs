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

        public AzureFileHandler(string url = null)
        {
            baseUrl = url;
        }

        public void MoveBlob( string startUrl, string finishUrl)
        {

            throw new NotImplementedException("MoveBlob not implemented");

        }

        // make container (ie file directory).
        public void MakeContainer(string url)
        {
            GetContainer(url);
        }

        public List<BasicBlobContainer> ListContainers(string baseUrl)
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

        // default is no filepath.
        // make parallel download later.
        public Blob ReadBlob(string url, string filePath = "")
        {
            Blob blob = null;

            var client = AzureHelper.GetSourceCloudBlobClient( url );
            var containerName = AzureHelper.GetContainerFromUrl(url);
        
            var container = client.GetContainerReference(containerName);
            //container.CreateIfNotExists();

            var blobRef = client.GetBlobReferenceFromServer( new Uri( url ) );
            
            // are we block?
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


        public void WriteBlob(string url, Blob blob,   int parallelUploadFactor=1, int chunkSizeInMB=4)
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
