using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace azurecopy.Datatypes
{

    [DataContract]
    public class AccessType
    {
        [DataMember(Name = "access")]
        public string Access { get; set; }

    }

    [DataContract]
    public class Person
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

    }

    [DataContract]
    public class SkyDriveDirectory
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

        [DataMember(Name = "count")]
        public int Count { get; set; }

        [DataMember(Name = "link")]
        public string Link { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "shared_with")]
        public AccessType SharedWith { get; set; }

        [DataMember(Name = "created_time")]
        public string CreateTime { get; set; }

        [DataMember(Name = "updated_time")]
        public string UpdatedTime { get; set; }

    }

    [DataContract]
    class Wrapper
    {
        [DataMember]
        public List<SkyDriveDirectory> data { get; set; }
    }

}
