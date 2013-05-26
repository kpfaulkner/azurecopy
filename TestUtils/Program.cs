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

            sourceHandler.ListBlobsInContainerSimple("test/dira");
        }

        static void Main(string[] args)
        {

            Test();
        }
    }
}
