
AppVeyor: [![AppVeyor](https://ci.appveyor.com/project/KenFaulkner/azurecopy)](https://ci.appveyor.com/project/KenFaulkner/azurecopy)

Azure Copy
----------

Allows easy copying between S3, Azure, Dropbox, Sharepoint Online, OneDrive and local filesystem. 

Configuration:

To access each cloud provider various keys/passwords will needed to be added to the azurecopy.exe.config (or app.config if you're coding your own). The AppSetting keys to use are:

Azure:
<add key="AzureAccountKey" value="" />

S3:
<add key="AWSAccessKeyID" value="" />
<add key="AWSSecretAccessKeyID" value="" />
<add key="AWSRegion" value="us-west-2" />

OneDrive: (yes yes, it still says skydrive in the config).
<add key="SkyDriveCode" value="" />
<add key="SkyDriveRefreshToken" value="" />

Dropbox:
<add key="DropBoxAPIKey" value="" />
<add key="DropBoxAPISecret" value="" />

Sharepoint Online:
<add key="SharepointUsername" value="" />
<add key="SharepointPassword" value="" />

For Azure,S3 and Sharepoint Online you simply copy the configuration values from the appropriate portal to the azurecopy.exe.config file directly. If you are unsure where to find these configuration values
please feel free to contact me at ken.faulkner@gmail.com and I can direct you.

For OneDrive and Dropbox the configuration is a little more invovled.

For OneDrive you need to issue the command:

azurecopy -configonedrive

and then follow the instructions. This basically involves copying a URL to a browser, accepting the OneDrive prompt saying you allow AzureCopy to access your OneDrive account and then copying part of the response 
URL back into the command prompt. The instructions will be clearly displayed in the command prompt.

For Dropbox you need to issue the command:

azurecopy -configdropbox 

Again, follow the instructions and the config file will be automatically modified for you.


Examples:


S3 to Azure using regular copy:   azurecopy.exe -i https://mybucket.s3.amazonaws.com/myblob -o https://myaccount.blob.core.windows.net/mycontainer
S3 to Azure using blob copy api (better for local bandwidth: azurecopy.exe -i https://mybucket.s3.amazonaws.com/myblob -o https://myaccount.blob.core.windows.net/mycontainer -blobcopy
Azure to S3: azurecopy.exe -i https://myaccount.blob.core.windows.net/mycontainer/myblob -o https://mybucket.s3.amazonaws.com/ 
List contents in S3 bucket: azurecopy.exe -list https://mybucket.s3.amazonaws.com/
List contents in Azure container: azurecopy.exe -list https://myaccount.blob.core.windows.net/mycontainer/ 
Onedrive to local using regular copy: azurecopy.exe -i sky://temp/myfile.txt -o c:\\temp\\");
Dropbox to local using regular copy: azurecopy.exe -i https://dropbox.com/temp/myfile.txt -o c:\\temp\\

More examples will be added directly to AzureCopy which you can see by running the command:

azurecopy -examples

