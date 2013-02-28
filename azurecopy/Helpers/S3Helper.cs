using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using azurecopy.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace azurecopy.Utils
{
    public static class S3Helper
    {

        static  string AmazonDetection = "amazon";

        public static string GetBucketFromUrl(string url)
        {
            var u = new Uri( url );
            var bucket = u.DnsSafeHost.Split('.')[0];

            return bucket;
        }


        public static string GetKeyFromUrl(string url)
        {
            var u = new Uri(url);

            var blobName = u.PathAndQuery.Substring(1);

            return blobName;
        }

        public static bool MatchHandler(string url)
        {
            return url.Contains(AmazonDetection);
        }


    }
}
