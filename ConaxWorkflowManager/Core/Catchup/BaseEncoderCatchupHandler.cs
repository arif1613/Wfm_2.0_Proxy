using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using System.Xml;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public abstract class BaseEncoderCatchupHandler
    {
        protected MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public abstract void GenerateManifest(List<String> channelsToProces);

        public abstract void GenerateNPVR(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime startTime, DateTime endTime);

        public abstract void ProcessArchive(List<String> channelsToProces);

        public abstract void DeleteCatchupSegments(EPGChannel epgChannel);

        public abstract void DeleteNPVR(ContentData content, Asset assetToDelete);

        public abstract String CreateAssetName(ContentData content, UInt64 serviceObjId, DeviceType deviceType, EPGChannel epgChannel);

        public abstract String GetAssetUrl(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, NPVRRecording recording, EPGChannel epgChannel);

        //protected virtual List<ContentData> GetUnprocessedCatchUpContents(AvailableDateTime availableDateTime, String manifestStateType, String CROName)
        //{
        //    //var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "MPP").SingleOrDefault();
        //    //DateTime availableTime = DateTime.UtcNow;
        //    //List<ContentRightsOwner> cros = mppWrapper.GetContentRightsOwners();

        //    ContentSearchParameters contentSearchParameters = new ContentSearchParameters();
        //    contentSearchParameters.ContentRightsOwner = CROName;
        //    contentSearchParameters.Properties.Add(CatchupContentProperties.ContentType, ContentType.CatchupTV.ToString("G"));
        //    contentSearchParameters.Properties.Add(CatchupContentProperties.ChannelId, availableDateTime.ChannelId.ToString());
            
        //    contentSearchParameters.EventPeriodTo = availableDateTime.UTCMaxEndtime;
        //    if (manifestStateType == CatchupContentProperties.NPVRSSManifestState ||
        //        manifestStateType == CatchupContentProperties.NPVRHLSManifestState)
        //    {   // NPVR type
        //        //if (manifestStateType == CatchupContentProperties.NPVRSSManifestState)
        //        //{  // search content for smooth 
        //        //    contentSearchParameters.Properties.Add(CatchupContentProperties.NPVRAllRecordingsUpdatedWithSmooth, false.ToString());
        //        //    contentSearchParameters.Properties.Add(CatchupContentProperties.SSSourceSegmentState, ManifestState.Available.ToString("G"));
        //        //}
        //        //else if (manifestStateType == CatchupContentProperties.NPVRHLSManifestState)
        //        //{    // earch content for hls
        //        //    contentSearchParameters.Properties.Add(CatchupContentProperties.NPVRAllRecordingsUpdatedWithHLS, false.ToString());
        //        //    contentSearchParameters.Properties.Add(CatchupContentProperties.HLSSourceSegmentState, ManifestState.Available.ToString("G"));
        //        //}
        //        contentSearchParameters.Properties.Add(CatchupContentProperties.NPVRRecordingsstState, NPVRRecordingsstState.Ongoing.ToString("G"));
        //    }
        //    else { 
        //        // catchup type
        //        contentSearchParameters.Properties.Add(manifestStateType, ManifestState.NotAvailable.ToString("G"));
        //    }

        //    List<ContentData> contents = new List<ContentData>();

        //    contents = mppWrapper.GetContent(contentSearchParameters, true);
        //    foreach(ContentData content in contents) {
        //        List<ContentAgreement> agreements = mppWrapper.GetAllServicesForContent(content);
        //        content.ContentAgreements = agreements;
        //    }

        //    return contents;
        //}

        protected virtual void GetStartAndEndtime(Asset asset, DateTime dtFrom, DateTime dtTo)
        {
            Property NPVRAssetStarttimeProperty = asset.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRAssetStarttime));
            if (!String.IsNullOrWhiteSpace(NPVRAssetStarttimeProperty.Value))
            {
                DateTime archivedStarttime = DateTime.ParseExact(NPVRAssetStarttimeProperty.Value, "yyyy-MM-dd HH:mm:ss", null);
                // if user recordings start time is older than the arcvhied start time
                // use best efforct.
                if (dtFrom < archivedStarttime)
                    dtFrom = archivedStarttime;                
            }

            Property NPVRAssetEndtimeProperty = asset.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRAssetEndtime));
            if (!String.IsNullOrWhiteSpace(NPVRAssetEndtimeProperty.Value))
            {
                DateTime archivedEndtime = DateTime.ParseExact(NPVRAssetEndtimeProperty.Value, "yyyy-MM-dd HH:mm:ss", null);
                // if user recordings end time is greater than the arcvhied end time
                // use best efforct.
                if (dtTo > archivedEndtime)
                    dtTo = archivedEndtime;
            }
        }

        protected virtual void GetMinStartNMaxEnd(ContentData content, out DateTime minStart, out DateTime maxEnd)
        {
            minStart = DateTime.MaxValue;
            maxEnd = DateTime.MinValue;
            foreach (MultipleContentService servcie in content.ContentAgreements[0].IncludedServices)
            {
                ICubiTVMWServiceWrapper cubiWrapper = CubiTVMiddlewareManager.Instance(servcie.ObjectID.Value);

                // check in cubi if any recordings.
                List<NPVRRecording> recordings = cubiWrapper.GetNPVRRecording(content.ExternalID);
                if (recordings.Count == 0)
                {
                    // no recordings on this cubi.
                    continue;
                }

                DateTime start = recordings.Min(r => r.Start.Value);
                if (minStart > start)
                    minStart = start;
                DateTime end = recordings.Max(r => r.End.Value); ;
                if (maxEnd < end)
                    maxEnd = end;
            }
        }

        protected virtual Dictionary<UInt64, List<NPVRRecording>> GetAllRecordingsForContent(ContentData content)
        {
            Dictionary<UInt64, List<NPVRRecording>> allRecordings = new Dictionary<UInt64, List<NPVRRecording>>();

            foreach (MultipleContentService servcie in content.ContentAgreements[0].IncludedServices)
            {
                ICubiTVMWServiceWrapper cubiWrapper = CubiTVMiddlewareManager.Instance(servcie.ObjectID.Value);

                // get recordings
                List<NPVRRecording> recordings = cubiWrapper.GetNPVRRecording(content.ExternalID);
                allRecordings.Add(servcie.ObjectID.Value, recordings);
            }

            return allRecordings;
        }

        //protected virtual List<ContentData> GetContentToProccess(String type, List<String> channelsToProces)
        //{
        //    List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();

        //    // check ´content 
        //    List<ContentData> allcontents = new List<ContentData>();
        //    List<AvailableDateTime> availableDateTimes = new List<AvailableDateTime>();

        //    foreach (EPGChannel channel in channels)
        //    {
        //        // load catchup enabled/ NPVR enabled channels
        //        if ((channel.EnableCatchUp && type.Equals(CatchupContentProperties.CatchupSSManifestState)) ||
        //            (channel.EnableNPVR && type.Equals(CatchupContentProperties.NPVRSSManifestState)))
        //        {
        //            if (channelsToProces != null)
        //            {
        //                // run defiend channels only
        //                if (!channelsToProces.Contains(channel.MppContentId.ToString()))
        //                    continue; // skip this foler. not set for this task
        //            }
        //            availableDateTimes.Add(new AvailableDateTime(channel.MppContentId, DateTime.UtcNow));
        //        }
        //    }
        //    //log.Debug("number of Channels matched to process " + availableDateTimes.Count);
        //    foreach (AvailableDateTime availableDateTime in availableDateTimes)
        //    {
        //        //log.Debug("For SS storage: ChannelId " + availableDateTime.ChannelId + " availableDateTimes " + availableDateTime.UTCMaxEndtime.ToString("yyyy-MM-dd HH:mm:ss"));
        //        //XElement channelNode = EPGChannelConfigXMLFile.XPathSelectElement("Channels/Channel[@id='" + availableDateTime.ChannelId + "']");
        //        var channel = channels.First(c => c.MppContentId == availableDateTime.ChannelId);
        //        //String CROName = channelNode.XPathSelectElement("ContentRightsOwner").Value;
        //        List<ContentData> contents = GetUnprocessedCatchUpContents(availableDateTime, type, channel.ContentRightOwner);
        //        //log.Debug("Found " + contents.Count + " countent for " + type + " to update for this channel.");
        //        allcontents.AddRange(contents);
        //    }
        //    return allcontents;
        //}
    }
}
