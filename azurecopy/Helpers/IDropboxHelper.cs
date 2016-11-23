using DropNet;
using System;

namespace azurecopy.Helpers
{
    public interface IDropboxHelper
    {
        DropNetClient GetClient();
        string BuildAuthorizeUrl();
        Tuple<string, string> GetAccessToken();
        bool MatchHandler(string url);
    }
}