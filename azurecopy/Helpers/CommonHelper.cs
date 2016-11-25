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
 
 using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using azurecopy.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace azurecopy.Utils
{
    public static class CommonHelper
    {

        public static Stream GetStream(string fileName)
        {
            Stream stream = null;

            // get stream to data.
            if (!string.IsNullOrEmpty(fileName))
            {
                string directory = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(directory))  Directory.CreateDirectory(directory);

                stream = new FileStream(fileName, FileMode.Create);
            }
            else
            {
                stream = new MemoryStream();
            }

            return stream;
        }

        public static bool IsABlob(string url)
        {
            return !url.EndsWith("/") &&  !url.EndsWith("\\");
        }

        public static BasicBlobContainer BlobToBasicBlobContainer(Blob blob)
        {
            var basicBlob = new BasicBlobContainer()
                {
                    Name = blob.Name,
                    Url = blob.Url,
                    Container = blob.Container,
                    BlobType = BlobEntryType.Blob
                };
            return basicBlob;
        }

        // Allow listing of virtual directories.
        // Azure provides this in the client library already, but will just keep this common functionality here
        // until I can figure out how to do this consistently across the multiple cloud providers.
        public static List<BasicBlobContainer> ListVirtualDirectory(List<BasicBlobContainer> blobList, string virtualDirectory)
        {
            return blobList.Where(b => b.Name.StartsWith(virtualDirectory)).ToList();
        }
    }
}
