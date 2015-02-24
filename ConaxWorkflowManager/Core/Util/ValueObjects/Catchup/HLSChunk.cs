using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup
{
    public class HLSChunk
    {
        public Int32 EXTXVERSION {get;set;}
        public Int32 EXTXTARGETDURATION {get; set;}
        public Int32 EXTXMEDIASEQUENCE {get; set;}
        public String EXTXKEY {get; set;}
        public String EXTINF {get; set;}
        public String URI {get; set;}
        public DateTime UTCStarttime { get; set; }
        public DateTime UTCEndtime { get; set; }
        //public String CubiChannelId { get; set; }
        public UInt64 ChannelId { get; set; }
        public String PlayListName { get; set; }
    }
}
