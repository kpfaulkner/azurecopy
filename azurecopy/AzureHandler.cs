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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    class AzureHandler : IBlobHandler
    {
        public AzureHandler()
        {

        }

        // default is no filepath.
        // make parallel download later.
        public Blob ReadBlob(string url, string filePath = "")
        {
            Blob blob = null;

            var client = AzureHelper.GetSourceCloudBlobClient( url );
            var containerName = AzureHelper.GetContainerFromUrl(url);
        
            var container = client.GetContainerReference(containerName);
            container.CreateIfNotExists();

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
                //var blobName = AzureHelper.GetBlobFromUrl(url);
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

                if (blob.BlobType == DestinationBlobType.Block)
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
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }

            }

        }

        // NOTE: need to check if we need to modify  blob.ServiceClient.ParallelOperationThreadCount
        private void ParallelWriteBlockBlob(Stream stream, CloudBlockBlob blob, int parallelFactor, int chunkSizeInMB)
        {
            int chunkSize = chunkSizeInMB * 1024*1024;
            var length = stream.Length;
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
                    count--;

                    
                }

                var waitedIndex = Task.WaitAny(taskList.ToArray());
                taskList.RemoveAt(waitedIndex);
            }


            Task.WaitAll(taskList.ToArray());

            blob.PutBlockList(blockIdList);
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


        public List<string> ListBlobsInContainer(string baseUrl)
        {
            var blobList = new List<string>();

            var client = AzureHelper.GetSourceCloudBlobClient(baseUrl);

            var containerName = AzureHelper.GetContainerFromUrl(baseUrl);
            var blobName = AzureHelper.GetBlobFromUrl(baseUrl);
        
            var container = client.GetContainerReference(containerName);

            var azureBlobList = container.ListBlobs();
            foreach (var blob in azureBlobList)
            {
                blobList.Add(blob.Uri.AbsoluteUri);
            }

            return blobList;



        }



    }
}
