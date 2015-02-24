using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Unified
{
    public class UnifiedHelper
    {
        public static TimeSpan GetServerTimeStamp(DateTime dt)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String catchUpEncoderOffset = systemConfig.GetConfigParam("CatchUpEncoderOffset");
            DateTime encoderOffset = DateTime.ParseExact(catchUpEncoderOffset, "yyyy-MM-dd", null);

            return dt - encoderOffset;
        }

        public static TimeSpan GetNPVRAssetStartOffset(DateTime dt, Asset asset)
        {
            Property NPVRAssetStarttimeProperty = asset.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRAssetStarttime));
            if (!String.IsNullOrWhiteSpace(NPVRAssetStarttimeProperty.Value))
            {
                DateTime archivedStarttime = DateTime.ParseExact(NPVRAssetStarttimeProperty.Value, "yyyy-MM-dd HH:mm:ss", null);

                return (dt - archivedStarttime);
            }
            return new TimeSpan();
        }
    }
}
