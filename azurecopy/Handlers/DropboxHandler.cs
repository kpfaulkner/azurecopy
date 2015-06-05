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

using azurecopy.Helpers;
using azurecopy.Utils;
using DropNet;
using DropNet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace azurecopy
{
    public class DropboxHandler : IBlobHandler
    {
        private string baseUrl = null;
        private DropNetClient client;
        private UserLogin accessToken;
        private string url;

        // really dont like the idea of storing plain passwords.
        // need to encrypt the app.config soon.
        public DropboxHandler( string url)
        {
            client = DropboxHelper.GetClient();

            baseUrl = url;
        }

        public string GetBaseUrl()
        {
            return baseUrl;
        }


        /// <summary>
        /// Gets container name from the full url.
        /// This is cloud specific.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetContainerNameFromUrl(string url)
        {
            // if ends if / then no blob name.
            if (url.EndsWith("/"))
            {
                var sp = url.Split('/');
                return string.Join("/", sp.Skip(3));
            }
            else
            {
                var sp = url.Split('/');
                var len = sp.Length;
                return string.Join("/", sp.Skip(3).Take(len - 4));   // skip beginning and blob name at the end.

            }
        }

        /// <summary>
        /// Gets blob name from the full url.
        /// This is cloud specific.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetBlobNameFromUrl(string url)
        {
            var sp = url.Split('/');
            return sp.Last();
        }


        /// <summary>
        /// Move blob
        /// </summary>
        /// <param name="originContainer"></param>
        /// <param name="destinationContainer"></param>
        /// <param name="startBlobname"></param>
        public void MoveBlob(string originContainer, string destinationContainer, string startBlobname)
        {
            throw new NotImplementedException("MoveBlob for DropBox not implemented");
        }

       // override configuration, instead of using app.configs.
        public void OverrideConfiguration(Dictionary<string, string> configuration)
        {
            throw new NotImplementedException("OverrideConfiguration not implemented yet");
        }

        /// <summary>
        /// List containers/directories off the root. For storage schemes that allow real directories maybe
        /// the root will be 
        /// </summary>
        /// <returns></returns>
        public List<BasicBlobContainer> ListContainers(string root)
        {
            throw new NotImplementedException("Dropbox list containers not implemented");
        }

        /// <summary>
        /// Make container/directory (depending on platform).
        /// </summary>
        /// <param name="container"></param>
        public void MakeContainer(string containerName)
        {
            client.CreateFolder(containerName);
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
            var blob = new Blob();

            blob.BlobSavedToFile = !string.IsNullOrEmpty(cacheFilePath);
            blob.FilePath = cacheFilePath;
            blob.BlobOriginType = UrlType.Dropbox;
            blob.Name = blobName;

            // generate path for dropbox.
            var dropboxPath = containerName + "/" + blobName; // FIXME: Need to verify this!!!!

            // get stream to store.
            using (var stream = CommonHelper.GetStream(cacheFilePath))
            {
                var fileBytes = client.GetFile(dropboxPath);
               
                if (!blob.BlobSavedToFile)
                {
                    blob.Data = fileBytes;
                }
                else
                {
                    stream.Write(fileBytes, 0, fileBytes.Length);
                }
            }

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
        public void WriteBlob(string containerName, string blobName, Blob blob,  int parallelUploadFactor=1, int chunkSizeInMB=4)
        {
            if (blob.BlobSavedToFile)
            {
                using( var stream = new FileStream(blob.FilePath, FileMode.Open))
                {
                    client.UploadFile(containerName, blob.Name, stream);
                }
            }
            else
            {
                client.UploadFile(containerName, blob.Name, blob.Data);
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
        /// <param name="containerName"></param>
        /// <param name="blobPrefix"></param>
        /// <returns></returns>
        public List<BasicBlobContainer> ListBlobsInContainer(string containerName = null, string blobPrefix = null)
        {
            var dirListing = new List<BasicBlobContainer>();

            //var metadata = client.GetMetaData(containerName, null, false, false);
            var metadata = client.GetMetaData(containerName);

            // generate list of dirs and files.
            foreach (var entry in metadata.Contents)
            {
                // basic blob info.
                var blob = new BasicBlobContainer();
                blob.Container = containerName;
                blob.DisplayName = entry.Name;
                blob.Name = entry.Name;
                blob.Url = entry.Path;

                if (entry.Is_Dir)
                {
                    blob.BlobType = BlobEntryType.Container;
                }
                else
                {
                    blob.BlobType = BlobEntryType.Blob;
                }
                dirListing.Add(blob);
            }
            return dirListing;
        }
    }
}
