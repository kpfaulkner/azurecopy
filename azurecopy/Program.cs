﻿//-----------------------------------------------------------------------
// <copyright >
//    Copyright 2013 Ken Faulkner
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

using azurecopy.Helpers;
using azurecopy.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    public enum UrlType { Azure, S3, Local };
    public enum Action { None, NormalCopy, BlobCopy, List }


    class Program
    {
        const string UsageString = "Usage: azurecopy -blobcopy -v -d <download directory> -i <inputUrl> -o <outputUrl> -list <inputUrl>\n    -list : Lists all blobs in given container/bucket\n    -blobcopy : Copy between input URL and output URL where output url HAS to be Azure\n    -v : verbose";
     
        const string LocalDetection = "???";
        const string VerboseFlag = "-v";
        const string InputUrlFlag = "-i";
        const string OutputUrlFlag = "-o";
        const string DownloadFlag = "-d";
        const string BlobCopyFlag = "-blobcopy";
        const string ListContainerFlag = "-list";
        const string AzureAccountKeyFlag = "-azurekey";
        const string AWSAccessKeyIDFlag = "-s3accesskey";
        const string AWSSecretAccessKeyIDFlag = "-s3secretkey";

        static UrlType _inputUrlType;
        static UrlType _outputUrlType;

        static string _inputUrl;
        static string _outputUrl;
        static string _downloadDirectory;
        static bool _verbose = false;
        static bool _amDownloading = false;
        static bool _useBlobCopy = false;
        static bool _listContainer = false;
        static Action _action = Action.None;
        static string _azureKey = String.Empty;
        static string _s3AccessKey = String.Empty;
        static string _s3SecretKey = String.Empty;

        static string GetArgument(string[] args, int i)
        {
            if (i < args.Length)
            {
                return args[i];
            }
            else
            {
                throw new ArgumentException("Invalid parameters...");
            }

        }

        static UrlType GetUrlType(string url)
        {
            UrlType urlType = UrlType.Local;


            if ( AzureHelper.MatchHandler(url))
            {
                urlType = UrlType.Azure;
            }
            else if ( S3Helper.MatchHandler(url))
            {
                urlType = UrlType.S3;
            }

            return urlType;
        }

        static void ParseArguments(string[] args)
        {
            var i = 0;

            if (args.Length > 0)
            {
                while (i < args.Length)
                {
                    switch (args[i])
                    {
                        case VerboseFlag:
                            _verbose = true;
                            break;

                        case BlobCopyFlag:
                            _useBlobCopy = true;
                            _action = Action.BlobCopy;
                            break;


                        case ListContainerFlag:
                            i++;
                            _inputUrl = GetArgument(args, i);
                            _inputUrlType = GetUrlType(_inputUrl);
                            _listContainer = true;
                            _action = Action.List;

                            break;

                        case AzureAccountKeyFlag:
                            i++;
                            _azureKey = GetArgument(args, i);
                            ConfigHelper.AzureAccountKey = _azureKey;
                            break;

                        case AWSAccessKeyIDFlag:
                            i++;
                            _s3AccessKey = GetArgument(args, i);
                            ConfigHelper.AWSAccessKeyID = _s3AccessKey;

                            break;

                        case AWSSecretAccessKeyIDFlag:
                            i++;
                            _s3SecretKey = GetArgument(args, i);
                            ConfigHelper.AWSSecretAccessKeyID = _s3SecretKey;

                            break;

                        case InputUrlFlag:
                            i++;
                            _inputUrl = GetArgument(args, i);
                            _inputUrlType = GetUrlType(_inputUrl);
                            if (_action == Action.None)
                            {
                                _action = Action.NormalCopy;
                            }
                            break;

                        case OutputUrlFlag:
                            i++;
                            _outputUrl = GetArgument(args, i);
                            _outputUrlType = GetUrlType(_outputUrl);
                            if (_action == Action.None)
                            {
                                _action = Action.NormalCopy;
                            }
                            break;

                        case DownloadFlag:
                            i++;
                            _downloadDirectory = GetArgument(args, i);
                            _amDownloading = true;
                            break;

                        default:
                            break;
                    }

                    i++;
                }
            }
            else
            {
                Console.WriteLine(UsageString);
            }

        }


        // default to local filesystem
        static IBlobHandler GetHandler(UrlType urlType)
        {
            IBlobHandler blobHandler;

            switch (urlType)
            {
                case UrlType.Azure:
                    blobHandler = new AzureHandler();
                    break;

                case UrlType.S3:
                    blobHandler = new S3Handler();
                    break;

                default:
                    blobHandler = new FileSystemHandler();
                    break;
            }

            return blobHandler;
        }



        static void DoNormalCopy()
        {

            IBlobHandler inputHandler = GetHandler(_inputUrlType);
            IBlobHandler outputHandler = GetHandler(_outputUrlType);


            if (inputHandler != null && outputHandler != null)
            {

                // handle multiple files.
                //currently sequentially.
                var sourceBlobList = GetSourceBlobList(inputHandler, _inputUrl);

                foreach (var url in sourceBlobList)
                {

                    var fileName = "";
                    if (_amDownloading)
                    {
                        fileName = GenerateFileName(_downloadDirectory, url);
                    }

                    // read blob
                    var blob = inputHandler.ReadBlob(url, fileName);

                    var outputUrl = GenerateOutputUrl(_outputUrl, url);

                    // write blob
                    outputHandler.WriteBlob(outputUrl, blob);
                }

            }
          
        }

        private static string GenerateOutputUrl(string baseOutputUrl, string inputUrl)
        {
            var u = new Uri(inputUrl);
            var blobName = "";
            blobName = u.Segments[u.Segments.Length - 1];

            var outputPath = Path.Combine(baseOutputUrl, blobName);

            return outputPath;

        }

        private static List<string> GetSourceBlobList(IBlobHandler inputHandler, string url)
        {
            var blobList = new List<string>();

            if (CommonHelper.IsABlob(url))
            {
                blobList.Add(url);
            }
            else
            {
                blobList = inputHandler.ListBlobsInContainer(url);
            }


            return blobList;
        }

        static void DoBlobCopy()
        {

            AzureBlobCopyHandler.StartCopy(_inputUrl, _outputUrl);

        }

        static void DoList()
        {
            IBlobHandler handler = GetHandler( _inputUrlType );
            var blobList = handler.ListBlobsInContainer(_inputUrl);

            foreach (var blob in blobList)
            {
                Console.WriteLine(blob);
            }

        }

        static void Process()
        {
            switch (_action)
            {
                case Action.List:
                    DoList();
                    break;
                
                case Action.BlobCopy:
                    DoBlobCopy();
                    break;

                case Action.NormalCopy:
                    DoNormalCopy();
                    break;

                default:
                    break;
            }

        }


        private static string GenerateFileName(string downloadDirectory, string url)
        {
            var u = new Uri(url);
            var blobName = "";
            blobName = u.Segments[u.Segments.Length - 1];
            var fullPath = Path.Combine( downloadDirectory, blobName);

            return fullPath;
        }


        static void Main(string[] args)
        {
            ParseArguments(args);
            Process();
        }

    }
}
