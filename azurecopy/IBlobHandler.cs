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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azurecopy
{
    /// <summary>
    /// Interface for reading and writing blobs.
    /// In theory this could all be generic URL's, but given we want to try and perform some
    /// of these tasks in parallel I'm guessing that we'll need to use the API's in a smart way
    /// and not just serial reads/writes...
    /// </summary>
    public interface IBlobHandler
    {
        Blob ReadBlob(string url, string filePath = "");

        void WriteBlob(string url, Blob blob,  int parallelUploadFactor=1, int chunkSizeInMB=4);

        List<BasicBlobContainer> ListBlobsInContainer(string baseUrl);
    }
}
