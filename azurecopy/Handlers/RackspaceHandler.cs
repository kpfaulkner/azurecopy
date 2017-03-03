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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace azurecopy
{
    public class RackspaceHandler : IBlobHandler
    {
        private string baseUrl = null;

        public RackspaceHandler(string url )
        {
        
        }

        /// <summary>
        /// Does blob exists in container
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public bool DoesBlobExists(string containerName, string blobName)
        {
            var exists = false;

            return exists;
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }


        /// <summary>
        /// Move blob
        /// </summary>
        /// <param name="originContainer"></param>
        /// <param name="destinationContainer"></param>
        /// <param name="startBlobname"></param>
        public void MoveBlob(string originContainer, string destinationContainer, string startBlobname)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }


        /// <summary>
        /// List containers/directories off the root. For storage schemes that allow real directories maybe
        /// the root will be 
        /// </summary>
        /// <returns></returns>
        public List<BasicBlobContainer> ListContainers(string root)
        {
            throw new NotImplementedException();
        }


        // core URL used for this handler.
        // could be the Azure url for a given account, or the bucket/S3 url.
        // This is purely so once a url has been used to establish a handler we can still
        // reference it for the copy methods.
        // ideally the copy methods would access this automatically (and not require urls in params)
        // but this modification will probably happen slowly.
        public string GetBaseUrl()
        {
            throw new NotImplementedException();
        }


        // override configuration, instead of using app.configs.
        public void OverrideConfiguration(Dictionary<string, string> configuration)
        {
            throw new NotImplementedException();
        }
    }
}
