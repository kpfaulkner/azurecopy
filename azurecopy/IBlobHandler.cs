using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    /// <summary>
    /// Interface for reading and writing blobs.
    /// In theory this could all be generic URL's, but given we want to try and perform some
    /// of these tasks in parallel I'm guessing that we'll need to use the API's in a smart way
    /// and not just serial reads/writes...
    /// </summary>
    interface IBlobHandler
    {
        Blob ReadBlob(string url, string filePath = "");

        void WriteBlob(string url, Blob blob);

        List<string> ListBlobsInContainer(string baseUrl);
    }
}
