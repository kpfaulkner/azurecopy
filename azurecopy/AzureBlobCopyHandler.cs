using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using azurecopy.Utils;

namespace azurecopy
{

    public class AzureBlobCopyHandler
    {

        // Copy from complete URL (assume URL is complete at this stage) to destination blob.
        public static void StartCopy(string sourceUrl, string DestinationUrl)
        {

            var client = AzureHelper.GetCloudBlobClient();

            var containerName = AzureHelper.GetContainerFromUrl( DestinationUrl);
            var blobName = AzureHelper.GetBlobFromUrl( DestinationUrl );

            var container = client.GetContainerReference( containerName );
            container.CreateIfNotExists();

            var blob = container.GetBlockBlobReference(blobName);

            //var blob = client.GetBlobReferenceFromServer(new Uri(DestinationUrl));

            // starts the copying process....
            var res = blob.StartCopyFromBlob( new Uri( sourceUrl ));
            var a = res;
        }

        // Monitor progress of copy.
        public static void MonitorCopy( string DestinationUrl )
        {

        }



    }
}
