using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.CatchUp;
using System.Xml.Linq;
using System.Xml.XPath;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Unified;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public class EnvivioHLSCatchupHandler : PlayListArchiveHLSCatchupHandler
    {
        public EnvivioHLSCatchupHandler() {
            this.systemName = "EnvivioEncoder";
            this.catchUpFileHandler = new EnvivioCatchUpFileHandler();
        }

        public override void DeleteNPVR(ContentData content, Asset assetToDelete)
        {
            throw new NotImplementedException();
        }

        public override String GetAssetUrl(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, NPVRRecording recording, EPGChannel epgChannel)
        {
            throw new NotImplementedException();
            //DateTime dtFrom = recording.Start.Value;
            //DateTime dtTo = recording.End.Value;
            //TimeSpan vbegin = UnifiedHelper.GetServerTimeStamp(dtFrom); //start använd handler
            //TimeSpan vend = UnifiedHelper.GetServerTimeStamp(dtTo);

            //String url = epgChannel.SSNPVRWebRoot + content.ID.Value + "/" + content.ExternalID + ".ism/Manifest?";
            //url += "vbegin=" + ((UInt64)vbegin.TotalSeconds).ToString() + "&";
            //url += "vend=" + ((UInt64)vend.TotalSeconds).ToString(); // end
            //return url;
        }
    }
}
