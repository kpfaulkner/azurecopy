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
            config.AppSettings.Settings.Add("SkyDriveRefreshToken",  refreshToken);
            
            // Save the changes in App.config file.
            config.Save(ConfigurationSaveMode.Modified);


        }

        public static void CreateFolder(string folderName)
        {
            var newFolderData = "{\r\n  \"name\": \""+folderName+"\",\r\n  \"description\": \"Created by azurecopy\"\r\n}";

            var url = "https://apis.live.net/v5.0/me/skydrive?access_token=" + ConfigHelper.SkyDriveAccessToken;
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


    }
}
