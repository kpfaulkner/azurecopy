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
    public static class CommonHelper
    {

        public static Stream GetStream(string fileName)
        {
            Stream stream = null;

            // get stream to data.
            if (!string.IsNullOrEmpty(fileName))
            {
                stream = new FileStream(fileName, FileMode.Create);
            }
            else
            {
                stream = new MemoryStream();
            }

            return stream;
        }

        public static bool IsABlob(string url)
        {
            return !url.EndsWith("/");

        }


    }
}
