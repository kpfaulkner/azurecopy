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
        public DropboxHandler()
        {
            client = new DropNetClient(ConfigHelper.DropBoxAPIKey, ConfigHelper.DropBoxAPISecret, ConfigHelper.DropBoxUserToken, ConfigHelper.DropBoxUserSecret);            
        }

        public string GetBaseUrl()
        {
            return null;
        }


        public Blob ReadBlob(string url, string filePath = "")
        {
            throw new NotImplementedException("Dropbox not implemented yet");
            
        }

        public void WriteBlob(string url, Blob blob,  int parallelUploadFactor=1, int chunkSizeInMB=4)
        {
            throw new NotImplementedException("Dropbox not implemented yet");
           

        }

        public List<BasicBlobContainer> ListBlobsInContainer(string url)
        {
            // how to strip off prefix to get the container/directory?
            var uri = new Uri(url);
            var container = uri.Segments[0];
            return ListBlobsInContainerSimple(container);
        }

        // not passing url. Url will be generated behind the scenes.
        public Blob ReadBlobSimple(string container, string blobName, string filePath = "")
        {
            throw new NotImplementedException("Dropbox not implemented yet");

        }

        // not passing url.
        public void WriteBlobSimple(string container, Blob blob, int parallelUploadFactor = 1, int chunkSizeInMB = 4)
        {
            throw new NotImplementedException("Dropbox not implemented yet");

        }

        // not required to pass full url.
        public List<BasicBlobContainer> ListBlobsInContainerSimple(string containerName)
        {
            var dirListing = new List<BasicBlobContainer>();

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
