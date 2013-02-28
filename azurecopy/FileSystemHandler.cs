using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    class FileSystemHandler : IBlobHandler
    {

        public Blob ReadBlob(string url, string filePath = "")
        {
            throw new NotImplementedException();
        }


        public void WriteBlob(string url, Blob blob)
        {

        }

        public List<string> ListBlobsInContainer(string baseUrl)
        {
            throw new NotImplementedException();
        }

        public bool MatchHandler(string url)
        {
            return false;
        }

    }
}
