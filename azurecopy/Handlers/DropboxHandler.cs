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
        public DropNetClient client { get; set; }
        private UserLogin accessToken;
        private string url;
        private string defaultBlobPrefix { get; set; }

        // really dont like the idea of storing plain passwords.
        // need to encrypt the app.config soon.
        public DropboxHandler( string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                // Create default client. Can override later if required.
                client = DropboxHelper.GetClient();
                baseUrl = url;
                defaultBlobPrefix = GetBlobPrefixFromUrl(url);
            }
        }

        public string GetBaseUrl()
        {
            return baseUrl;
        }

        // https://dropbox.com/mydir/
        private string GetBlobPrefixFromUrl(string url)
        {
            var sp = url.Split('/');
            return string.Join("/", sp.Skip(3));
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
            var dirListing = new List<BasicBlobContainer>();
            var containerName = "";
            var blobPrefix = "";

            //var metadata = client.GetMetaData(containerName, null, false, false);
            var metadata = client.GetMetaData();
            
            // generate list of dirs and files.
            foreach (var entry in metadata.Contents)
            {
                // basic blob info.
                var blob = new BasicBlobContainer();
                blob.Container = containerName;
                blob.DisplayName = entry.Name;

                blob.Url = entry.Path;
                blob.BlobPrefix = blobPrefix;

                if (entry.Is_Dir)
                {
                    blob.BlobType = BlobEntryType.Container;
                    blob.Name = entry.Name;
                    dirListing.Add(blob);
                }
                else
                {
                    //var name = entry.Name.StartsWith("/") ? entry.Name : "/" + entry.Name;
                    var name = entry.Name;
                    blob.Name = containerName + name;
                    blob.BlobType = BlobEntryType.Blob;
                    dirListing.Add(blob);
                }

            }
            return dirListing;
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

            // why this confusion? Need to check...  
            var dropboxPath = containerName + "/" + blobName; // FIXME: Need to verify this!!!!
            //var dropboxPath = blobName; // FIXME: Need to verify this!!!!

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
            // strip filesystem slashes...   replace with web based.
            var fullPath = GenerateFullPath(containerName, blobName);
            var dir = GenerateDir(fullPath);
            var fileName = GenerateFileName(fullPath);
            MakeDirectories(dir);
            if (blob.BlobSavedToFile)
            {
                using( var stream = new FileStream(blob.FilePath, FileMode.Open))
                {
                    client.UploadFile(dir, blob.Name, stream);
                }
            }
            else
            {
                client.UploadFile(dir, fileName, blob.Data);
            }
        }

        private string GenerateFileName(string fullPath)
        {
            return fullPath.Split('/').Last();
        }

        private string GenerateDir(string fullPath)
        {
            var sp = fullPath.Split('/');
            return string.Join("/", sp.Take(sp.Length - 1));
        }

        private string GenerateFullPath(string containerName, string blobName)
        {
            var newContainerName = containerName.Replace(@"\",@"/");
            var newBlobName = blobName.Replace(@"\", @"/");

            var fullPath = string.Empty;

            if (newContainerName.EndsWith("/"))
            {
                fullPath = newContainerName + newBlobName;

            }
            else
            {
                fullPath = newContainerName + "/"+newBlobName;
            }
            return fullPath;
        }

        /// <summary>
        /// make directories in dropbox.
        /// Needs to create parent then children.
        /// </summary>
        /// <param name="dir"></param>
        private void MakeDirectories(string dir)
        {
            var dirs = dir.Split(Path.DirectorySeparatorChar);
            var fullDirectory = string.Empty;
            foreach( var d in dirs)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(d))
                    {
                        if (!string.IsNullOrWhiteSpace(fullDirectory))
                        {
                            fullDirectory += "/";
                        }
                        fullDirectory += d;
                        client.CreateFolder(fullDirectory);
                    }
                }
                catch( Exception ex)
                {
                    // dir possibly exists
                    // need to do this better.
                }
            }
        }

        /// <summary>
        /// Lists all blobs in a container.
        /// Recurse directories to get all contents.
        /// Either do it here, or when reading blob (and it turns out to be a directory).
        /// Always recursing might not always be wanted, but think its a good default (for now).
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobPrefix"></param>
        /// <returns></returns>
        public IEnumerable<BasicBlobContainer> ListBlobsInContainer(string containerName = null, string blobPrefix = null, bool debug = false)
        {
            //var metadata = client.GetMetaData(containerName, null, false, false);
            var metadata = client.GetMetaData(path:containerName);

            if (string.IsNullOrWhiteSpace(blobPrefix))
            {
                blobPrefix = defaultBlobPrefix;
            }

            // generate list of dirs and files.
            foreach (var entry in metadata.Contents)
            {
                // basic blob info.
                var blob = new BasicBlobContainer();
                blob.Container = containerName;
                blob.DisplayName = entry.Name;
                
                blob.Url = "https://dropbox.com"+entry.Path;
                blob.BlobPrefix = blobPrefix;
                
                if (entry.Is_Dir)
                {
                    blob.BlobType = BlobEntryType.Container;

                    var newContainerName = string.Empty;
                    if (containerName.EndsWith("/"))
                    {
                        newContainerName = containerName + entry.Name + "/";
                    }
                    else
                    {
                        newContainerName = containerName + "/" + entry.Name + "/";
                    }

                    var recursiveBlobList = ListBlobsInContainer(newContainerName, blobPrefix, debug);
                    foreach( var d in recursiveBlobList)
                    {
                        yield return d;
                    }
                }
                else
                {
                    //var name = entry.Name.StartsWith("/") ? entry.Name : "/" + entry.Name;
                    var name = entry.Name;
                    blob.Name =  name;
                    blob.BlobType = BlobEntryType.Blob;
                    yield return blob;
                }                
            }
        }
    }
}
