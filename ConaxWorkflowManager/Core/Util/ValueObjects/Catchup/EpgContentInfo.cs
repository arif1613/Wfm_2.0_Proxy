using System;
using System.Collections.Generic;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup
{
    public class EpgContentInfo
    {
        public ContentData Content { get; set; }

        public bool ExternalIDMatches(String externalID)
        {
            return Content.ExternalID.Equals(externalID);
        }

        public List<ulong> Services = new List<ulong>();

        public bool IsPublishedToAllServices { get; set; } // same thing as content have property CatchupContentProperties.CubiEpgId with a value for all services

        public bool ExistsInMpp { get; set; } // no longer in use..`.?

        public String MetaDataHash { get; set; }

        public UInt64 ChannelID { get; set; } // need this for optimaize,
    }
}
