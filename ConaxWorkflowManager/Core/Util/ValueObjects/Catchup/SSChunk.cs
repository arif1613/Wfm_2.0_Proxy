using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup
{
    public class SSChunk
    {
        //public String Cubichannelid { get; set; }
        //public String ManifestFileName { get; set; }
        public DateTime UTCStartTime { get; set; }
        public DateTime UTCEndTime { get; set; }
        //public String StreamIndexType { get; set; }
        public Int64 T { get; set; }
        public Int32 D { get; set; }
    }
}
