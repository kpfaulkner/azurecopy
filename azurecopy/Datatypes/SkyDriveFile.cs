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
using System.Runtime.Serialization;
using System.Text;

namespace azurecopy.Datatypes
{

    [DataContract]
    public class SkyDriveFile
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "from")]
        public Person From { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "parent_id")]
        public string ParentId { get; set; }

        [DataMember(Name = "size")]
        public long Size { get; set; }

        [DataMember(Name = "upload_location")]
        public string UploadLocation { get; set; }

        [DataMember(Name = "comments_count")]
        public int CommentsCount { get; set; }

        [DataMember(Name = "comments_enabled")]
        public bool CommentsEnabled { get; set; }

        [DataMember(Name = "is_embeddable")]
        public bool IsEmbeddable { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "shared_with")]
        public AccessType SharedWith { get; set; }

        [DataMember(Name = "created_time")]
        public string CreateTime { get; set; }

        [DataMember(Name = "updated_time")]
        public string UpdatedTime { get; set; }


        [DataMember(Name = "source")]
        public string Source { get; set; }

    }

    [DataContract]
    class FileWrapper
    {
        [DataMember]
        public List<SkyDriveFile> data { get; set; }
    }

}
