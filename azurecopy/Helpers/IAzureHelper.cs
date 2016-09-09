using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy.Helpers
{
    interface IAzureHelper
    {
        CloudBlobClient GetSourceCloudBlobClient(string url);
        CloudBlobClient GetTargetCloudBlobClient(string url);
        CloudStorageAccount GetCloudStorageAccount(string url, string accountKey, string accountName);
        CloudStorageAccount GetCloudStorageAccount(string accountKey, string accountName);
        CloudBlobClient GetCloudBlobClient(string accountName, string accountKey);
        CloudBlobClient GetCloudBlobClient(string url, bool isSrc, string accountKey = null);
        CloudFileClient GetCloudFileClient(string url, bool isSrc);
        bool IsDevUrl(string url);
        IEnumerable<IListBlobItem> ListBlobsInContainer(string containerUrl);
        List<string> ListBlobsInContainer(string containerUrl, CopyStatus copyStatusFilter);
        string GetBlobFromUrl(string blobUrl);
        string GetAccountNameFromUrl(string blobUrl);
        bool MatchHandler(string url);
        bool MatchFileHandler(string url);
        BasicBlobContainer AzureContainerToBasicBlobContainer(CloudBlobContainer container);
        string GetDisplayName(string fullBlobName);
        string GetVirtualDirectoryFromUrl(string blobUrl);
        string GetContainerFromUrl(string blobUrl, bool assumeNoBlob = false);
    }
}
