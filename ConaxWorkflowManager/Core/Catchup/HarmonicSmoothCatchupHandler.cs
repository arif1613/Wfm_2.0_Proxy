using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Harmonic;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Carbon;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public class HarmonicSmoothCatchupHandler : BaseEncoderCatchupHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override void GenerateManifest(List<String> channelsToProces)
        {
            // handle by harmonic server, just update the state
            // TODO: do I need to keep this manifest state anymore? since WFM no longer generate any.

            //List<ContentData> allcontents = GetContentToProccess(CatchupContentProperties.CatchupSSManifestState, channelsToProces);
            //log.Debug("Total " + allcontents.Count + " content for Catchup enabled to update CatchupSSManifestState.");
            //foreach (ContentData content in allcontents)
            //{
            //    log.Debug("Update CatchupSSManifestState for content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID);

            //    var property = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.CatchupSSManifestState, StringComparison.OrdinalIgnoreCase));
            //    property.Value = ManifestState.Available.ToString("G");
            //    mppWrapper.UpdateContent(content);
            //}
        }

        public override void GenerateNPVR(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime startTime, DateTime endTime)
        {
            log.Debug("process content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID);
            IHarmonicOriginWrapper wrapper = HarmonicOriginWrapperManager.Instance;

            var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType, AssetType.NPVR);
            // Archive SS vod from codeshop to long term storage
            // update to recording state for same assets in mpp.
            List<Property> NPVRAssetWithRecordingState = ConaxIntegrationHelper.SetNPVRAssetArchiveStateByAssetName(content, asset.Name,
                                                                       NPVRAssetArchiveState.Recording);
            mppWrapper.UpdateContentProperties(content.ID.Value, NPVRAssetWithRecordingState);

            CallStatus recordResult = wrapper.RecordSmoothContent(content, serviceObjId, serviceViewLanugageISO, deviceType, startTime, endTime);
            if (!recordResult.Success)
            {
                // failed to record smooth asset
                log.Error("Failed to Record content " + content.Name + " " + content.ExternalID + " with minStart:" + startTime.ToString("yyyy-MM-dd HH:mm:ss") + " maxEnd:" + endTime.ToString("yyyy-MM-dd HH:mm:ss"), recordResult.Exception);
                //if (recordResult.ReturnCode == -2)
                //{
                //    // empty manifest, mark this content that the segements are not available.
                //    log.Error(recordResult.Message + " mark this content that the segments are not available.");
                //    var SSSourceSegmentStateProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.SSSourceSegmentState, StringComparison.OrdinalIgnoreCase));
                //    SSSourceSegmentStateProperty.Value = ManifestState.NotAvailable.ToString("G");
                //    mppWrapper.UpdateContentProperty(content.ID.Value, SSSourceSegmentStateProperty);
                //}
                //throw new Exception(recordResult.Error, recordResult.Exception);
                List<Property> NPVRAssetWithFailedState = ConaxIntegrationHelper.SetNPVRAssetArchiveStateByAssetName(content, asset.Name,
                                                                       NPVRAssetArchiveState.Failed);
                mppWrapper.UpdateContentProperties(content.ID.Value, NPVRAssetWithFailedState);
            }
            else
            {
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
                log.Debug("Smooth asset " + asset.Name + " archived for content " + content.Name + " " + content.ExternalID);
            }

        }

        public override void ProcessArchive(List<String> channelsToProces)
        {
            // do nothing
            // handle by Harmonic server
        }

        public override void DeleteCatchupSegments(EPGChannel epgChannel)
        {
            // Delete catchup buffer.
            // do nothing, Harmonics doesn't have this kind of API, the encoder will take care of if for a fixed configured time on the encoer.
        }

        public override void DeleteNPVR(ContentData content, Asset assetToDelete)
        {
            IHarmonicOriginWrapper wrapper = HarmonicOriginWrapperManager.Instance;
            CallStatus res = wrapper.DeleteSmoothContent(content, assetToDelete);
            if (res.ErrorCode >= 400 && res.ErrorCode <= 409)
            {
                log.Debug("Delete asset " + assetToDelete.Name + " for content " + content.Name + " " + content.ID.Value + " " + content.ExternalID + " has following status code " + res.ErrorCode + " " + res.Error);
            }
            else if (res.Exception != null)
            {
                throw new Exception(res.Error, res.Exception);
            }            
        }

        public override String CreateAssetName(ContentData content, UInt64 serviceObjId, DeviceType deviceType, EPGChannel channel)
        {
            // for catchup
            // http://<delivery-ip>/Content/SS/Catchup/Channel(name=<channel-name>,startTime=<program-start-time>,endTime=<program-end-time>).isml/Manifest
            // http://10.4.8.99/Content/SS/Live/Channel(nrk1_Clear2).isml/Manifest

            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);

            if (channel == null)
                channel = CatchupHelper.GetEPGChannel(content);
            String stream = channel.ServiceEpgConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType).Stream;

            Int32 pos1 = stream.IndexOf("/live/channel(", 0, StringComparison.OrdinalIgnoreCase);
            if (pos1 == -1)
                log.Error("Stream not a valid Harmonic stream format " + stream);

            String catchupUrl = stream.Substring(0, pos1);

            String streamName = CarbonEncoderHelper.GetStreamName(stream);

            Int32 startTimePendingSec = systemConfig.EPGStartTimePendingSec;
            Int32 endTimePendingSec = systemConfig.EPGEndTimePendingSec;
            Int64 startTime = CarbonEncoderHelper.GetEpochTime(content.EventPeriodFrom.Value.AddSeconds(-1 * startTimePendingSec));
            Int64 endTime = CarbonEncoderHelper.GetEpochTime(content.EventPeriodTo.Value.AddSeconds(endTimePendingSec));
            
            String paramString = "(name=" + streamName + ",startTime=" + startTime + ",endTime=" + endTime + ")";
            catchupUrl += "/Catchup/Channel" + paramString + ".isml/Manifest";


            return catchupUrl;
        }

        public override String GetAssetUrl(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, NPVRRecording recording, EPGChannel epgChannel)
        {
            // for npvr recordigns
            // http://<delivery-ip>/Content/SS/LLCU/Asset(<asset-name>).ism/Manifest
            // http://10.4.8.99/Content/SS/Live/Channel(nrk1_Clear2).isml/Manifest
            String stream = epgChannel.ServiceEpgConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType).Stream;

            Int32 pos1 = stream.IndexOf("/live/channel(", 0, StringComparison.OrdinalIgnoreCase);
            String npvrUrl = stream.Substring(0, pos1);

            var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType, AssetType.NPVR);

            String paramString = "(" + asset.Name + ")";
            npvrUrl += "/LLCU/Asset" + paramString + ".isml/Manifest";


            return npvrUrl;
        }
    }
}
