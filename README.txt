Azure Copy
----------

Allows easy copying between S3, Azure and local filesystem. 
Only a few days old, so still in early development.

UPDATE:

Skydrive now has basic support (read/write blobs).
To use Skydrive the prefix "sky://" needs to be appended to the URL. For example, if copying a file c:\temp\test.txt to a folder "temp" in my Skydrive account, then the
command is:

	azurecopy -i c:\temp\test.txt -o sky://temp

See end of file for details regarding setting up of Skydrive configuration.

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

	This version monitors the blob copy and waits until there are no pending copies left.
	azurecopy.exe -m -blobcopy -i "https://testurl.s3-us-west-2.amazonaws.com/myfile.txt" -o "https://azuretest.blob.core.windows.net/test/"



You can list blobs in a container/bucket.

	eg.
	azurecopy.exe -list "https://testurl.s3.amazonaws.com"


You *can* have the Azure Account Key, S3 Access Key and S3 Access Key secret in the azurecopy.exe.config file. But if preferred these can be passed in on the command line:

	eg.
	
	azurecopy.exe -i "https://testurl.s3-us-west-2.amazonaws.com/myfile.txt" -o "https://azuretest.blob.core.windows.net/test/" -azurekey "myazurekey" -s3accesskey "mys3accesskey" -s3secretkey "mys3secretkey"

To copy to Skydrive issue the command similar to:

	azurecopy -i c:\temp\test.txt -o sky://temp

Complete list of command line arguments:

	-v : verbose
	-i : input url
	-o : output url
	-d : download to filesystem before uploading to output url. (use for big blobs)
	-blobcopy : use blobcopy API for when Azure is output url.
	-list : list blobs in bucket/container. Use in conjunction with -i
	-m : Monitor progress of copy when in "blobcopy" mode (ie -blobcopy flag was used). Program will not exit until all pending copies are complete.
	-destblobtype page|block : Destination blob type. Used when destination url is Azure and input url was NOT azure. eg S3 to Azure. 
	-ak | -azurekey : Azure account key.
	-s3k | -s3accesskey : S3 access key.
	-s3sk | -s3secretkey : S3 access key secret.
	-sak | -srcazurekey : input url Azure account key.
	-ss3k | -srcs3accesskey : input url S3 access key.
	-ss3sk | -srcs3secretkey : input url S3 access key secret.
	-tak | -targetazurekey : output url Azure account key.
	-ts3k | -targets3accesskey : output url S3 access key.
	-ts3sk | -targets3secretkey : output url S3 access key secret.

Please see TODO.txt for planned changes/enhancements.



SkyDrive Configuration:

Due to the OAuth authentication/authorisation for Skydrive the setup process is currently a little cumbersome. Hopefully this will change soon.
Currently the steps required to setup Skydrive is as follows:

	1) In your favourite browser, load the URL:  https://login.live.com/oauth20_authorize.srf?client_id=00000000480EE365&scope=wl.offline_access,wl.skydrive,wl.skydrive_update&response_type=code&redirect_uri=http://kpfaulkner.com/azurecopyoauth/
	2) Skydrive will ask you to login, allowing Azurecopy to access your Skydrive details. Please login.
	3) You'll get redirected to a URL similar to: http://kpfaulkner.com/azurecopyoauth/?code=a06e4364-bd1d-9f10-6b24-0d576c37a8a7
	4) Copy/paste the code (after the = character) into an editor.
	5) Open the azurecopy.exe.config file which came with the azurecopy zip file.
	6) Modify the "SkyDriveCode" line in the config file, entering the code copied in step 4.

		eg. The line should look like:

		<add key="SkyDriveCode" value="a06e4364-bd1d-9f10-6b24-0d576c37a8a7"/>

	7) Save the azurecopy.exe.config  
	8) Profit $$$