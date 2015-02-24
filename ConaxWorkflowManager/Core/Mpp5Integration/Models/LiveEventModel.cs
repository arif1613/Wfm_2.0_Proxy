using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NodaTime;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models
{
    public class LiveEventModel
    {
        //public LiveEventModel()
        //{
        //    HandledMessages = new List<Guid>();
        //    FieldChanges = new Dictionary<string, Instant>();
        //}
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, Dictionary<CultureInfo, string>> MetaData { get; set; }
        public Guid WamsAccountId { get; set; }
        public bool HiveAccelerated { get; set; }
        public bool AesEncryptionEnabled { get; set; }
        public int StreamingProtocol { get; set; } // Change the StreamingProtocol type to int while testing.
        public bool ActiveDirectoryRestricted { get; set; }
        public IList<IpRangeInfo> InputIpRangeList { get; set; }
        public IList<IpRangeInfo> PreviewIpRangeList { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? ProvisioningTime { get; set; }
        public string CreatedUser { get; set; }
    }
    public class IpRangeInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int? SubnetPrefixLength { get; set; }
    }
}
