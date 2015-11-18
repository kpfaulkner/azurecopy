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
        private string baseUrl = null;
        public FileSystemHandler(string url)
        {
            baseUrl = url;
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
        /// Gets container name from the full url.
        /// url will be something like:
        ///     c:\temp\myfile.txt
        ///     or
        ///     c:\temp\temp2\myfile.txt
        ///     
        /// In these cases the blob will be called "myfile.txt" and the container would be
        /// temp or temp\temp2
        /// 
        /// Need to figure out if this should just be a single string OR a collection of strings?
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetContainerNameFromUrl(string url)
        {
            var root = Path.GetPathRoot(url);
            //var container = url.Substring(root.Length);
            var container = Path.GetDirectoryName( url);  // should we still keep the drive? Maybe!
            return container;
        }

        /// <summary>
        /// Gets blob name from the full url.
        /// This is cloud specific.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetBlobNameFromUrl(string url)
        {
            return Path.GetFileName(url);
        }

        /// <summary>
        /// Make container/directory (depending on platform).
        /// For local filesystem the containername is really a full path.
        /// </summary>
        /// <param name="container"></param>
        public void MakeContainer(string containerName)
        {
            Directory.CreateDirectory(containerName);
        }

        public string GetBaseUrl()
        {
            return baseUrl;
        }

        // override configuration. 
        public void OverrideConfiguration(Dictionary<string, string> configuration)
        {
            throw new NotImplementedException("OverrideConfiguration not implemented yet");
        }

        /// <summary>
        /// Read blob.
        /// For filesystem the containerName is really just the full path directory.
        /// The blobName is the filename.
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="cacheFilePath"></param>
        /// <returns></returns>
        public Blob ReadBlob(string containerName, string blobName, string cacheFilePath = "")
        {
            var blob = new Blob();
            blob.BlobSavedToFile = false;   // false since we're not caching it elsewhere... but have it REALLY on FS.
            blob.BlobType = DestinationBlobType.Unknown;
            blob.FilePath = Path.Combine( new List<string>{containerName, blobName}.ToArray());
            blob.BlobOriginType = UrlType.Local;
            blob.BlobSavedToFile = true;
            blob.Name = blobName;
            return blob;
        }

        /// <summary>
        /// List containers/directories off the root. For storage schemes that allow real directories maybe
        /// the root will be 
        /// </summary>
        /// <returns></returns>
        public List<BasicBlobContainer> ListContainers(string root)
        {
            throw new NotImplementedException("Filesystem list containers not implemented");
        }


        /// <summary>
        /// Write blob
        /// For FS the containerName is just the full path (excluding filename).
        /// Need to create directories where appropriate.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="blobName"></param>
        /// <param name="blob"></param>
        /// <param name="parallelUploadFactor"></param>
        /// <param name="chunkSizeInMB"></param>
        public void WriteBlob(string containerName, string blobName, Blob blob, int parallelUploadFactor = 1, int chunkSizeInMB = 4)
        {
            Stream stream = null;
            try
            {
                var outFile = Path.Combine(containerName, blobName);

                var directory = Path.GetDirectoryName(outFile);
                Directory.CreateDirectory(directory);

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
        public List<BasicBlobContainer> ListBlobsInContainer(string containerName = null, string blobPrefix = null, bool debug = false)
        {
            var fileList = new List<BasicBlobContainer>();

            var files = Directory.EnumerateFiles(baseUrl);

            foreach( var file in files)
            {
                var f = new BasicBlobContainer();

                var name = Path.GetFileName(file);

                f.BlobType = BlobEntryType.Blob;
                f.DisplayName = name;
                f.Url = file;
                f.Name = name;

                fileList.Add(f);
            }
            return fileList;
        }
    }
}
