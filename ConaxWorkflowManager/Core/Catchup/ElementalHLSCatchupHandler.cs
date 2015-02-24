using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.CatchUp;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public class ElementalHLSCatchupHandler : PlayListArchiveHLSCatchupHandler
    {
        public ElementalHLSCatchupHandler() {
            this.systemName = "ElementalEncoder";
            this.catchUpFileHandler = new ElementalFileHandler();
        }

        public override void DeleteNPVR(ContentData content, Asset assetToDelete)
        {
            throw new NotImplementedException();
        }

        protected override String GetBitrateFileName(String orginalStr)
        {

            Int32 pos = orginalStr.LastIndexOf('_');
            String newName = orginalStr;
            if (pos > 0)
                newName = orginalStr.Substring(0, pos) + ".m3u8";

            return newName;
        }

        public override String GetAssetUrl(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, NPVRRecording recording, EPGChannel epgChannel)
        {

            DateTime dtFrom = recording.Start.Value;
            DateTime dtTo = recording.End.Value;
            TimeSpan vbegin = UnifiedHelper.GetServerTimeStamp(dtFrom); //start använd handler
            TimeSpan vend = UnifiedHelper.GetServerTimeStamp(dtTo);

            var source = epgChannel.ServiceEpgConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType);
            String NPVRWebRoot = source.NpvrWebRoot;
            if (!NPVRWebRoot.EndsWith("/"))
                NPVRWebRoot += "/";

            String url = NPVRWebRoot + content.ID.Value + "/" + content.ExternalID + ".ism/Manifest?";
            url += "vbegin=" + ((UInt64)vbegin.TotalSeconds).ToString() + "&";
            url += "vend=" + ((UInt64)vend.TotalSeconds).ToString(); // end
            return url;
        }
    }
}
