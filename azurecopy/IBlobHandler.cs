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
 
 using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    /// <summary>
    /// Interface for reading and writing blobs.
    /// For both reading and writing there are multiple options.
    /// You can either specify the entire url that you want to read/write too
    /// OR
    /// You can just specify the blob you want to write, the container you want to write too and the handler 
    /// itself creates the url for you. This may be easier in the long run (no messing about with urls) but
    /// will keep full url option there for those who want more power.
    /// </summary>
    public interface IBlobHandler
    {
        // expected to pass entire url.
        Blob ReadBlob(string url, string filePath = "");
        
        // not passing url. Url will be generated behind the scenes.
        Blob ReadBlobSimple(string container, string blobName, string filePath = "");

        // expected to pass entire url
        void WriteBlob(string url, Blob blob,  int parallelUploadFactor=1, int chunkSizeInMB=4);

        // not passing url.
        void WriteBlobSimple(string container, Blob blob, int parallelUploadFactor = 1, int chunkSizeInMB = 4);

        // passing full url.
        // can contain virtual directories such as https://....../mycontainer/virtualdir1/virtualdir2  
        List<BasicBlobContainer> ListBlobsInContainer(string baseUrl);

        // not required to pass full url.
        // for S3 the container name could be the bucket name.
        // for azure it would be an azure container.
        // for others it would probably be the first directory in a full path listing many directories. (eg. dir1/dir2/dir3/file.txt)
        List<BasicBlobContainer> ListBlobsInContainerSimple(string containerName);

        // core URL used for this handler.
        // could be the Azure url for a given account, or the bucket/S3 url.
        // This is purely so once a url has been used to establish a handler we can still
        // reference it for the copy methods.
        // ideally the copy methods would access this automatically (and not require urls in params)
        // but this modification will probably happen slowly.
        string GetBaseUrl();

    }
}
