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

            return blob;
        }



        private Blob ReadBlockBlob(ICloudBlob blobRef, string fileName = "" )
        {
            var blob = new Blob();
            blob.BlobSavedToFile = !string.IsNullOrEmpty(fileName);
            blob.Name = blobRef.Name;
            blob.IsBlockBlob = true;
            blob.FilePath = fileName;

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
            blob.IsBlockBlob = false;
            blob.FilePath = fileName;

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


        public void WriteBlob(string url, Blob blob)
        {
            Stream stream = null;

            try
            {
                var client = AzureHelper.GetTargetCloudBlobClient(url);

                var containerName = AzureHelper.GetContainerFromUrl(url);
                var blobName = AzureHelper.GetBlobFromUrl(url);
                blob.Name = blobName;

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

                if (blob.IsBlockBlob)
                {
                    WriteBlockBlob(stream, blob, container);   
                }
                else
                {
                    WritePageBlob(stream, blob, container);
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

        // can make this concurrent... soonish. :)
        private void WriteBlockBlob(Stream stream, Blob blob, CloudBlobContainer container)
        {

            var blobRef = container.GetBlockBlobReference(blob.Name);

            blobRef.UploadFromStream(stream);

        }

        private void WritePageBlob(Stream stream, Blob blob, CloudBlobContainer container)
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
