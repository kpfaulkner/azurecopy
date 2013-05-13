using azurecopy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestUtils
{
    // basic test program to iron out issues.
    // These aren't unit tests, are more integration tests but really just working samples.
    class Program
    {
        static void CopyFromAzureToS3()
        {
        
            var azureUrl = ConfigHelper.AzureBaseUrl;
            var S3Url = ConfigHelper.S3BaseUrl;

            var sourceHandler = new AzureHandler(azureUrl);
            var targetHandler = new S3Handler(S3Url);

            var blob = sourceHandler.ReadBlobSimple("temp", "test.png");
            targetHandler.WriteBlobSimple("", blob);

        }

        static void Main(string[] args)
        {

            CopyFromAzureToS3();
        }
    }
}
