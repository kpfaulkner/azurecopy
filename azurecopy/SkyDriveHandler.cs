using azurecopy.Helpers;
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

        public SkyDriveHandler()
        {
            accessToken = SkyDriveHelper.GetAccessToken();

        }

        public Blob ReadBlob(string url, string filePath = "")
        {
            throw new NotImplementedException();
        }

        public void WriteBlob(string url, Blob blob,  int parallelUploadFactor=1, int chunkSizeInMB=4)
        {
            Stream stream = null;

            // get stream to data.
            if (blob.BlobSavedToFile)
            {
                stream = new FileStream(blob.FilePath, FileMode.Open);
            }
            else
            {
                stream = new MemoryStream(blob.Data);
            }

        
            var requestUriFile =  new StringBuilder("https://apis.live.net/v5.0/folder.6bc852c8e2ff5fed/files/upload2.txt");
            requestUriFile.AppendFormat("?access_token={0}", accessToken);

            byte[] arr = System.IO.File.ReadAllBytes("C:\\temp\\upload.txt");
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUriFile.ToString());
            request.Method = "PUT";
            //request.ContentType = "text/plain";
            request.ContentLength = arr.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(arr, 0, arr.Length);
            dataStream.Close();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string returnString = response.StatusCode.ToString();

        }


        public List<string> ListBlobsInContainer(string baseUrl)
        {

            //throw new NotImplementedException();

            //var requestUriFile = new StringBuilder("https://apis.live.net/v5.0/me/skydrive");
            var requestUriFile = new StringBuilder("https://apis.live.net/v5.0/me/folders");
            requestUriFile.AppendFormat("?access_token={0}", ConfigHelper.SkyDriveAccessToken);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUriFile.ToString());
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();

            byte[] arr = new byte[2000];
            stream.Read(arr, 0, 2000);
            var mystring = System.Text.Encoding.Default.GetString(arr);

            string returnString = response.StatusCode.ToString();

            return null;
        }


    }
}
