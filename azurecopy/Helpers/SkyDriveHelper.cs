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
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace azurecopy.Helpers
{
    public class SkyDriveHelper
    {
        private static string skyDriveClientId = @"00000000480EE365";
        private static string skyDriveRedirectUri = @"https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf";
        private static string accessToken;
        private static string refreshToken;

        // public just so handler can filter out crap.
        public const string OneDrivePrefix = "one://";

        // determines if this is the first time (and need access and refresh token)
        // or determines if we're just refreshing a refresh token.
        public static string GetAccessToken()
        {
            if ( string.IsNullOrEmpty(ConfigHelper.SkyDriveRefreshToken))
            {
                // no refresh token at all, therefore need to request it.
                GetLiveAccessAndRefreshTokens(ConfigHelper.SkyDriveCode);
                return accessToken;
            }
            else
            {
                // have refresh token... refresh and get new access token.
                RefreshAccessToken();
                return accessToken;
            }

        }

        public static void GetLiveAccessAndRefreshTokens(string code)
        {
            var argTemplate = @"client_id={0}&redirect_uri={1}&code={2}&grant_type=authorization_code";
            var baseUrl = @"https://login.live.com/oauth20_token.srf";

            // does client secret really need to be secret?
            // if so... how do I distribute? FIXME
            var args = string.Format(argTemplate, skyDriveClientId, skyDriveRedirectUri, code);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(baseUrl);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";

            using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(args);
            }


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();

            byte[] arr = new byte[3000];
            stream.Read(arr, 0, 3000);
            var responseString = System.Text.Encoding.Default.GetString(arr);

            ParseAccessRefreshResponse(responseString);
            SaveRefreshTokenToAppConfig();
            ConfigHelper.SkyDriveAccessToken = accessToken;
            ConfigHelper.SkyDriveRefreshToken = refreshToken;
        }


        public static void RefreshAccessToken()
        {
            var argTemplate = @"client_id={0}&redirect_uri=https://oauth.live.com/desktop&grant_type=refresh_token&refresh_token={1}";
            var baseUrl = @"https://login.live.com/oauth20_token.srf";
         
            var args = string.Format(argTemplate, skyDriveClientId, ConfigHelper.SkyDriveRefreshToken);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(baseUrl);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(args);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();

            byte[] arr = new byte[3000];
            stream.Read(arr, 0, 3000);
            var responseString = System.Text.Encoding.Default.GetString(arr);

            ParseRefreshTokenRefreshResponse(responseString);
            SaveRefreshTokenToAppConfig();
            ConfigHelper.SkyDriveAccessToken = accessToken;
            ConfigHelper.SkyDriveRefreshToken = refreshToken;

        }


        // put in real JSON parsing later.
        private static void ParseAccessRefreshResponse(string tokenResponse)
        {
            var sp = tokenResponse.Split(',');
            foreach (var entry in sp)
            {
                if (entry.Contains("access_token"))
                {
                    var sp2 = entry.Split('"');
                    accessToken = sp2[3];
                    ConfigHelper.SkyDriveAccessToken = accessToken;
                }

                if (entry.Contains("refresh_token"))
                {
                    var sp2 = entry.Split('"');
                    refreshToken = sp2[3];
                    ConfigHelper.SkyDriveRefreshToken = refreshToken;

                }

            }
        }

        // put in real JSON parsing later.
        private static void ParseRefreshTokenRefreshResponse(string tokenResponse)
        {
            var sp = tokenResponse.Split(',');
            foreach (var entry in sp)
            {
                if (entry.Contains("access_token"))
                {
                    var sp2 = entry.Split('"');
                    accessToken = sp2[3];
                    ConfigHelper.SkyDriveAccessToken = accessToken;
                }

                if (entry.Contains("refresh_token"))
                {
                    var sp2 = entry.Split('"');
                    refreshToken = sp2[3];
                    ConfigHelper.SkyDriveRefreshToken = refreshToken;

                }

            }

        }

        // add tokens into config
        private static void SaveRefreshTokenToAppConfig()
        {
            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Add an Application Setting.
            config.AppSettings.Settings.Remove("SkyDriveRefreshToken");
            config.AppSettings.Settings.Add("SkyDriveRefreshToken", refreshToken);
            
            // Save the changes in App.config file.
            config.Save(ConfigurationSaveMode.Modified);


        }

        // complete path. eg,  /firstdir/seconddir/thirddir
        public static SkyDriveDirectory CreateFolder(string folderPath)
        {
            // getting root folder.
            var rootList = ListSkyDriveDirectoryWithUrl("");
            string parentId = rootList[0].ParentId;

            var sp = folderPath.Split('/');
            var directory = "";
            SkyDriveDirectory latestDir = null;
            foreach (var folder in sp.Where( x => x != ""))
            {
                directory += "/" + folder;
                var dir = GetSkyDriveDirectory(directory);
                if (dir == null && directory != "")
                {
                    CreateFolder(folder, parentId);
                    dir = GetSkyDriveDirectory(directory);
                }
                parentId = dir.Id;
                latestDir = dir;
            }

            return latestDir;
        }


        // folder and parent id
        public static void CreateFolder(string folderName, string parentId)
        {
            var newFolderData = "{\r\n  \"name\": \"" + folderName + "\",\r\n  \"description\": \"Created by azurecopy\"\r\n}";

            var url = "https://apis.live.net/v5.0/"+parentId+"?access_token=" + ConfigHelper.SkyDriveAccessToken;
            Byte[] arr = System.Text.Encoding.UTF8.GetBytes(newFolderData);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url.ToString());
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = arr.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(arr, 0, arr.Length);
            dataStream.Close();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        }


        public static string GetFolderId(string folderName)
        {
            throw new NotImplementedException();
        }

        public static List<SkyDriveDirectory> ListSkyDriveRootDirectories()
        {
            return ListSkyDriveDirectoryWithUrl("");
        }

        public static List<SkyDriveDirectory> ListSkyDriveDirectoryContent(string dirPath)
        {
            dirPath = dirPath.Replace( SkyDriveHelper.OneDrivePrefix, "");
            var skyId = "";
            if (!string.IsNullOrEmpty(dirPath))
            {
                var skyDriveDirectory = GetSkyDriveDirectory(dirPath);
                skyId = skyDriveDirectory.Id;
            }
            return ListSkyDriveDirectoryWithUrl(skyId);

        }


        public static SkyDriveDirectory GetSkyDriveDirectory(string fullPath)
        {

            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            List<SkyDriveDirectory> skydriveListing;
            SkyDriveDirectory selectedEntry = null;

            // root listing.
            skydriveListing = SkyDriveHelper.ListSkyDriveDirectoryWithUrl("");
            var fileFound = false;
            var sp = fullPath.Split('/');
            var searchDir = "";
            foreach (var entry in sp.Where( x => x != "") )
            {
                var foundEntry = (from e in skydriveListing where e.Name == entry select e).FirstOrDefault();
                if (foundEntry != null && foundEntry.Type == "folder")
                {
                    searchDir = foundEntry.Id + "/";
                    skydriveListing = ListSkyDriveDirectoryWithUrl(searchDir);
                }
                else
                {
                    return null;
                }

                selectedEntry = foundEntry;
            }

            return selectedEntry;
        }

        // fullPath is any number of directories then filename
        // eg. dir1/dir2/dir3/myfile.txt
        public static SkyDriveFile GetSkyDriveEntryByFileNameAndDirectory(string fullPath)
        {
            
            if ( string.IsNullOrEmpty( fullPath ))
            {
                return null;
            }

            List<SkyDriveDirectory> skydriveListing;
            SkyDriveDirectory selectedEntry = null;
            SkyDriveFile selectedFile = null;

            // root listing.
            skydriveListing = SkyDriveHelper.ListSkyDriveDirectoryWithUrl("");
            var fileFound = false;
            var sp = fullPath.Split('/');
            var searchDir = "";
            foreach( var entry in sp)
            {
                var foundEntry = (from e in skydriveListing where e.Name == entry select e).FirstOrDefault();
                if ( foundEntry != null && foundEntry.Type == "folder"  )
                {
                    searchDir = foundEntry.Id + "/";
                    skydriveListing = ListSkyDriveDirectoryWithUrl(searchDir);

                }

                // cant have == "file", since "photos" etc come up as different types.
                if (foundEntry != null && foundEntry.Type != "folder")
                {
                    fileFound = true;
                    var l = ListSkyDriveFileWithUrl(foundEntry.Id);
                    if (l != null )
                    {
                        selectedFile = l;
                    }
                }

                selectedEntry = foundEntry;

            }

            if (!fileFound)
            {
                selectedFile = null;
            }

            return selectedFile;

        }

        public static List<SkyDriveDirectory> ListSkyDriveEntry(string initialUrl)
        {

            var url = GenerateSkyDriveURL(initialUrl);

            return ListSkyDriveDirectoryWithUrl(url);
        }


        public static string GenerateSkyDriveURL(string initialUrl, bool isFile=false)
        {

            //throw new NotImplementedException();

            //var requestUriFile = new StringBuilder("https://apis.live.net/v5.0");
            var urlTemplate = "https://apis.live.net/v5.0{0}";
            var containerStr = "";
            if (string.IsNullOrEmpty(initialUrl))
            {
                containerStr = @"/me/skydrive/";
            }
            else
            {
                containerStr = @"/" + initialUrl;
                if (!containerStr.EndsWith("/"))
                {
                    containerStr += "/";
                }

            }

            if (!isFile)
            {
                urlTemplate += "files";
            }
            var url = string.Format(urlTemplate, containerStr);

            return url;
        }

        public static List<SkyDriveDirectory> ListSkyDriveDirectoryWithUrl(string url)
        {
            var url2 = GenerateSkyDriveURL(url);
            var directory = (Wrapper)ListSkyDriveWithUrl<Wrapper>(url2);
            var results = directory.data;

            return results;
        }

        public static SkyDriveFile ListSkyDriveFileWithUrl(string url)
        {
            var url2 = GenerateSkyDriveURL(url, true);
            var file = (SkyDriveFile)ListSkyDriveWithUrl<SkyDriveFile>(url2);
            return file;
        }

        public static ReturnType ListSkyDriveWithUrl<ReturnType>(string url)
        {
            var requestUriFile = new StringBuilder(url);
            requestUriFile.AppendFormat("?access_token={0}", ConfigHelper.SkyDriveAccessToken);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUriFile.ToString());
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();

            byte[] arr = new byte[20000];
            var bytesRead = 0;
            string responseString = "";
            do
            {
                bytesRead = stream.Read(arr, 0, 20000);
                if (bytesRead > 0)
                {
                    var mystring = System.Text.Encoding.Default.GetString(arr, 0, bytesRead);

                    responseString += mystring;
                }
            }
            while (bytesRead > 0);

            var wrapperResponse = (ReturnType) JsonHelper.DeserializeJsonToObject<ReturnType>(responseString);

            return wrapperResponse;
        }

        public static bool MatchHandler(string url)
        {
            return url.Contains(OneDrivePrefix);
        }

   
    }
}
