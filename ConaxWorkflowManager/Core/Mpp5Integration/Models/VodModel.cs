using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NodaTime;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models
{

    public class VodModel : IViewDocument
    {
        public string Name { get; set; }
        public Guid HolderId { get; set; }
        public Dictionary<string, Dictionary<CultureInfo, string>> MetaData { get; set; }
        public Uri ImagePath { get; set; }
        public Dictionary<Guid, String> UploadAssets { get; set; }
        public List<asset> Assets { get; set; }
        public string ThumbnailUrl { get; set; }
        public Guid Id { get; set; }
        public bool Deleted { get; set; }
        public IList<Guid> HandledMessages { get; set; }
        public Instant CreatedDate { get; set; }
        public string Status { get; set; }
        public IDictionary<string, Instant> FieldChanges { get; set; }
        public Instant LastChangeTime { get; set; }
        public bool ActiveDirectoryRestricted { get; set; }

        public VodModel()
        {
            HandledMessages = new List<Guid>();
            FieldChanges = new Dictionary<string, Instant>();
            MetaData = new Dictionary<string, Dictionary<CultureInfo, string>>();
            UploadAssets = new Dictionary<Guid, String>();
            Assets = new List<asset>();
        }
    }


    public class asset
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public List<string> Urls { get; set; }
        public EncodeModel EncodingStatus { get; set; }
        public bool AesEncryptionEnabled { get; set; }

        public asset()
        {
            Urls = new List<string>();
        }
    }
}

