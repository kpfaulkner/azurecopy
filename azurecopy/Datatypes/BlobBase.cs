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
 
 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    public enum DestinationBlobType { Unknown, Block, Page };
    public enum UrlType { Azure, S3, SkyDrive, Local, Sharepoint, Dropbox, Other };

    public enum BlobEntryType
    {
        Blob,
        Container,
        Unknown
    };

    // basic blob version.
    // purely used for listing blobs.
    public class BasicBlobContainer
    {
        // Blob name.
        // This can be a virtual directory structure such as /dir1/dir2/blobname
        // although technically with both S3 and Azure it's really just the blob name.
        // Will supply helper classes to deal with traversing these virtual directories.
        public string Name { get; set; }

        // display name. This would be used when virtual directories are involved.
        // ie if the name is "dir1/dir2/myfile.txt" then the displayname will be "myfile.txt"
        public string DisplayName { get; set; }

        // container for blob. Can mean different things to different 
        // cloud providers. For S3 it would be bucket. For Azure it would be container. etc.
        // Once I get around to sharepoint it will probably be parent directory.
        public string Container { get; set; }

        // virtual directory parent of the blob.
        // Both S3 and Azure (and I'm sure other cloud storage providers) have the concept 
        // of virtual directories, which is basically just manipulation of the blob name to 
        // make it appear there are directories between the blob and the container.
        // ie a blob can have the name "dir1/dir2/myfile.txt". It *looks* like there are 
        // directories, but there really isn't.
        public string VirtualDirectoryParent { get; set; }

        // original url.
        public string Url { get; set; }

        public BlobEntryType BlobType { get; set; }

    }

    public class BlobBase
    {

        // name.
        public string Name { get; set; }

        // container for blob. Can mean different things to different 
        // cloud providers. For S3 it would be bucket. For Azure it would be container. etc.
        // Once I get around to sharepoint it will probably be parent directory.
        public string Container { get; set; }

        // original url.
        public string Url { get; set; }

        // metadata/properties. Blob information that is NOT part of the core blob itself.
        public Dictionary<string, string> MetaData { get; set; }

        public BlobBase()
        {
        
        }

    }
}
