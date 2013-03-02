Azure Copy
----------

Allows easy copying between S3, Azure and local filesystem. 
Only a few days old, so still in early development.

Usage:

azurecopy.exe -i inputurl -o outputurl

	eg.

	azurecopy.exe -i "https://testurl.s3-us-west-2.amazonaws.com/myfile.txt" -o "https://azuretest.blob.core.windows.net/test/"

	This will copy the S3 blob "myfile.txt" into Azure Blob Storage into the "test" container.

You can also simply list directories and not files.

	eg.
	azurecopy.exe -i "https://testurl.s3-us-west-2.amazonaws.com/" -o "https://azuretest.blob.core.windows.net/test/"

	This will copy all S3 blobs that are in the "testurl" bucket into Azure Blob Storage and put them into the "test" container.


If the target location is Azure Blob Storage, then we can get Azure to perform the copy for us (so we dont have to transfer between S3 and where azurecopy is running). 
Simply add "-blobcopy" into the command.

	eg.
	azurecopy.exe -blobcopy -i "https://testurl.s3-us-west-2.amazonaws.com/myfile.txt" -o "https://azuretest.blob.core.windows.net/test/"

	This is still in early development.


You can list blobs in a container/bucket.

	eg.
	azurecopy.exe -list "https://testurl.s3.amazonaws.com"


You *can* have the Azure Account Key, S3 Access Key and S3 Access Key secret in the azurecopy.exe.config file. But if preferred these can be passed in on the command line:

	eg.
	
	azurecopy.exe -i "https://testurl.s3-us-west-2.amazonaws.com/myfile.txt" -o "https://azuretest.blob.core.windows.net/test/" -azurekey "myazurekey" -s3accesskey "mys3accesskey" -s3secretkey "mys3secretkey"


Please see TODO.txt for planned changes/enhancements.

