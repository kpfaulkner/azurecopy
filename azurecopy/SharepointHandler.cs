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
using System.Threading.Tasks;

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

        /// <summary>
        /// Return reference to files in Sharepoint.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private Microsoft.SharePoint.Client.File GetSharepointFile(string documentURL)
        {

            var context = GetContext(documentURL);

            // get file collection.
            var fileCollection = GetSharepointFileCollection(documentURL);
            ctx.Load(fileCollection);
            ctx.ExecuteQuery();

            try
            {
                var f = Microsoft.SharePoint.Client.File.OpenBinaryDirect(ctx, "/Shared Documents/ken.txt");

                var fs = new FileStream("c:\\temp\\ken.file", FileMode.Create);
                f.Stream.CopyTo(fs);
                f.Stream.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                var a = ex;
            }

            //Microsoft.SharePoint.Client.File.SaveBinaryDirect( ctx, documentURL, myStream, true);
            //ctx.Load(docSetFile);
            //ctx.ExecuteQuery();
            return null;
            //return docSetFile;
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

            WriteBlobSimple(url, blob, parallelUploadFactor, chunkSizeInMB);

        }

        public List<BasicBlobContainer> ListBlobsInContainer(string url)
        {
            var context = GetContext(url);

            return ListBlobsInContainerSimple(url);

        }

        // not passing url. Url will be generated behind the scenes.
        public Blob ReadBlobSimple(string url, string blobName, string filePath = "")
        {
            var context = GetContext(url);

            var spFile = GetSharepointFile("https://faulkner.sharepoint.com/Shared Documents/");
   
            return null;
        }

        // not passing url.
        public void WriteBlobSimple(string url, Blob blob, int parallelUploadFactor = 1, int chunkSizeInMB = 4)
        {
            var context = GetContext(url);

            // get file collection.
            var fileCollection = GetSharepointFileCollection(url);

            byte[] data;
            Stream inputStream = null;
            // get stream to data.
            if (blob.BlobSavedToFile)
            {
                inputStream = new FileStream(blob.FilePath, FileMode.Open);
                var length = inputStream.Length;
                var lengthInt = Convert.ToInt32(length);
                data = new byte[lengthInt];
                inputStream.Read(data, 0, lengthInt);

            }
            else
            {
                data = blob.Data;
            }


            //populate information about the new file
            FileCreationInformation fci = new FileCreationInformation();
            fci.Url = blob.Name;
            fci.Content = data;
            fci.Overwrite = true;


            //add this file to the file collection
            Microsoft.SharePoint.Client.File newFile = fileCollection.Add(fci);


            // probably need to do this elsewhere from Azure Storage.
            // but leave here until I know for sure.
            var t = Task.Factory.StartNew(() =>
            {
                ctx.Load(newFile);

                ctx.ExecuteQuery();

            });

            // wait... this is incase the application finishes and exits before the async upload is complete.
            if (true)
            {
                t.Wait();
            }

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
