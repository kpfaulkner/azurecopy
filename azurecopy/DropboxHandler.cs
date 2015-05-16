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
        public DropboxHandler( string url = null)
        {
            client = DropboxHelper.GetClient();

            baseUrl = null;
        }

        public string GetBaseUrl()
        {
            return baseUrl;
        }

        public void MoveBlob(string startUrl, string finishUrl)
        {
            throw new NotImplementedException("MoveBlob for DropBox not implemented");
        }

        // override configuration. 
        public void OverrideConfiguration(Dictionary<string, string> configuration)
        {
            throw new NotImplementedException("OverrideConfiguration not implemented yet");
        }

        public List<BasicBlobContainer> ListContainers(string baseUrl)
        {
            throw new NotImplementedException("Dropbox list containers not implemented");
        }

        // make container
        // assumption being last part of url is the new container.
        public void MakeContainer(string url)
        {
            var uri = new Uri(url);
            var pathUri = uri.PathAndQuery;
            client.CreateFolder(pathUri);
        }


        public Blob ReadBlob(string url, string filePath = "")
        {
            var uri = new Uri(url);
            var pathUri = uri.PathAndQuery;
            
            var blob = new Blob();

            blob.BlobSavedToFile = !string.IsNullOrEmpty(filePath);
            blob.FilePath = filePath;
            blob.BlobOriginType = UrlType.Dropbox;
            blob.Name = uri.Segments[uri.Segments.Length - 1];
            // get stream to store.
            using (var stream = CommonHelper.GetStream(filePath))
            {
                var fileBytes = client.GetFile(pathUri);

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

        // synchronous atm. Async it later.
        public void WriteBlob(string url, Blob blob,  int parallelUploadFactor=1, int chunkSizeInMB=4)
        {

            var uri = new Uri( url);
            var container = uri.PathAndQuery;

            if (blob.BlobSavedToFile)
            {
                using( var stream = new FileStream(blob.FilePath, FileMode.Open))
                {
                    client.UploadFile(container, blob.Name, stream);
                }

            }
            else
            {
                client.UploadFile(container, blob.Name, blob.Data);
            }

        }

        public List<BasicBlobContainer> ListBlobsInContainer(string url)
        {
            // how to strip off prefix to get the container/directory?
            var uri = new Uri(url);
            var container = uri.PathAndQuery;
            return ListBlobsInContainerSimple(container);
        }

        // not passing url. Url will be generated behind the scenes.
        public Blob ReadBlobSimple(string container, string blobName, string filePath = "")
        {
            if (string.IsNullOrEmpty(baseUrl))
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
        public List<BasicBlobContainer> ListBlobsInContainerSimple(string containerName)
        {
            var dirListing = new List<BasicBlobContainer>();

            var metadata = client.GetMetaData( containerName, null,false,false);

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

        public void MakeContainerSimple(string container)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException("Constructor needs base url passed");
            }

            if (string.IsNullOrEmpty(container))
            {
                throw new ArgumentNullException("container is empty/null");
            }

            var url = baseUrl + "/" + container;
            var uri = new Uri(url);
            var pathUri = uri.PathAndQuery;
            client.CreateFolder(pathUri); 
                
                
        }


    }
}
