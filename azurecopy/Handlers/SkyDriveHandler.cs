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
    public class SkyDriveHandler : IBlobHandler
    {

        private string accessToken;
        private string baseUrl = "";

        // store so we dont have to keep retrieving it.
        private static Datatypes.SkyDriveDirectory destinationDirectory = null;

        public SkyDriveHandler( string url )
        {
            accessToken = SkyDriveHelper.GetAccessToken();
            baseUrl = url;
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
            var sp = url.Split('/');
            return sp[3];
        }


        public string GetBaseUrl()
        {
            return baseUrl;
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

        // override configuration. 
        public void OverrideConfiguration(Dictionary<string, string> configuration)
        {
            throw new NotImplementedException("OverrideConfiguration not implemented yet");
        }

        /// <summary>
        /// Make container/directory (depending on platform).
        /// </summary>
        /// <param name="container"></param>
        public void MakeContainer(string containerName)
        {
            var url = baseUrl + "/" + containerName;
            url = url.Replace(SkyDriveHelper.OneDrivePrefix, "");
            var targetDirectory = SkyDriveHelper.CreateFolder(url);

        }

        /// <summary>
        /// List containers/directories off the root. For storage schemes that allow real directories maybe
        /// the root will be 
        /// </summary>
        /// <returns></returns>
        public List<BasicBlobContainer> ListContainers(string root)
        {
            throw new NotImplementedException("Onedrive list containers not implemented");
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
            Blob blob = new Blob();
            StringBuilder requestUriFile;

            var url = baseUrl + "/" + containerName + "/" + blobName;

            if (url.IndexOf(SkyDriveHelper.OneDrivePrefix) != -1)
            {
                url = url.Replace(SkyDriveHelper.OneDrivePrefix, "");
                var skydriveFileEntry = SkyDriveHelper.GetSkyDriveEntryByFileNameAndDirectory(url);
                requestUriFile = new StringBuilder(skydriveFileEntry.Source);
                var sp = url.Split('/');
                blobName = sp[sp.Length - 1];
            }
            else
            {
                // get blob name from url then remove it.
                // ugly ugly hack.
                var sp = url.Split('=');
                blobName = sp.Last();

                var lastIndex = url.LastIndexOf("&");

                requestUriFile = new StringBuilder(url.Substring(0, lastIndex));
                // manipulated url to include blobName="real blob name". So parse end off url.
                // ugly hack... but needed a way for compatibility with other handlers.
            }

            requestUriFile.AppendFormat("?access_token={0}", accessToken);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUriFile.ToString());
            request.Method = "GET";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var s = response.GetResponseStream();

            // get stream to store.
            using (var stream = CommonHelper.GetStream(cacheFilePath))
            {
                byte[] data = new byte[32768];
                int bytesRead = 0;
                do
                {
                    bytesRead = s.Read(data, 0, data.Length);
                    stream.Write(data, 0, bytesRead);
                }
                while (bytesRead > 0);

                if (!blob.BlobSavedToFile)
                {
                    var ms = stream as MemoryStream;
                    blob.Data = ms.ToArray();
                }

            }

            blob.Name = blobName;
            blob.BlobOriginType = UrlType.SkyDrive;
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
        public void WriteBlob(string containerName, string blobName, Blob blob, int parallelUploadFactor = 1, int chunkSizeInMB = 4)
        {
            var url = baseUrl + "/" + containerName + "/" + blob.Name;
            url = url.Replace( SkyDriveHelper.OneDrivePrefix, "");

            if (destinationDirectory == null)
            {
                // check if target folder exists.
                // if not, create it.
                destinationDirectory = SkyDriveHelper.GetSkyDriveDirectory(url);

                if (destinationDirectory == null)
                {
                    destinationDirectory = SkyDriveHelper.CreateFolder(url);
                }
            }
           
            var urlTemplate = @"https://apis.live.net/v5.0/{0}/files/{1}?access_token={2}";
            var requestUrl = string.Format(urlTemplate, destinationDirectory.Id, blobName, accessToken);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            request.Method = "PUT";
            Stream dataStream = request.GetRequestStream();
            Stream inputStream = null;

            // get stream to data.
            if (blob.BlobSavedToFile)
            {
                inputStream = new FileStream(blob.FilePath, FileMode.Open);
            }
            else
            {
                inputStream = new MemoryStream(blob.Data);
            }

            int bytesRead;
            int readSize = 64000;
            int totalSize = 0;
            byte[] arr = new byte[readSize];
            do
            {
                bytesRead = inputStream.Read( arr,0, readSize);
                if (bytesRead > 0)
                {
                    totalSize += bytesRead;
                    dataStream.Write(arr, 0, bytesRead);
                }
            }
            while (  bytesRead > 0);
         
            dataStream.Close();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            response.Close();
        }

        // assuming only single dir.
        // url == directory/blobname
        private string GetDirectoryNameFromUrl(string url)
        {
            var sp = url.Split('/');
            return sp[2];
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
        public List<BasicBlobContainer> ListBlobsInContainer(string containerName = null, string blobPrefix = null)
        {
            var blobList = new List<BasicBlobContainer>();

            var skydriveListing = SkyDriveHelper.ListSkyDriveDirectoryContent(containerName);
            foreach (var skyDriveEntry in skydriveListing)
            {
                var blob = new BasicBlobContainer();
                blob.Name = skyDriveEntry.Name;

                var resolvedOneDriveEntry = SkyDriveHelper.ListSkyDriveFileWithUrl(skyDriveEntry.Id);

                // keep display name same as name until determine otherwise.
                blob.DisplayName = blob.Name;
                blob.Container = containerName;
                blob.Url = string.Format("{0}&blobName={1}", resolvedOneDriveEntry.Source, blob.Name);       // modify link so we can determine blob name purely from link.             
                blobList.Add(blob);
            }
            return blobList;

        }

        private string GetSkyDriveDirectoryId(string directoryName)
        {
            var skydriveListing = SkyDriveHelper.ListSkyDriveRootDirectories();

            var skydriveId = (from e in skydriveListing where e.Name == directoryName select e.Id).FirstOrDefault();

            return skydriveId;

        }


    }
}
