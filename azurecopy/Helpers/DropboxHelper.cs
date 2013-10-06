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

using azurecopy.Datatypes;
using DropNet;
using DropNet.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace azurecopy.Helpers
{
    public class DropboxHelper
    {
        const string DropboxDetection = "dropbox";

        static DropNetClient client = null;

        static DropboxHelper()
        {
            if (client == null)
            {
                client = new DropNetClient(ConfigHelper.DropBoxAPIKey, ConfigHelper.DropBoxAPISecret);
            }

        }

        public static string BuildAuthorizeUrl()
        {
            client.GetToken();
            var url = client.BuildAuthorizeUrl();
            return url;
        }

        public static Tuple<string,string>  GetAccessToken()
        {
            var userLogin = client.GetAccessToken();

            return new Tuple<string, string>(userLogin.Secret, userLogin.Token);
        }

        public static bool MatchHandler(string url)
        {
            return url.Contains(DropboxDetection);
        }

   
    }
}
