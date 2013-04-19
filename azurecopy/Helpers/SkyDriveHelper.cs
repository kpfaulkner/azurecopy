using azurecopy.Datatypes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace azurecopy.Helpers
{
    public class SkyDriveHelper
    {
        private static string skyDriveClientId = @"00000000480EE365";
        private static string skyDriveRedirectUri = @"http://kpfaulkner.com";
        private static string accessToken;
        private static string refreshToken;
        const string SkyDriveDetection = "sky://";

        // determines if this is the first time (and need access and refresh token)
        // or determines if we're just refreshing a refresh token.
        public static string GetAccessToken()
        {
            if (ConfigHelper.SkyDriveRefreshToken == null || ConfigHelper.SkyDriveRefreshToken == "")
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

        public static void GetLiveAccessAndRefreshTokens( string code )
        {
            var urlTemplate = @"https://oauth.live.com/token?client_id={0}&redirect_uri={1}&code={2}&grant_type=authorization_code";
            var url = string.Format(urlTemplate, skyDriveClientId, skyDriveRedirectUri,code);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create( url );
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();

            byte[] arr = new byte[3000];
            stream.Read(arr, 0, 3000);
            var responseString = System.Text.Encoding.Default.GetString(arr);

            ParseAccessRefreshResponse( responseString);
            SaveRefreshTokenToAppConfig();
            ConfigHelper.SkyDriveAccessToken = accessToken;
            ConfigHelper.SkyDriveRefreshToken = refreshToken;
        }

        public static void RefreshAccessToken()
        {
            var urlTemplate = @"https://oauth.live.com/token?client_id={0}&redirect_uri=https://oauth.live.com/desktop&grant_type=refresh_token&refresh_token={1}";
            var url = string.Format(urlTemplate, skyDriveClientId, ConfigHelper.SkyDriveRefreshToken);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "GET";
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
            dirPath = dirPath.Replace("sky://", "");
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
                if (foundEntry != null && foundEntry.Type == "file")
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
            return url.Contains(SkyDriveDetection);
        }

   
    }
}
