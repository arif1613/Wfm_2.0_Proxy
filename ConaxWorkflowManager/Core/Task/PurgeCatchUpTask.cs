using System;
using System.Collections.Generic;
using System.Linq;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class PurgeCatchUpTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");


            List<String> channelsToProces = null;
            if (this.TaskConfig.ConfigParams.ContainsKey("ChannelsToProcess") &&
                !String.IsNullOrEmpty(this.TaskConfig.GetConfigParam("ChannelsToProcess")))
            {
                channelsToProces = new List<String>();
                channelsToProces.AddRange(this.TaskConfig.GetConfigParam("ChannelsToProcess").Split(",".ToArray(), StringSplitOptions.RemoveEmptyEntries));
                log.Debug("Channels to process for this task are " + this.TaskConfig.GetConfigParam("ChannelsToProcess"));
            }
            else
            {
                log.Debug("All Channels will be processed for this task.");
            }

            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();           
            // delete 1 week back / catchup time
            DateTime deleteTime = DateTime.UtcNow.AddHours(-1 * Double.Parse(managerConfig.GetConfigParam("KeepCatchupAliveInHour")));
            

            // load channel list
            Dictionary<UInt64, EPGChannel> channelList = new Dictionary<UInt64, EPGChannel>();
            List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();
            //////////////////////////////////////////////////////////////////////
            // work on delete time for channels
            foreach (EPGChannel epgChannel in channels)
            {

                if (channelsToProces != null &&
                    !channelsToProces.Contains(epgChannel.MppContentId.ToString()))
                    continue;

                // calculate the datetime for this channel

                if (!channelList.ContainsKey(epgChannel.MppContentId))
                {
                    channelList.Add(epgChannel.MppContentId, epgChannel);
                }
                else
                {
                    log.Warn("Same contentID seems to been added twice for channels, id= " + epgChannel.MppContentId);
                }

                if (epgChannel.EnableCatchUpInAnyService)
                {
                    // fixed buffer length
                    epgChannel.DeleteTimeSSBuffer = deleteTime;
                    epgChannel.DeleteTimeHLSBuffer = deleteTime;
                    GetChannelDeleteTimeForCatchupChannel(epgChannel);
                }
                else if (epgChannel.EnableNPVRInAnyService)
                {
                    // buffer length is the last unproccessed npvr contents start time + some long pre-guard time.
                    epgChannel.DeleteTimeSSBuffer = DateTime.MinValue;
                    epgChannel.DeleteTimeHLSBuffer = DateTime.MinValue;
                    GetChannelDeleteTimeForNPVRChannel(epgChannel);
                }

            }

            // delete segment files
            BaseEncoderCatchupHandler smoothHandler = managerConfig.SmoothCatchUpHandler;
            BaseEncoderCatchupHandler hlshHandler = managerConfig.HLSCatchUpHandler;
            foreach (KeyValuePair<UInt64, EPGChannel> kvp in channelList)
            {
                try
                {
                    if (kvp.Value.EnableCatchUpInAnyService || kvp.Value.EnableNPVRInAnyService)
                    {
                        if (kvp.Value.DeleteTimeSSBuffer < DateTime.UtcNow)
                            smoothHandler.DeleteCatchupSegments(kvp.Value);

                        if (kvp.Value.DeleteTimeHLSBuffer < DateTime.UtcNow)
                            hlshHandler.DeleteCatchupSegments(kvp.Value);
                    }


                    // delete old catchup content and manfiest files
                    DeleteOldCatchupContent(kvp.Value, deleteTime);
                }catch(Exception ex) {
                    log.Warn("Failed to purge for channel " + kvp.Key, ex);
                }
            }

            
           
            log.Debug("DoExecute End");
        }

        private void DeleteContentsFromCubiWare(ContentData content)
        {
            log.Debug("DeleteContentsFromCubiWare");
            List<Property> publishedToProperties = content.Properties.FindAll(p => p.Type.Equals("ServiceExtContentID", StringComparison.OrdinalIgnoreCase));
            log.Debug("Found " + publishedToProperties.Count.ToString() + " publishedTo properties");
            ICubiTVMWServiceWrapper wrapper = null;
            foreach (Property publishedTo in publishedToProperties)
            {
                try
                {
                    log.Debug("PublishedTo value = " + publishedTo.Value);
                    String[] publishedInfo = publishedTo.Value.Split(':');
                    wrapper = CubiTVMiddlewareManager.Instance(ulong.Parse(publishedInfo[0]));
                    String id = publishedInfo[1];
                    log.Debug("catchupEventID= " + id);
                    if (!wrapper.DeleteContent(ulong.Parse(id)))
                        log.Warn("could not delete catchup event with id " + id);
                    else
                        log.Debug("Catchup event with id " + id + " was deleted");
                }
                catch (Exception ex)
                {
                    log.Warn("Error deleting catchup from cubiware", ex);
                }
            }
        }

        private void DeleteOldCatchupContent(EPGChannel epgChannel, DateTime deleteTime)
        {
            // Get old Catchup contents for delete
            List<ContentData> oldContents = new List<ContentData>();

            ContentSearchParameters contentSearchParameters = new ContentSearchParameters();
            contentSearchParameters.ContentRightsOwner = epgChannel.ContentRightOwner;
            contentSearchParameters.EventPeriodTo = deleteTime;
            //contentSearchParameters.Properties.Add(CatchupContentProperties.ChannelId, epgChannel.MppContentId.ToString());
            //contentSearchParameters.Properties.Add(CatchupContentProperties.ContentType, ContentType.CatchupTV.ToString("G"));
            //contentSearchParameters.Properties.Add(CatchupContentProperties.EnableCatchUp, true.ToString());
            //contentSearchParameters.Properties.Add(CatchupContentProperties.EnableNPVR, false.ToString());
            //contentSearchParameters.Properties.Add(SearchProperty.S_ChannelIdContentTypeEnableCatchUpEnableNPVR,
                //epgChannel.MppContentId.ToString() + ":" +
                //ContentType.CatchupTV.ToString("G") + ":" +
                //true.ToString() + ":" +
                //false.ToString());
            //List<ContentData> contents = mppWrapper.GetContent(contentSearchParameters, true);
            List<ContentData> contents = mppWrapper.GetContentFromProperties(contentSearchParameters, true);
            log.Info("Fetched " + contents.Count + " catchup contents to delete");


            contentSearchParameters.EventPeriodTo = DateTime.UtcNow.AddHours(-1 * CommonUtil.GetEpgHistoryTimeInHours());
            contentSearchParameters.Properties.Clear();
            //contentSearchParameters.Properties.Add(SearchProperty.S_ChannelIdContentTypeEnableCatchUpEnableNPVR,
                //epgChannel.MppContentId.ToString() + ":" +
                //ContentType.CatchupTV.ToString("G") + ":" +
                //false.ToString() + ":" +
                //false.ToString());
            List<ContentData> pureEpgContents = mppWrapper.GetContentFromProperties(contentSearchParameters, true);
            log.Info("Fetched " + pureEpgContents.Count + " pure epgs to delete");
            foreach (ContentData content in pureEpgContents)
            {
                if (!contents.Contains(content))
                    contents.Add(content);
            }

            //oldContents.AddRange(contents);
            oldContents =
                contents.Where(
                    c =>
                        c.Properties.Exists(
                            p => p.Type.Equals(CatchupContentProperties.EpgIsSynked) && p.Value.Equals(bool.TrueString)))
                    .ToList();

            log.Debug("contents to delete " + oldContents.Count);
            foreach (ContentData content in oldContents)
            {
                // TODO: Generate of manfiest is no longer implemented, so no support for this now.
                //// delete composite manfiest fiels
                //String ssCompfilename = content.ExternalID + ".csm";
                //// delete SS files
                //String destcomp = Path.Combine(epgChannel.SSCompositeFSRoot, ssCompfilename);
                //if (File.Exists(destcomp))
                //{
                //    log.Debug("delete ss composite " + destcomp);
                //    try
                //    {
                //        File.Delete(destcomp);
                //    }
                //    catch (Exception ex)
                //    {
                //        log.Error("Failed to delete " + destcomp, ex);
                //    }
                //}
                // TODO: Generate playlist is not updated, so we don't support it for now.
                //// delete hls files
                //String[] playlists = Directory.GetFiles(epgChannel.HLSCompositeFSRoot, content.ExternalID + "-*", SearchOption.TopDirectoryOnly);
                //if (playlists.Length > 0)
                //{
                //    foreach (String playlist in playlists)
                //    {
                //        log.Debug("Delete hls playlist " + playlist);
                //        try
                //        {
                //            File.Delete(playlist);
                //        }
                //        catch (Exception ex)
                //        {
                //            log.Error("Failed to delete " + playlist, ex);
                //        }
                //    }
                //}
                if (epgChannel.EnableCatchUpInAnyService || epgChannel.EnableNPVRInAnyService)
                    DeleteContentsFromCubiWare(content);
                // delete content in mPP
                log.Debug("Delete catchup content in mpp:" + content.Name + " " + content.ID);
                mppWrapper.DeleteContent(content);
            }
        }

        private void GetChannelDeleteTimeForCatchupChannel(EPGChannel epgChannel)
        {
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            //MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            List<ContentData> lastValidContents = new List<ContentData>();

            
            ContentSearchParameters contentSearchParameters = new ContentSearchParameters();
            contentSearchParameters.ContentRightsOwner = epgChannel.ContentRightOwner;
            contentSearchParameters.EventPoint = epgChannel.DeleteTimeSSBuffer;
            //contentSearchParameters.Properties.Add(CatchupContentProperties.ContentType, ContentType.CatchupTV.ToString("G"));
            //contentSearchParameters.Properties.Add(CatchupContentProperties.ChannelId, epgChannel.MppContentId.ToString());
            //contentSearchParameters.Properties.Add(CatchupContentProperties.EnableCatchUp, true.ToString());
            //contentSearchParameters.Properties.Add(SearchProperty.S_ChannelIdContentTypeEnableCatchUp, epgChannel.MppContentId.ToString() + ":" +
            //                                                                                           ContentType.CatchupTV.ToString("G") + ":" +
            //                                                                                           true.ToString());
            //List<ContentData> contents = mppWrapper.GetContent(contentSearchParameters, true);
            List<ContentData> contents = mppWrapper.GetContentFromProperties(contentSearchParameters, true);
            lastValidContents.AddRange(contents);
            

            // modify deletetime if needed.
            foreach (ContentData content in lastValidContents)
            {
                // add start pending time to event start
                DateTime EventPeriodFromWPending = content.EventPeriodFrom.Value.AddSeconds(-1 * Double.Parse(managerConfig.GetConfigParam("EPGStartTimePendingSec")));

                // Shorten the deletetime for the last valid contents start time.
                if (EventPeriodFromWPending < epgChannel.DeleteTimeSSBuffer) {
                    epgChannel.DeleteTimeSSBuffer = EventPeriodFromWPending;
                    epgChannel.DeleteTimeHLSBuffer = EventPeriodFromWPending;
                }
            }
        }

        private void GetChannelDeleteTimeForNPVRChannel(EPGChannel epgChannel) {

            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
            //// get unproccessed smooth NPVR content.
            //List<ContentData> ssContents = GetUnProcessedNPVRContent(epgChannel, CatchupContentProperties.NPVRAllRecordingsUpdatedWithSmooth);
            //if (ssContents.Count > 0) {
            //    var content = ssContents.Min(c => c.EventPeriodFrom);
            //    epgChannel.DeleteTimeSSBuffer = (DateTime)content;
            //    epgChannel.DeleteTimeSSBuffer = epgChannel.DeleteTimeSSBuffer.AddSeconds(-1 * Double.Parse(managerConfig.GetConfigParam("NPVRBufferPreGuardInSec")));
            //}

            //// get unproccessed hls NPVR content.
            //List<ContentData> hlsContents = GetUnProcessedNPVRContent(epgChannel, CatchupContentProperties.NPVRAllRecordingsUpdatedWithHLS);
            //if (hlsContents.Count > 0) {
            //    var content = hlsContents.Min(c => c.EventPeriodFrom);
            //    epgChannel.DeleteTimeHLSBuffer = (DateTime)content;
            //    epgChannel.DeleteTimeHLSBuffer = epgChannel.DeleteTimeHLSBuffer.AddSeconds(-1 * Double.Parse(managerConfig.GetConfigParam("NPVRBufferPreGuardInSec")));
            //}

            // get unproccessed hls NPVR content.
            List<ContentData> Contents = GetUnProcessedNPVRContent(epgChannel);
            if (Contents.Count > 0)
            {
                var content = Contents.Min(c => c.EventPeriodFrom);
                epgChannel.DeleteTimeSSBuffer = (DateTime)content;
                epgChannel.DeleteTimeSSBuffer = epgChannel.DeleteTimeSSBuffer.AddSeconds(-1 * Double.Parse(managerConfig.NPVRBufferPreGuardInSec.ToString()));
                epgChannel.DeleteTimeHLSBuffer = (DateTime)content;
                epgChannel.DeleteTimeHLSBuffer = epgChannel.DeleteTimeHLSBuffer.AddSeconds(-1 * Double.Parse(managerConfig.NPVRBufferPreGuardInSec.ToString()));
            }
            // make sure it's not too big numbers, shouldn't excceed 30 days.
            if (epgChannel.DeleteTimeSSBuffer < DateTime.UtcNow.AddDays(-30))
                epgChannel.DeleteTimeSSBuffer = DateTime.UtcNow.AddDays(-30);
            if (epgChannel.DeleteTimeHLSBuffer < DateTime.UtcNow.AddDays(-30))
                epgChannel.DeleteTimeHLSBuffer = DateTime.UtcNow.AddDays(-30);
        }

        private List<ContentData> GetUnProcessedNPVRContent(EPGChannel epgChannel) {

            ContentSearchParameters contentSearchParameters = new ContentSearchParameters();
            contentSearchParameters.ContentRightsOwner = epgChannel.ContentRightOwner;
            //contentSearchParameters.Properties.Add(CatchupContentProperties.ContentType, ContentType.CatchupTV.ToString("G"));
            //contentSearchParameters.Properties.Add(CatchupContentProperties.ChannelId, epgChannel.MppContentId.ToString());
            //contentSearchParameters.Properties.Add(CatchupContentProperties.EnableCatchUp, false.ToString());
            //contentSearchParameters.Properties.Add(CatchupContentProperties.EnableNPVR, true.ToString());
            //contentSearchParameters.Properties.Add(CatchupContentProperties.NPVRRecordingsstState, NPVRRecordingsstState.Ongoing.ToString("G"));

            //contentSearchParameters.Properties.Add(SearchProperty.S_ChannelIdContentTypeEnableCatchUpEnableNPVRNPVRRecordingsstState, epgChannel.MppContentId.ToString() + ":" +
            //                                                                                                                          ContentType.CatchupTV.ToString("G") + ":" +
            //                                                                                                                          false.ToString() + ":" +
            //                                                                                                                          true.ToString() + ":" +
            //                                                                                                                          NPVRRecordingsstState.Ongoing.ToString("G"));

            //List<ContentData> contents = mppWrapper.GetContent(contentSearchParameters, true);
            List<ContentData> contents = mppWrapper.GetContentFromProperties(contentSearchParameters, true);
            return contents;
        }
    }
}
