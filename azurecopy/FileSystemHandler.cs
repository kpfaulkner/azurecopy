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
    public class FileSystemHandler : IBlobHandler
    {

        // we will NOT be using the cachedFilePath (since we're just reading the file from the local 
        // filesystem anyway).
        // We will reference the file in the blob, but will make sure that the blob url type is set to local
        // this will mean that any future cleaning of the cache will NOT try and erase the original file.
        public Blob ReadBlob(string localFilePath, string cachedFilePath = "")
        {

            var blob = new Blob();
            blob.BlobSavedToFile = false;
            blob.BlobType = DestinationBlobType.Unknown;
            blob.FilePath = localFilePath;
            blob.BlobOriginType = UrlType.Local;
            blob.BlobSavedToFile = true;
            
            var uri = new Uri(localFilePath);

            blob.Name = uri.Segments[uri.Segments.Length - 1];
            return blob;
        }


        public void WriteBlob(string localFilePath, Blob blob,   int parallelUploadFactor=1, int chunkSizeInMB=4)
        {
            Stream stream = null;

            try
            {

                var outFile = Path.Combine(localFilePath, blob.Name);

                // get stream to data.
                if (blob.BlobSavedToFile)
                {
                    stream = new FileStream(blob.FilePath, FileMode.Open);
                }
                else
                {
                    stream = new MemoryStream(blob.Data);
                }

                using (var writeStream = new FileStream(outFile, FileMode.Create))
                {
                    stream.CopyTo(writeStream);
                }

            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Writing to local filesystem failed: " + ex.ToString());
                throw;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

        }

        public List<BasicBlobContainer> ListBlobsInContainer(string baseUrl)
        {
            throw new NotImplementedException();
        }

        public bool MatchHandler(string url)
        {
            return false;
        }

    }
}
