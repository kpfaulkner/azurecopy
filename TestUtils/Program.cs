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
        static void AzureListBlobsInContainerSimpleTest()
        {
            var azureUrl = ConfigHelper.AzureBaseUrl;
        
            var sourceHandler = new AzureHandler(azureUrl);

            var res = sourceHandler.ListBlobsInContainerSimple("temp");
            foreach(var i in res)
            {
                Console.WriteLine(i.Url);

            }
        }

        static void AzureCopyBlobSimpleTest()
        {
            var azureUrl = ConfigHelper.AzureBaseUrl;

            var sourceHandler = new AzureHandler(azureUrl);

            var blob = sourceHandler.ReadBlobSimple("temp", "Test.cmd");

            blob.Name = "Test2.cmd";

            sourceHandler.WriteBlobSimple("temp", blob);
        }


        static void S3ListBlobsInContainerSimpleTest()
        {
            var s3Url = ConfigHelper.S3BaseUrl;

            var sourceHandler = new S3Handler(s3Url);

            var res = sourceHandler.ListBlobsInContainerSimple("");
            foreach (var i in res)
            {
                Console.WriteLine(i.Url);

            }
        }

        static void S3CopyBlobSimpleTest()
        {
            var s3Url = ConfigHelper.S3BaseUrl;

            var sourceHandler = new S3Handler(s3Url);

            var blob = sourceHandler.ReadBlobSimple("", "Test.cmd");

            blob.Name = "Test2.cmd";

            sourceHandler.WriteBlobSimple("test", blob);
        }


      

        static void Test3()
        {

            var bb = ConfigHelper.DropBoxAPIKey;
            var sourceHandler = new DropboxHandler();

            var a = sourceHandler.ListBlobsInContainerSimple("");

        }

        static void Test4()
        {

            var sourceHandler = new SharepointHandler("https://xx.sharepoint.com/");

            var a = sourceHandler.ReadBlobSimple("https://xx.sharepoint.com/Shared Documents/ken.txt", "foo");


        }

        static void Test5()
        {
            var azureHandler = new AzureHandler();
            var dropboxHandler = new DropboxHandler();

            var inputUrl = "https://xxx.blob.core.windows.net/temp/test2";
            var outputUrl = "https://dropbox.com/stuff/";

            var blob = azureHandler.ReadBlob(inputUrl);
            dropboxHandler.WriteBlob(outputUrl, blob);
            Console.WriteLine("done");
            Console.ReadKey();
        }


        static void Main(string[] args)
        {
            
            //AzureListBlobsInContainerSimpleTest();

            //AzureCopyBlobSimpleTest();

            S3ListBlobsInContainerSimpleTest();

            S3CopyBlobSimpleTest();
            //Test3();
            //Test4();
            //sTest5();

            Console.WriteLine("done");
            Console.ReadKey();
        }
    }
}
