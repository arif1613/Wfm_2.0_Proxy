using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Harmonic;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public class SeaWellHLSCatchupHandler : PlayListArchiveHLSCatchupHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SeaWellHLSCatchupHandler()
        {
            this.systemName = "SeaWell";
            this.catchUpFileHandler = null;
        }

        public override void DeleteNPVR(ContentData content, Asset assetToDelete)
        {
            IUnifiedServicesWrapper uw = UnifiedServicesWrapperManager.Instance;
            RecordResult res = uw.DeleteSmoothAsset(content, assetToDelete);
            if (res.ReturnCode >= 400 && res.ReturnCode <= 409)
            {
                log.Debug("Delete asset " + assetToDelete.Name + " for content " + content.Name + " " + content.ID.Value + " " + content.ExternalID + " has following status code " + res.ReturnCode + " " + res.Message);
            }
            else if (res.Exception != null)
            {
                throw new Exception(res.Message, res.Exception);
            }
        }

        public override void DeleteCatchupSegments(EPGChannel epgChannel) {
            // do nothing, Seawell converts smooth into HLS on the fly,
            // so tehre is no physical HLS representation to delete.
        }

        protected override String GetBitrateFileName(String orginalStr)
        {   // since we don't hannle physical HLS representaion, we don't need to index any segements.
            throw new NotImplementedException("This method is used for HLS segment indexing, and it's not implemented for Seawell setup.");
        }

        public override void GenerateManifest(List<String> channelsToProces)
        {
            //log.Debug("Start Generate HLS manifests.");
        }

        public override void GenerateNPVR(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime startTime, DateTime endTime)
        {
            // do nothing. converts on the fly by CDN
            var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType, AssetType.NPVR);
            // update to archived state in mpp.
            List<Property> NPVRAssetWithArchivedState = ConaxIntegrationHelper.SetNPVRAssetArchiveStateByAssetName(content, asset.Name,
                                                                   NPVRAssetArchiveState.Archived);
            mppWrapper.UpdateContentProperties(content.ID.Value, NPVRAssetWithArchivedState);
            // update the start time and the end time to the asset.
            List<Asset> UpdateAssetWithStartNEndTime = content.Assets.Where(a => a.Name.Equals(asset.Name)).ToList();
            foreach (Asset updateAsset in UpdateAssetWithStartNEndTime)
            {
                Property startTimeProperty = updateAsset.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRAssetStarttime));
                startTimeProperty.Value = startTime.ToString("yyyy-MM-dd HH:mm:ss");
                Property endTimeProperty = updateAsset.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRAssetEndtime));
                endTimeProperty.Value = endTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            mppWrapper.UpdateAssets(UpdateAssetWithStartNEndTime);
        }

        public override String GetAssetUrl(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, NPVRRecording recording, EPGChannel epgChannel)
        {
            var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType, AssetType.NPVR);
            DateTime dtFrom = recording.Start.Value;
            DateTime dtTo = recording.End.Value;
            GetStartAndEndtime(asset, dtFrom, dtTo);

            TimeSpan vbegin = UnifiedHelper.GetNPVRAssetStartOffset(dtFrom, asset);
            TimeSpan vend = (dtTo - dtFrom) + vbegin;            

            var source = epgChannel.ServiceEpgConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType);
            if (source == null)
                throw new Exception("No source matching " + deviceType + " was found in EpgConfig");
            String NPVRWebRoot = source.NpvrWebRoot;
            if (String.IsNullOrEmpty(NPVRWebRoot))
                throw new Exception("No NPVRWebRoot was set for " + deviceType + " in EpgConfig");
            if (!NPVRWebRoot.EndsWith("/"))
                NPVRWebRoot += "/";

            String url = NPVRWebRoot + epgChannel.NameInAlphanumeric.ToLower() + "/" + content.ID.Value + "/" + asset.Name + ".m3u8?";
            url += "vbegin=" + ((UInt64)vbegin.TotalSeconds).ToString() + "&";
            url += "vend=" + ((UInt64)vend.TotalSeconds).ToString(); // end

            String conaxContegoContentID = ConaxIntegrationHelper.Getcxid(epgChannel);
            if (String.IsNullOrWhiteSpace(conaxContegoContentID))
                conaxContegoContentID = ConaxIntegrationHelper.GetConaxContegoContentID(content);
            if (String.IsNullOrEmpty(conaxContegoContentID))
            {
                throw new Exception("conaxContego ContentID is missing for MPP content:" + content.Name + " ID:" + content.ID.Value.ToString());
            }
            url += "&cxid=" + conaxContegoContentID;
            return url;
        }
    }
}
