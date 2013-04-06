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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace azurecopy
{

    public enum Action { None, NormalCopy, BlobCopy, List }
    
    class Program
    {
        const string UsageString = 
           @"Usage: azurecopy
                -v : verbose
	            -i : input url
	            -o : output url
	            -d : download to filesystem before uploading to output url. (use for big blobs)
	            -blobcopy : use blobcopy API for when Azure is output url.
	            -list : list blobs in bucket/container. Use in conjunction with -i
                -pu : parallel upload
                -cs : chunk size used for parallel upload (in MB).
	            -m : Monitor progress of copy when in 'blobcopy' mode (ie -blobcopy flag was used). Program will not exit until all pending copies are complete.
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
                -rd : Retry delay in seconds used when communicating with cloud storage environments.
                -mr : Maximum number of retries for a given operation.
                Note: Remember when local file system is destination/output do NOT end the directory with a \";

            
        const string LocalDetection = "???";
        const string VerboseFlag = "-v";
        const string InputUrlFlag = "-i";
        const string OutputUrlFlag = "-o";
        const string DownloadFlag = "-d";
        const string BlobCopyFlag = "-blobcopy";
        const string ListContainerFlag = "-list";
        const string MonitorBlobCopyFlag = "-m";
        const string ParallelUploadFlag = "-pu";
        const string ChunkSizeFlag = "-cs";
        const string RetryAttemptDelayInSecondsFlag = "-rd";
        const string MaxRetryAttemptsFlag = "-mr";
       
        // only makes sense for azure destination.
        const string DestBlobType = "-destblobtype";

        // default access keys.
        const string AzureAccountKeyShortFlag = "-ak";
        const string AWSAccessKeyIDShortFlag = "-s3k";
        const string AWSSecretAccessKeyIDShortFlag = "-s3sk";

        const string AzureAccountKeyFlag = "-azurekey";
        const string AWSAccessKeyIDFlag = "-s3accesskey";
        const string AWSSecretAccessKeyIDFlag = "-s3secretkey";

        // source access keys
        const string SourceAzureAccountKeyShortFlag = "-sak";
        const string SourceAWSAccessKeyIDShortFlag = "-ss3k";
        const string SourceAWSSecretAccessKeyIDShortFlag = "-ss3sk";

        const string SourceAzureAccountKeyFlag = "-srcazurekey";
        const string SourceAWSAccessKeyIDFlag = "-srcs3accesskey";
        const string SourceAWSSecretAccessKeyIDFlag = "-srcs3secretkey";

        // target access keys
        const string TargetAzureAccountKeyShortFlag = "-tak";
        const string TargetAWSAccessKeyIDShortFlag = "-ts3k";
        const string TargetAWSSecretAccessKeyIDShortFlag = "-ts3sk";

        const string TargetAzureAccountKeyFlag = "-targetazurekey";
        const string TargetAWSAccessKeyIDFlag = "-targets3accesskey";
        const string TargetAWSSecretAccessKeyIDFlag = "-targets3secretkey";


        static UrlType _inputUrlType;
        static UrlType _outputUrlType;

        static string _inputUrl;
        static string _outputUrl;   
        static Action _action = Action.None;
        static bool _listContainer = false;

        // destination blob...  can only assign if source is NOT azure and destination IS azure.
        static DestinationBlobType _destinationBlobType = DestinationBlobType.Unknown;

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
            else if (S3Helper.MatchHandler(url))
            {
                urlType = UrlType.S3;
            }
            else
            {
                urlType = UrlType.Local;  // local filesystem.
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
                            ConfigHelper.Verbose = true;
                            break;

                        case ParallelUploadFlag:
                            i++;
                            ConfigHelper.ParallelFactor = Convert.ToInt32(GetArgument(args, i));
                            
                            break;

                        case RetryAttemptDelayInSecondsFlag:
                            i++;
                            ConfigHelper.RetryAttemptDelayInSeconds = Convert.ToInt32(GetArgument(args, i));
                            break;

                        case MaxRetryAttemptsFlag:
                            i++;
                            ConfigHelper.MaxRetryAttempts = Convert.ToInt32(GetArgument(args, i));
                            break;

                        case ChunkSizeFlag:
                            i++;
                            ConfigHelper.ChunkSizeInMB = Convert.ToInt32(GetArgument(args, i));

                            break;


                        case DestBlobType:
                            i++;
                            var destType = GetArgument(args, i);
                            if (destType == "page")
                            {
                              ConfigHelper.DestinationBlobTypeSelected = DestinationBlobType.Page;
                            }
                            else if (destType == "block")
                            {
                                ConfigHelper.DestinationBlobTypeSelected = DestinationBlobType.Block;
                            }

                            break;

                        case MonitorBlobCopyFlag:
                            ConfigHelper.MonitorBlobCopy = true;
                            break;

                        case BlobCopyFlag:
                            ConfigHelper.UseBlobCopy = true;
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
                        case AzureAccountKeyShortFlag:
                            i++;
                            var azureKey = GetArgument(args, i);
                            ConfigHelper.AzureAccountKey = azureKey;
                            ConfigHelper.SrcAzureAccountKey = azureKey;
                            ConfigHelper.TargetAzureAccountKey = azureKey;
                            break;

                        case AWSAccessKeyIDFlag:
                        case AWSAccessKeyIDShortFlag:

                            i++;
                            var s3AccessKey = GetArgument(args, i);
                            ConfigHelper.AWSAccessKeyID = s3AccessKey;
                            ConfigHelper.SrcAWSAccessKeyID = s3AccessKey;
                            ConfigHelper.TargetAWSAccessKeyID = s3AccessKey;
                            break;

                        case AWSSecretAccessKeyIDFlag:
                        case AWSSecretAccessKeyIDShortFlag:
                            i++;
                            var s3SecretKey = GetArgument(args, i);
                            ConfigHelper.AWSSecretAccessKeyID = s3SecretKey;
                            ConfigHelper.SrcAWSSecretAccessKeyID = s3SecretKey;
                            ConfigHelper.TargetAWSSecretAccessKeyID = s3SecretKey;

                            break;

                        case SourceAzureAccountKeyFlag:
                        case SourceAzureAccountKeyShortFlag:
                            i++;
                            var srcAzureKey = GetArgument(args, i);
                            ConfigHelper.SrcAzureAccountKey = srcAzureKey;
                            break;

                        case SourceAWSAccessKeyIDFlag:
                        case SourceAWSAccessKeyIDShortFlag:

                            i++;
                            var srcS3AccessKey = GetArgument(args, i);
                            ConfigHelper.SrcAWSAccessKeyID = srcS3AccessKey;

                            break;

                        case SourceAWSSecretAccessKeyIDFlag:
                        case SourceAWSSecretAccessKeyIDShortFlag:
                            i++;
                            var srcS3SecretKey = GetArgument(args, i);
                            ConfigHelper.SrcAWSSecretAccessKeyID = srcS3SecretKey;

                            break;

                        case TargetAzureAccountKeyFlag:
                        case TargetAzureAccountKeyShortFlag:
                            i++;
                            var targetAzureKey = GetArgument(args, i);
                            ConfigHelper.TargetAzureAccountKey = targetAzureKey;
                            break;

                        case TargetAWSAccessKeyIDFlag:
                        case TargetAWSAccessKeyIDShortFlag:

                            i++;
                            var targetS3AccessKey = GetArgument(args, i);
                            ConfigHelper.TargetAWSAccessKeyID = targetS3AccessKey;

                            break;

                        case TargetAWSSecretAccessKeyIDFlag:
                        case TargetAWSSecretAccessKeyIDShortFlag:
                            i++;
                            var targetS3SecretKey = GetArgument(args, i);
                            ConfigHelper.TargetAWSSecretAccessKeyID = targetS3SecretKey;

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
                            ConfigHelper.DownloadDirectory = GetArgument(args, i);
                            ConfigHelper.AmDownloading  = true;
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

                case UrlType.Local:
                    blobHandler = new FileSystemHandler();
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

                // currently sequential.
                // TODO: make concurrent.
                foreach (var url in sourceBlobList)
                {
                    var fileName = "";
                    if ( ConfigHelper.AmDownloading)
                    {
                        fileName = GenerateFileName(ConfigHelper.DownloadDirectory, url);
                    }

                    var outputUrl = GenerateOutputUrl(_outputUrl, url);

                    if (!ConfigHelper.UseBlobCopy)
                    {

                        // read blob
                        var blob = inputHandler.ReadBlob(url, fileName);

                        // if blob is marked with type "Unknown" then set it to what was passed in on command line.
                        // if nothing was passed in, then default to block?
                        if (blob.BlobType == DestinationBlobType.Unknown)
                        {
                            if (_destinationBlobType != DestinationBlobType.Unknown)
                            {
                                blob.BlobType = _destinationBlobType;
                            }
                            else
                            {
                                blob.BlobType = DestinationBlobType.Block;
                            }
                        }

                        // write blob
                        outputHandler.WriteBlob(outputUrl, blob, ConfigHelper.ParallelFactor, ConfigHelper.ChunkSizeInMB);
                    }
                    else
                    {
                        Console.WriteLine("using blob copy {0} to {1} of type {2}", url, outputUrl, _destinationBlobType);
                        AzureBlobCopyHandler.StartCopy(url, outputUrl, _destinationBlobType);
                    }
                }

                // if blob copy and monitoring
                if (ConfigHelper.UseBlobCopy && ConfigHelper.MonitorBlobCopy)
                {
                    AzureBlobCopyHandler.MonitorBlobCopy(_outputUrl);
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

            AzureBlobCopyHandler.StartCopy(_inputUrl, _outputUrl, _destinationBlobType);

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
              
                case Action.NormalCopy:
                case Action.BlobCopy:
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
            var sh = new SkyDriveHandler();
            sh.ListBlobsInContainer("");


            ParseArguments(args);

            var sw = new Stopwatch();
            sw.Start();
            Process();
            sw.Stop();
            Console.WriteLine("Operation took {0} ms", sw.ElapsedMilliseconds);
        }

    }
}
