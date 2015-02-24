using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup
{
    public class SSManifest
    {
        //public String CubiChannelId { get; set; }
        public UInt64 ChannelId { get; set; }
        public String ManifestFileName { get; set; }
        public String ManifestData { get; set; }
        public DateTime UTCManifestStartTime { get; set; }
        public DateTime UTCStreamStartTime { get; set; }
        public DateTime UTCStreamEndTime { get; set; }

    }
}
