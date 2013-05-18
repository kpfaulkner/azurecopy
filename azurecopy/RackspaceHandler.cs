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

        public RackspaceHandler(string url = null)
        {
        
        }

        public string GetBaseUrl()
        {
            return baseUrl;
        }

        public Blob ReadBlob(string url, string filePath = "")
        {
            throw new NotImplementedException("Sharepoint not implemented yet");
            
        }

        public void WriteBlob(string url, Blob blob,  int parallelUploadFactor=1, int chunkSizeInMB=4)
        {
            throw new NotImplementedException("Sharepoint not implemented yet");
           

        }

        public List<BasicBlobContainer> ListBlobsInContainer(string container)
        {
            throw new NotImplementedException("Sharepoint not implemented yet");

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
            throw new NotImplementedException("Dropbox not implemented yet");

        }

    }
}
