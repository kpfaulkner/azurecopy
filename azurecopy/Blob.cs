using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    class Blob
    {

        // name.
        public string Name { get; set; }

        // original url.
        public string Url { get; set; }

        // indicator on where data is.
        public bool BlobSavedToFile { get; set; }

        // file on local filesystem (if required).
        public string FilePath { get; set; }

        // actual data..... if we're just storing in memory.
        public byte[] Data { get; set; }

        // metadata/properties. Blob information that is NOT part of the core blob itself.
        public Dictionary<string, string> MetaData { get; set; }

        // page or block blob (for Azure)
        public bool IsBlockBlob { get; set; }


        public Blob()
        {
            BlobSavedToFile = false;
            IsBlockBlob = true;
        }

    }
}
