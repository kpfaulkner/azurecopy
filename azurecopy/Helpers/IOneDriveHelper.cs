using azurecopy.Datatypes;
using System.Collections.Generic;

namespace azurecopy.Helpers
{
    public interface IOneDriveHelper
    {
        string GetAccessToken();

        void GetLiveAccessAndRefreshTokens(string code);
        void RefreshAccessToken();
        
        // complete path. eg,  /firstdir/seconddir/thirddir
        OneDriveDirectory CreateFolder(string folderPath);
        
        // folder and parent id
        void CreateFolder(string folderName, string parentId);
        string GetFolderId(string folderName);
        List<OneDriveDirectory> ListOneDriveRootDirectories();
        List<OneDriveDirectory> ListOneDriveDirectoryContent(string dirPath);
        OneDriveDirectory GetOneDriveDirectory(string fullPath);

        // fullPath is any number of directories then filename
        // eg. dir1/dir2/dir3/myfile.txt
        OneDriveFile GetOneDriveEntryByFileNameAndDirectory(string fullPath);
        List<OneDriveDirectory> ListOneDriveEntry(string initialUrl);
        string GenerateOneDriveURL(string initialUrl, bool isFile = false);
        List<OneDriveDirectory> ListOneDriveDirectoryWithUrl(string url);
        OneDriveFile ListOneDriveFileWithUrl(string url);
        ReturnType ListOneDriveWithUrl<ReturnType>(string url);
        bool MatchHandler(string url);

        string OneDrivePrefix();
    }
}