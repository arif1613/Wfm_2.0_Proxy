using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Xml.Linq;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.EPG
{
    public interface IEPGParser
    {
        Dictionary<UInt64, List<EpgContentInfo>> ParseEPGXML(XElement EPGXML, XElement EPGChannelConfigXML, XElement CatchUpFilterConfigXML, TimeZoneInfo feedtimeZone, List<EpgContentInfo> existingEpgs, List<EPGChannel> channels);

        void SetExecuteTime(DateTime executeTime);
    }
}
