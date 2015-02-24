using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Xml.Linq;
using System.Xml.XPath;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using System.Diagnostics;
using System.Collections;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public class CodeShopSmoothCatchupHandler : BaseEncoderCatchupHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //protected List<EPGChannel> channels = null;

        public override String CreateAssetName(ContentData content, UInt64 serviceObjId, DeviceType deviceType, EPGChannel channel)
        {
            String AssetName = "";
           
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            //String EPGChannelConfigXMLUrl = systemConfig.GetConfigParam("EPGChannelConfigXML");
            //XElement EPGChannelConfigXMLFile = XElement.Load(EPGChannelConfigXMLUrl);
          //  log.Debug("SS CreateAssetName, 1: " + timer.ElapsedMilliseconds.ToString() + "ms");
            //UInt64 channelId = UInt64.Parse(content.Properties.First(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value);
           // log.Debug("SS CreateAssetName, 2: " + timer.ElapsedMilliseconds.ToString() + "ms");
           // List<EPGChannel> channels = CatchupHelper.GetAllEPGChannelsFromConfigOnly();
            if (channel == null)
                channel = CatchupHelper.GetEPGChannel(content);
            //XElement channelNode = EPGChannelConfigXMLFile.XPathSelectElement("Channels/Channel[@cubiChannelId='" + CubiChannelId + "']");
            //String liveStreamUrl = channelNode.XPathSelectElement("SS/LiveStream").Value;
            String liveStreamUrl = channel.ServiceEpgConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType).Stream;
            //DateTime encoderOffset = DateTime.ParseExact(catchUpEncoderOffset, "yyyy-MM-dd", null);
            //TimeSpan vbegin = content.EventPeriodFrom.Value - encoderOffset;
            //TimeSpan vend = content.EventPeriodTo.Value - encoderOffset;
           
            Int32 startTimePendingSec = Int32.Parse(systemConfig.GetConfigParam("EPGStartTimePendingSec"));
            Int32 endTimePendingSec = Int32.Parse(systemConfig.GetConfigParam("EPGEndTimePendingSec"));
            TimeSpan vbegin = UnifiedHelper.GetServerTimeStamp(content.EventPeriodFrom.Value.AddSeconds(-1 * startTimePendingSec));
            TimeSpan vend = UnifiedHelper.GetServerTimeStamp(content.EventPeriodTo.Value.AddSeconds(endTimePendingSec));
            
            AssetName = liveStreamUrl + "?vbegin=" + ((UInt64)vbegin.TotalSeconds).ToString() + "&vend=" + ((UInt64)vend.TotalSeconds).ToString();
            return AssetName;
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
            String NPVRWebRoot = source.NpvrWebRoot;
            if (String.IsNullOrEmpty(NPVRWebRoot))
            {
                throw new Exception("Channel " + epgChannel.Name + " with id " + epgChannel.MppContentId +
                          " is missing NPVRWebRoot for deviceTpe= " + deviceType + " in service with objectID " +serviceObjId);
            }
            if (!NPVRWebRoot.EndsWith("/"))
                NPVRWebRoot += "/";

            String url = NPVRWebRoot + epgChannel.NameInAlphanumeric.ToLower() + "/" + content.ID.Value + "/" + asset.Name + ".ism/Manifest?";
            url += "vbegin=" + ((UInt64)vbegin.TotalSeconds).ToString() + "&";
            url += "vend=" + ((UInt64)vend.TotalSeconds).ToString(); // end
            return url;
        }

        #region Generate
        public override void GenerateManifest(List<String> channelsToProces)
        {
            log.Debug("Start SS Manifest Generation.");
            // do nothing
            //UnifiedServicesWrapper wrapper = new UnifiedServicesWrapper();
            //wrapper.RecordSmoothContent(null);
            //CubiTVMiddlewareManager.Instance(513697793).CreateNPVRRecording("6868");


            GenerateCatchupManifest(channelsToProces);

            //GenerateNPVRManifest(channelsToProces);
        }

        private void GenerateCatchupManifest(List<String> channelsToProces) 
        {
            // handle by codeshop server, just update the state
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
            //log.Debug("process content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID);
            IUnifiedServicesWrapper wrapper = UnifiedServicesWrapperManager.Instance;

            var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType, AssetType.NPVR);
            // Archive SS vod from codeshop to long term storage
            // update to recording state for same assets in mpp.
            List<Property> NPVRAssetWithRecordingState = ConaxIntegrationHelper.SetNPVRAssetArchiveStateByAssetName(content, asset.Name,
                                                                       NPVRAssetArchiveState.Recording);
            mppWrapper.UpdateContentProperties(content.ID.Value, NPVRAssetWithRecordingState);

            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.First(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            RecordResult recordResult = null;
            Int32 recordingRetries = 0;
            NPVRAssetLoger.WriteLog(content, "arcvhing start for service " + serviceObjId + " " + serviceViewLanugageISO + " " + deviceType.ToString());
            do {                
                recordResult = wrapper.RecordSmoothContent(content, serviceObjId, serviceViewLanugageISO, deviceType, startTime, endTime);
                if (recordResult.ReturnCode == -1) {
                    recordingRetries++;
                    log.Debug("unexpected error when recording content " + content.Name + " " + content.ExternalID + ", recording retry " + recordingRetries);
                }
            } while (recordResult.ReturnCode == -1 &&
                    recordingRetries <= systemConfig.RecordingRetries);

            NPVRAssetLoger.WriteLog(content, "arcvhing done for service " + serviceObjId + " " + serviceViewLanugageISO + " " + deviceType.ToString() + " return (" + recordResult.ReturnCode + ") " + recordResult.Message);
            if (recordResult.ReturnCode != 0)
            {
                // failed to record smooth asset
                log.Error("Failed to Record content " + content.Name + " " + content.ExternalID + " with minStart:" + startTime.ToString("yyyy-MM-dd HH:mm:ss") + " maxEnd:" + endTime.ToString("yyyy-MM-dd HH:mm:ss"), recordResult.Exception);
                //if (recordResult.ReturnCode == -2) { 
                    // empty manifest, mark this content that the segements are not available.
                    //log.Error(recordResult.Message + " mark this content that the segments are not available.");
                    //var SSSourceSegmentStateProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.SSSourceSegmentState, StringComparison.OrdinalIgnoreCase));
                    //SSSourceSegmentStateProperty.Value = ManifestState.NotAvailable.ToString("G");
                    //mppWrapper.UpdateContentProperty(content.ID.Value, SSSourceSegmentStateProperty);
                //}
                //throw new Exception(recordResult.Message, recordResult.Exception);
                log.Debug("Setting archiveState failed for asset " + asset.Name);
                List<Property> NPVRAssetWithFailedState = ConaxIntegrationHelper.SetNPVRAssetArchiveStateByAssetName(content, asset.Name,
                                                                       NPVRAssetArchiveState.Failed);
                mppWrapper.UpdateContentProperties(content.ID.Value, NPVRAssetWithFailedState);
            }
            else
            {
                // update to archived state in mpp.
                List<Property> NPVRAssetWithArchivedState = ConaxIntegrationHelper.SetNPVRAssetArchiveStateByAssetName(content, asset.Name,
                                                                       NPVRAssetArchiveState.Archived);
                log.Debug("Updating ArchiveState to Archived for " + NPVRAssetWithArchivedState.Count + " devices for content " + content.Name + " with id " + content.ID);
                mppWrapper.UpdateContentProperties(content.ID.Value, NPVRAssetWithArchivedState);
                // update the start time and the end time to the asset.
                List<Asset> UpdateAssetWithStartNEndTime = content.Assets.Where(a => a.Name.Equals(asset.Name)).ToList();
                foreach (Asset updateAsset in UpdateAssetWithStartNEndTime)
                {
                    log.Debug("Handeling start and end time");
                    Property startTimeProperty =  updateAsset.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRAssetStarttime));
                    startTimeProperty.Value = startTime.ToString("yyyy-MM-dd HH:mm:ss");
                    Property endTimeProperty = updateAsset.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRAssetEndtime));
                    endTimeProperty.Value = endTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                log.Debug("Updating assets");
                mppWrapper.UpdateAssets(UpdateAssetWithStartNEndTime);

                log.Debug("Smooth asset  " + asset.Name + " archived for content " + content.Name + " " + content.ExternalID);
            }

        }

        #endregion

        #region LoadToDB
        public override void ProcessArchive(List<String> channelsToProces)
        {
            // do nothing
            // handle by codeshop server


        }
        #endregion

        #region Delete
        public override void DeleteCatchupSegments(EPGChannel epgChannel)
        {
            // get all smooth streams
            List<String> streams = new List<String>();
            foreach (KeyValuePair<UInt64, ServiceEPGConfig> kvp in epgChannel.ServiceEpgConfigs) { 
                foreach(SourceConfig sc in kvp.Value.SourceConfigs) {
                    // add new smooth url
                    if (!streams.Contains(sc.Stream) && sc.Stream.IndexOf(".isml", 0, StringComparison.OrdinalIgnoreCase) > 0)
                        streams.Add(sc.Stream);                    
                }
            }

            
            foreach(String stream in streams) {
            
                // Delete catchup buffer.
                IUnifiedServicesWrapper uniWrapper = UnifiedServicesWrapperManager.Instance;
            
                //epgChannel.SSLiveStream
                String callUrl = "";

                Int32 pos = stream.IndexOf(".isml");
                callUrl = stream.Substring(0, pos + 5);

                TimeSpan ts = DateTime.UtcNow - epgChannel.DeleteTimeSsBuffer;

                callUrl += "/purge?t=" + String.Format("{0:00}", (Int32)ts.TotalHours) + ":" +
                                         String.Format("{0:00}", ts.Minutes) + ":" +
                                         String.Format("{0:00}", ts.Seconds);

            

                uniWrapper.DeleteBuffer(callUrl);
            }

        }

        public override void DeleteNPVR(ContentData content, Asset assetToDelete)
        {
            // delete from rest script?
            IUnifiedServicesWrapper uw = UnifiedServicesWrapperManager.Instance;
            RecordResult res = uw.DeleteSmoothAsset(content, assetToDelete);
            if (res.ReturnCode >= 400 && res.ReturnCode <= 409) {
                log.Debug("Delete asset " + assetToDelete.Name + " for content " + content.Name + " " + content.ID.Value + " " + content.ExternalID + " has following status code " + res.ReturnCode + " " + res.Message);
            }
            else if (res.Exception != null) {
                throw new Exception(res.Message, res.Exception);
            }
        }

        #endregion

    }
}
