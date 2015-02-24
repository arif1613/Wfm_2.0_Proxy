using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup
{
    public class AvailableDateTime
    {
        public AvailableDateTime() { }

        public AvailableDateTime(UInt64 channelId, DateTime utcMaxEndtime) {
            this.ChannelId = channelId;
            this.UTCMaxEndtime = utcMaxEndtime;
        }

        public UInt64 ChannelId { get; set; }
        public DateTime UTCMaxEndtime { get; set; }
    }
}
