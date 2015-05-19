﻿//-----------------------------------------------------------------------
// <copyright >
//    Copyright 2015 Ken Faulkner
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
 
 using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    /// <summary>
    /// Major rewrite. Interface will now deal with containers (directories) and blob names.
    /// Hopefully the only URL mentioned will be in the constructor, which will be the base URL that can be
    /// used to reconstruct full URLs (if needed).
    /// 
    /// Am hoping this will simplify the methods.
    /// </summary>
    public interface IBlobHandler
    {
        /// <summary>
        /// Make container/directory (depending on platform).
        /// </summary>
        /// <param name="container"></param>
        void MakeContainer(string containerName);

        /// <summary>
        /// Read blob.
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="cacheFilePath"></param>
        /// <returns></returns>
        Blob ReadBlob(string containerName, string blobName, string cacheFilePath = "");
        
        /// <summary>
        /// Write blob
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <param name="blob"></param>
        /// <param name="parallelUploadFactor"></param>
        /// <param name="chunkSizeInMB"></param>
        void WriteBlob(string containerName, string blobName, Blob blob,  int parallelUploadFactor=1, int chunkSizeInMB=4);
        
        /// <summary>
        /// Move blob
        /// </summary>
        /// <param name="originContainer"></param>
        /// <param name="destinationContainer"></param>
        /// <param name="startBlobname"></param>
        void MoveBlob(string originContainer, string destinationContainer, string startBlobname);
       
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
        List<BasicBlobContainer> ListBlobsInContainer(string containerName = null, string blobPrefix = null);

        /// <summary>
        /// List containers/directories off the root. For storage schemes that allow real directories maybe
        /// the root will be 
        /// </summary>
        /// <returns></returns>
        List<BasicBlobContainer> ListContainers(string root);
      
        // core URL used for this handler.
        // could be the Azure url for a given account, or the bucket/S3 url.
        // This is purely so once a url has been used to establish a handler we can still
        // reference it for the copy methods.
        // ideally the copy methods would access this automatically (and not require urls in params)
        // but this modification will probably happen slowly.
        string GetBaseUrl();

        // override configuration, instead of using app.configs.
        void OverrideConfiguration(Dictionary<string, string> configuration);
    }
}
