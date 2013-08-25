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
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace azurecopy
{
    public class SharepointHandler : IBlobHandler
    {
        private string baseUrl = null;
        private ClientContext ctx = null;

        public SharepointHandler( string url = null)
        {
            baseUrl = url;
            if (baseUrl != null)
            {
                ctx = SharepointHelper.GetContext(baseUrl);
            }
        }

        private ClientContext GetContext(string url)
        {
            if (ctx == null)
            {
                var uri = new Uri(url);
                baseUrl = uri.Scheme + "://" + uri.Host + "/";
                ctx = SharepointHelper.GetContext(baseUrl);
            }

            return ctx;

        }

        /// <summary>
        /// Return reference to files in Sharepoint.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private FileCollection GetSharepointFileCollection( string documentURL)
        {
            Web site = ctx.Web;

            //get the document library folder
            Folder docSetFolder = site.GetFolderByServerRelativeUrl( documentURL );
            ctx.ExecuteQuery();

            //load the file collection for the documents in the library
            FileCollection documentFiles = docSetFolder.Files;
            ctx.Load(documentFiles);
            ctx.ExecuteQuery();
            
            return docSetFolder.Files;
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

        public List<BasicBlobContainer> ListBlobsInContainer(string url)
        {
            var context = GetContext(url);

            return ListBlobsInContainerSimple(url);

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

        // ontainer name is just appended to base url.
        public List<BasicBlobContainer> ListBlobsInContainerSimple(string url)
        {
            var blobList = new List<BasicBlobContainer>();

            var fileCollection = GetSharepointFileCollection(url);
           
            foreach (var obj in fileCollection)
            {
                // construct properly.s
                var fullUrl = url + "/" + obj.Name;

                //var fullPath = Path.Combine(baseUrl, obj.Key);
                var blob = new BasicBlobContainer();
                blob.Name = obj.Name;
                blob.Url = fullUrl;
                blob.Container = url;
                blob.BlobType = BlobEntryType.Blob;
                blob.DisplayName = blob.Name;  // same as blob name for Sharepoint....

                blobList.Add(blob);
            }


            return blobList;

        }

    }
}
