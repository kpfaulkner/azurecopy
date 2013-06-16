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
        static void Test()
        {
            var azureUrl = ConfigHelper.AzureBaseUrl;
        
            var sourceHandler = new AzureHandler(azureUrl);

            sourceHandler.ListBlobsInContainerSimple("temp/test/");
        }

        static void Test2()
        {
            var s3Url = ConfigHelper.S3BaseUrl;

            var sourceHandler = new S3Handler(s3Url);

            sourceHandler.ListBlobsInContainerSimple("testken123/test/");
        }

        static void Test3()
        {

            var bb = ConfigHelper.DropBoxAPIKey;
            var sourceHandler = new DropboxHandler();

            var a = sourceHandler.ListBlobsInContainerSimple("");

        }




        static void Main(string[] args)
        {

            Test3();
        }
    }
}
