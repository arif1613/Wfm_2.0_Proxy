using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public class NullHLSCatchupHandler : BaseEncoderCatchupHandler
    {
        public override void GenerateManifest(List<String> channelsToProces)
        {
            
        }

        public override void GenerateNPVR(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime startTime, DateTime endTime)
        {
            // TODO: do nothing? leave notarchived state?
            //var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType);
            //var sameAssets = content.Assets.Where(a => a.Name.Equals(asset.Name));
            //List<Asset> assetsToUpdate = new List<Asset>(sameAssets);

            //// update state for same assets in mpp.
            //foreach (Asset sameAsset in assetsToUpdate)
            //{
            //    var archiveStateProeprty = sameAsset.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRAssetArchiveState));
            //    archiveStateProeprty.Value = NPVRAssetArchiveState.Archived.ToString();
            //}
            //mppWrapper.UpdateAssets(assetsToUpdate);
        }

        public override void ProcessArchive(List<String> channelsToProces)
        {
            
        }

        public override void DeleteCatchupSegments(EPGChannel epgChannel)
        {
            
        }

        public override void DeleteNPVR(ContentData content, Asset assetToDelete)
        {
            
        }

        public override string CreateAssetName(ContentData content, UInt64 serviceObjId, DeviceType deviceType, EPGChannel channel)
        {
            return "";
        }

        public override string GetAssetUrl(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, NPVRRecording recording, EPGChannel epgChannel)
        {
            return "";
        }
    }
}
