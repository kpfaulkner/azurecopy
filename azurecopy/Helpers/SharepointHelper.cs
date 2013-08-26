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
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using azurecopy.ThirdParty;
using Microsoft.SharePoint.Client;

namespace azurecopy.Utils
{
    public static class SharepointHelper
    {

        static MsOnlineClaimsHelper msOnlineHelper = null;

        static SharepointHelper()
        {
           
        }

        public static ClientContext GetContext(string url)
        {
            var username = ConfigHelper.SharepointUsername;
            var password = ConfigHelper.SharepointPassword;

            if (msOnlineHelper == null)
            {
                msOnlineHelper = new MsOnlineClaimsHelper(username, password, url);
            }

            ClientContext ctx = new ClientContext(url);
            ctx.ExecutingWebRequest += new EventHandler<WebRequestEventArgs>(ctx_ExecutingWebRequest);

            //Upload(ctx, "https://faulkner.sharepoint.com/Shared Documents", "c:\\temp\\test.txt");

            return ctx;
        }

        private static void ctx_ExecutingWebRequest(object sender, WebRequestEventArgs e)
        {
            e.WebRequestExecutor.WebRequest.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            e.WebRequestExecutor.WebRequest.CookieContainer = msOnlineHelper.CookieContainer;
        }

        private static FileCollection GetSharepointFileCollection(ClientContext ctx, string documentURL)
        {
            Web site = ctx.Web;

            //get the document library folder
            Folder docSetFolder = site.GetFolderByServerRelativeUrl(documentURL);
            ctx.ExecuteQuery();


            //load the file collection for the documents in the library
            FileCollection documentFiles = docSetFolder.Files;
            ctx.Load(documentFiles);
            ctx.ExecuteQuery();

            return docSetFolder.Files;
        }


        public static bool MatchHandler(string url)
        {
            return url.Contains("sharepoint");
        }


    }
}
