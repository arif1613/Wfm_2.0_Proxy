using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using log4net;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.EPG;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.Diagnostics;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using System.Collections;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class EPGIngestTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ICubiTVMWServiceWrapper wrapper = CubiTVMiddlewareManager.Instance(0);
        private MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
        private static Hashtable cachedChannels = new Hashtable();
        private DateTime _executeTime;
        public override void DoExecute()
        {
            log.Debug("DoExecute Start");
            Stopwatch totalTimer = new Stopwatch();
            totalTimer.Start();
            try
            {
                var managerConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager");
                Double keepCatchupAliveInHour = Double.Parse(managerConfig.GetConfigParam("KeepCatchupAliveInHour"));
                _executeTime = DateTime.UtcNow;
                // get all EPG items from feed(s)
                log.Debug("Fetching epgs from the feed");
                Dictionary<UInt64, List<EpgContentInfo>> epgItems = GetEpgItemsFromFeeds();
                var epgItemsCount = 0;

                foreach (KeyValuePair<UInt64, List<EpgContentInfo>> kvp in epgItems)
                {
                    epgItemsCount += kvp.Value.Count;
                }

                log.Debug("Found " + epgItemsCount + " epg items");

                // get all EPG items from MPP within EPG feeds range
                log.Debug("Fetching items to process for epg");
                var epgFetchStopwatch = new Stopwatch();
                epgFetchStopwatch.Start();


                //List<EpgContentInfo> mppEpgInfos = GetEPGInfosFromMPPByFeedRange(epgItems);
                Dictionary<UInt64, List<EpgContentInfo>> mppEpgInfos = GetEPGInfosFromMPPByFeedRange(epgItems);
                epgFetchStopwatch.Stop();
                log.Debug("Fetched epgInfos, took " + (epgFetchStopwatch.ElapsedMilliseconds / 1000).ToString() + "s");
                var mppEpgInfosCount = 0;
                
                foreach(KeyValuePair<UInt64, List<EpgContentInfo>> kvp in mppEpgInfos) {
                    if (kvp.Value != null)
                        mppEpgInfosCount += mppEpgInfos.Count;
                }
                log.Debug("Fetched " + mppEpgInfosCount + " already existing epgInfos");

                // remove from epgItems if EPG couldn't be fetch from mpp.
                foreach (KeyValuePair<UInt64, List<EpgContentInfo>> kvp in mppEpgInfos)
                {
                    if (kvp.Value == null) { 
                        // no EPG was abeld be fetched from MPP
                        // remove epg items from the epg feed list. don't process thou now.
                        log.Debug("EPG for channel " + kvp.Key + " will not be processed, since failed to fetch epgs from MPP");
                        epgItems.Remove(kvp.Key);
                    }
                }

                var newContent = new List<ContentData>();
                var contentsToSendToCubiware = new List<ContentData>();
                List<EpgContentInfo> catchupToDeleteFromCubiware = new List<EpgContentInfo>();
                // check for matches on externalId (channel + programId + start + stop)
                var hitList = new List<MappedItem>();
                String hitListName = "Hitlist_" + _executeTime.ToString("yyyyMMdd_HHmm");
                log.Debug("Matching to existings epgs in mpp");
                foreach (KeyValuePair<UInt64, List<EpgContentInfo>> kvp in epgItems)
                {
                    foreach (EpgContentInfo epgInfo in kvp.Value)
                    {
                        var match = MapEpgItemToExisting(epgInfo, mppEpgInfos[epgInfo.ChannelID]);

                        if (match != null)
                        {
                            PrintToSeparateLogs(hitListName,
                                "EpgItem " + epgInfo.Content.Name + ":" + epgInfo.Content.ExternalID +
                                " matched content with id" + match.Content.ID + "name " + match.Content.Name + "ExternalId=" + match.Content.ExternalID);
                                
                            hitList.Add(new MappedItem()
                                {
                                    EpgFeedItem = epgInfo,
                                    MppEpgItem = match
                                });
                            mppEpgInfos[epgInfo.ChannelID].Remove(match);
                        }
                        else
                        {
                            PrintToSeparateLogs(hitListName,
                                "EpgItem " + epgInfo.Content.Name + ":" + epgInfo.Content.ExternalID +
                                " didnt match any existing items");
                            newContent.Add(epgInfo.Content);
                        }
                    }
                }
                contentsToSendToCubiware.AddRange(newContent);

                var updatedContent = new List<ContentData>();

                String updateListName = "UpdateList_" + _executeTime.ToString("yyyyMMdd_HHmm");
                // check hitlist for changes
                foreach (var mappedItem in hitList)
                {
                    if (mappedItem.EpgFeedItem.MetaDataHash != mappedItem.MppEpgItem.MetaDataHash)
                    {
                        PrintToSeparateLogs(updateListName,
                            "EpgItem " + mappedItem.MppEpgItem.Content.Name + ":" + mappedItem.MppEpgItem.Content.ID +
                            " Had new data, needs update");
                        // update
                        var toUpdate = mappedItem.EpgFeedItem.Content;
                        toUpdate.ID = mappedItem.MppEpgItem.Content.ID;
                        toUpdate.ExternalID = mappedItem.MppEpgItem.Content.ExternalID;
                        SetToNonSynked(toUpdate);
                        updatedContent.Add(toUpdate);
                        contentsToSendToCubiware.Add(toUpdate);
                    }
                    else if (!mappedItem.MppEpgItem.IsPublishedToAllServices)
                    {
                        PrintToSeparateLogs(updateListName,
                           "EpgItem " + mappedItem.MppEpgItem.Content.Name + ":" + mappedItem.MppEpgItem.Content.ID +
                           " wasn't published to all services, need to send again");
                        // check if the EPG in MPP is synced yet, it might never get ingested to Cubi or some problem in Cubi it never get created, we need to resent the XML.
                        var toUpdate = mappedItem.EpgFeedItem.Content;
                        toUpdate.ExternalID = mappedItem.MppEpgItem.Content.ExternalID;
                        contentsToSendToCubiware.Add(toUpdate);
                    }
                    // else: no changes
                }

                // delete old epg items from mpp, only items that overlaps feed rang                
                //var ChannelIDs = epgItems.Select(e => e.ChannelID).Distinct();
                String deleteContentListName = "DeleteInfo_" + _executeTime.ToString("yyyyMMdd_HHmm");
                log.Debug("Start to delete old content from MPP");
                var totalNumberOfEpgInMppToDel = 0;
                foreach (KeyValuePair<UInt64, List<EpgContentInfo>> kvp in epgItems)
                {
                    //var eventStart = epgItems.Where(e => e.ChannelID == ChannelID).Min(e => e.Content.EventPeriodFrom);
                    //var eventEnd = epgItems.Where(e => e.ChannelID == ChannelID).Max(e => e.Content.EventPeriodTo);
                    var eventStart = kvp.Value.Min(e => e.Content.EventPeriodFrom);
                    var eventEnd = kvp.Value.Max(e => e.Content.EventPeriodTo);

                    PrintToSeparateLogs(deleteContentListName,
                        "Checking epg to delete for channel " + kvp.Key + ", eventStart= " + eventStart + ", eventEnd= " +
                        eventEnd);
                    //var epgInMppToDeleted = mppEpgInfos.Where(c => c.ChannelID == ChannelID 
                    //                                            && c.Content.EventPeriodFrom >= eventStart 
                    //                                            && c.Content.EventPeriodTo <= eventEnd);

                    var epgInMppToDeleted = mppEpgInfos[kvp.Key].Where(c => c.Content.EventPeriodFrom >= eventStart
                                                                         && c.Content.EventPeriodTo <= eventEnd);
                    PrintToSeparateLogs(deleteContentListName,
                       "There where " + epgInMppToDeleted.Count() + " epg to delete for channel " + kvp.Key);
                    foreach(EpgContentInfo epgInMppToDel in epgInMppToDeleted)
                    {
                        PrintToSeparateLogs(deleteContentListName,
                            "Deleting content " + epgInMppToDel.Content.Name + ", id= " + epgInMppToDel.Content.ID +
                            "eventDates from=" + epgInMppToDel.Content.EventPeriodFrom.ToString() + ": to= " +
                            epgInMppToDel.Content.EventPeriodTo.ToString());
                        Deletecontent(epgInMppToDel.Content);
                    }
                   
                    if (epgInMppToDeleted.Count() > 0)
                        catchupToDeleteFromCubiware.AddRange(epgInMppToDeleted.ToList());
                    
                    totalNumberOfEpgInMppToDel += epgInMppToDeleted.Count();
                }
                log.Debug("Deleted " + totalNumberOfEpgInMppToDel + " content from MPP");
                

                // create new content in mpp
                log.Debug("There are " + newContent.Count + " content that doesn't exist in MPP, creating");
                if (newContent.Count > 0)
                {
                    SendContentsToMpp(newContent, true);
                }
                log.Debug("Content are created in MPP");

                // update content in mpp
                log.Debug("There are " + updatedContent.Count + " content that needs to be updated in MPP");
                if (updatedContent.Count > 0)
                {
                    SendContentsToMpp(updatedContent, false);
                }
                log.Debug("Content are updated in MPP");

                // send to cubi
                if (contentsToSendToCubiware.Count > 0)
                {
                    contentsToSendToCubiware = contentsToSendToCubiware.OrderBy(p => p.EventPeriodFrom).ToList();
                    log.Debug("There are " + contentsToSendToCubiware.Count.ToString() + " to send to Cubiware");
                    Int32 XMLTVImportChunkSize = 1000;
                    if (managerConfig.ConfigParams.ContainsKey("XMLTVImportChunkSize"))
                        XMLTVImportChunkSize = Int32.Parse(managerConfig.GetConfigParam("XMLTVImportChunkSize"));

                    //contentsToSendToCubiware = CommonUtil.FilterLockedEpgs(contentsToSendToCubiware);
                    List<List<ContentData>> splitlist = CommonUtil.SplitIntoChunks(contentsToSendToCubiware, XMLTVImportChunkSize);
                    log.Debug("Split into " + splitlist.Count.ToString() + " lists of  " + XMLTVImportChunkSize + " items");
                    int i = 0;
                    Stopwatch timer = new Stopwatch();
                    foreach (List<ContentData> newContentSublist in splitlist)
                    {   // execute sublist

                        // sort by services
                        i++;
                        try
                        {
                            log.Debug("Handling list " + i + " of " + splitlist.Count + " lists!");
                            timer.Start();
                            Dictionary<UInt64, List<ContentData>> contentByServices = new Dictionary<UInt64, List<ContentData>>();
                            contentByServices = CommonUtil.SortContentByServices(newContentSublist);

                            foreach (KeyValuePair<UInt64, List<ContentData>> kvp in contentByServices)
                            {
                                log.Debug("Foreach contentByService, key = " + kvp.Key.ToString());
                                wrapper = CubiTVMiddlewareManager.Instance(kvp.Key);
                                // POST XMLTV
                                try
                                {
                                    // filter exist cathucp
                                    List<ContentData> filterList = CommonUtil.FilterAlreadyExistingEpgs(kvp.Value, kvp.Key);
                                    if (filterList.Count > 0)
                                    {
                                        log.Debug("Creating epgImports");
                                        wrapper.CreateEpgImports(filterList, keepCatchupAliveInHour);
                                    }
                                    else
                                    {
                                        log.Debug("Filtered list is empty, nothing to create");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    log.Error("Failed to ingest XMLTV XML to CUbi.", ex);
                                    log.Debug("DoExecute End");
                                }
                            }
                            timer.Stop();
                            log.Debug("Done sending chunk " + i + ", took " + timer.ElapsedMilliseconds + " ms");
                            timer.Reset();
                        }
                        catch (Exception exc)
                        {
                            log.Debug("Error handling sublist, continuing to next", exc);
                        }
                    }
                }
                String deleteListName = "DeleteFromCubiware_" + _executeTime.ToString("yyyyMMdd_HHmm");
                log.Debug("Start to delete old catchup events from Cubiware");
                int numberOfCatchupEventsDeleted = 0;
                Stopwatch timer2 = new Stopwatch();
                timer2.Start();
                //log.Debug("There are " + catchupToDeleteFromCubiware.Count + " catchup to delete in cubiware " + kvp.Key);
                foreach (EpgContentInfo epgContentInfo in catchupToDeleteFromCubiware)
                {

                    EPGChannel epgChannel = GetCachedEPGChannel(epgContentInfo.Content);

                    foreach (
                        KeyValuePair<ulong, ServiceEPGConfig> sc in
                            epgChannel.ServiceEpgConfigs.Where(s => s.Value.EnableCatchup)
                        )
                    {
                        try
                        {
                            wrapper = CubiTVMiddlewareManager.Instance(sc.Key);
                            PrintToSeparateLogs(deleteListName,
                                "Deleting EpgItem " + epgContentInfo.Content.Name + ":" + epgContentInfo.Content.ID +
                                ":" + epgContentInfo.Content.ExternalID +
                                " From Cubiware");
                            if (wrapper.DeleteCatchupEvent(epgContentInfo.Content.ExternalID))
                                numberOfCatchupEventsDeleted++;
                        }
                        catch (Exception exc)
                        {
                            log.Warn(
                                "Error deleting content with contentId " + epgContentInfo.Content.ID +
                                " from Cubiware continue with next", exc);
                        }
                    }
                }
                timer2.Stop();
                log.Debug(numberOfCatchupEventsDeleted + " catchup events were deleted from Cubiware, took= " + timer2.ElapsedMilliseconds + " ms");
            }
            catch (Exception ex)
            {
                log.Error("EPGIngestTask failed to execute: " + ex.Message, ex);
            }
            totalTimer.Stop();
            log.Debug("EpgIngestTask done, took " + (totalTimer.ElapsedMilliseconds / 1000).ToString() + "s");
            cachedChannels.Clear();
            log.Debug("DoExecute End");
        }

        private void SetToNonSynked(ContentData toUpdate)
        {
            var propertyToUpdate =
                toUpdate.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.EpgIsSynked));
            propertyToUpdate.Value = bool.FalseString;
        }

        private Dictionary<UInt64, List<EpgContentInfo>> GetEPGInfosFromMPPByFeedRange(Dictionary<UInt64, List<EpgContentInfo>> epgItems)
        {
            Dictionary<UInt64, List<EpgContentInfo>> result = new Dictionary<UInt64, List<EpgContentInfo>>();
            //List<EpgContentInfo> result = new List<EpgContentInfo>();

            //var ChannelIDs = epgItems.Select(e => e.ChannelID).Distinct();

            foreach (KeyValuePair<UInt64, List<EpgContentInfo>> kvp in epgItems)
            {
                //var eventStart = epgItems.Where(e => e.ChannelID == ChannelID).Min(e => e.Content.EventPeriodFrom);
                //var eventEnd = epgItems.Where(e => e.ChannelID == ChannelID).Max(e => e.Content.EventPeriodTo);

                //List<EpgContentInfo> epgs = mppWrapper.GetEpgContentInfoByChannel(ChannelID, eventStart.Value, eventEnd.Value);
                List<EpgContentInfo> epgs = mppWrapper.GetEpgContentInfoByChannel(kvp.Key, CommonUtil.GetEpgHistoryTimeInHours(), _executeTime);
                result.Add(kvp.Key, epgs);
                //result.AddRange(epgs);
            }

            

            //mppWrapper.GetAllEpgInfos(CommonUtil.GetEpgHistoryTimeInHours());
            return result;
        }

        private EpgContentInfo MapEpgItemToExisting(EpgContentInfo epgItem, List<EpgContentInfo> itemsInMpp)
        {
            var epgContent = epgItem.Content;
            var match = itemsInMpp.FirstOrDefault(x => x.ExternalIDMatches(epgContent.ExternalID));
            if (match == null)
            {
                var epgChannel = epgContent.GetPropertyValue(CatchupContentProperties.ChannelId);
                var epgProgram = epgContent.GetPropertyValue(CatchupContentProperties.EpgProgrammeId);
                foreach (var channelAndProgramHit in itemsInMpp.Where(
                        x => x.Content.GetPropertyValue(CatchupContentProperties.ChannelId) == epgChannel && x.Content.GetPropertyValue(CatchupContentProperties.EpgProgrammeId) == epgProgram))
                {
                    var c = channelAndProgramHit.Content;
                    if (EpgOverlapsMppContent(c, epgContent))
                    {
                        match = channelAndProgramHit;
                        break;
                    }
                }
            }
            return match;
        }

        public static bool EpgOverlapsMppContent(ContentData c, ContentData epgContent)
        {
            bool hit = (c.EventPeriodFrom > epgContent.EventPeriodFrom && c.EventPeriodFrom < epgContent.EventPeriodTo);
            if (hit)
                return hit;
            hit = (c.EventPeriodTo < epgContent.EventPeriodTo && c.EventPeriodTo > epgContent.EventPeriodFrom);
            if (hit)
                return hit;
            hit = (c.EventPeriodFrom <= epgContent.EventPeriodFrom && c.EventPeriodTo >= epgContent.EventPeriodTo);
            if (hit)
                return hit;
            hit = (epgContent.EventPeriodFrom < c.EventPeriodFrom && epgContent.EventPeriodTo > c.EventPeriodTo);
            if (hit)
                return hit;
            return false;
            //return (c.EventPeriodFrom > epgContent.EventPeriodFrom && c.EventPeriodFrom < epgContent.EventPeriodTo)
            //       || (c.EventPeriodTo < epgContent.EventPeriodTo && c.EventPeriodTo > epgContent.EventPeriodFrom)
            //       || (c.EventPeriodFrom < epgContent.EventPeriodFrom && c.EventPeriodTo > epgContent.EventPeriodTo)
            //       || (epgContent.EventPeriodFrom < c.EventPeriodFrom && epgContent.EventPeriodTo > c.EventPeriodTo);
        }

        private void SendContentsToMpp(List<ContentData> contentList, bool isNew)
        {
            var managerConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager");
            int noToSendPerCall = 100;
            if (managerConfig.ConfigParams.ContainsKey("ContentsToSendPerCallToMpp"))
                noToSendPerCall = Int32.Parse(managerConfig.GetConfigParam("ContentsToSendPerCallToMpp"));

            log.Debug("Sending " + noToSendPerCall.ToString() + " content per call");

            List<List<ContentData>> chunks = CommonUtil.SplitIntoChunks(contentList, noToSendPerCall);
            int totalSent = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (List<ContentData> contentSublist in chunks)
            {
                totalSent += contentSublist.Count;
                log.Debug("sending " + totalSent.ToString() + " of " + contentList.Count().ToString() + " total contents");
                if (isNew)
                {
                    log.Debug("Calling CreateContent");
                    try
                    {
                        mppWrapper.AddContents(contentSublist);
                    }
                    catch (Exception exc)
                    {
                        log.Warn("Error adding content to MPP", exc);
                    }
                    log.Debug("Done creating content");
                }
                else
                {
                    log.Debug("Calling UpdateContent");
                    try
                    {
                        mppWrapper.UpdateContentsLimited(contentSublist);
                    }
                    catch (Exception exc)
                    {
                        log.Warn("Error when updating content in MPP", exc);
                    }
                    log.Debug("Done updating content");
                }
                
            }
            stopwatch.Stop();
            log.Debug("Sent a total of " + contentList.Count.ToString() + " in " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
        }


        private void Deletecontent(ContentData content)
        {
            try
            {
                log.Debug("Start deleting content " + content.ID + " " + content.Name);
                mppWrapper.DeleteContent(content);
            }
            catch (Exception ex)
            {
                log.Warn("Failed to delete content " + content.Name + " " + content.ID.Value);
            }
        }

        public static EPGChannel GetCachedEPGChannel(ContentData content)
        {
            String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;
            if (cachedChannels.ContainsKey(channelId))
            {
                return cachedChannels[channelId] as EPGChannel;
            }
            else
            {
                EPGChannel channel = CatchupHelper.GetEPGChannel(content);
                if (channel != null)
                    cachedChannels.Add(channelId, channel);
                return channel;
            }

        }

        private static void AddChannelsToCache(List<EPGChannel> channels)
        {
            foreach (EPGChannel channel in channels)
            {
                if (!cachedChannels.ContainsKey(channel.MppContentId.ToString()))
                {
                    cachedChannels.Add(channel.MppContentId.ToString(),channel);
                }
            }
        }

        public Dictionary<UInt64, List<EpgContentInfo>> GetEpgItemsFromFeeds()
        {
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            String EPGChannelConfigXMLUrl = systemConfig.EPGChannelConfigXML;
            XElement EPGChannelConfigXMLFile = XElement.Load(EPGChannelConfigXMLUrl);

            String CatchUpFilterConfigXML = systemConfig.CatchUpFilterConfigXML;
            XElement CatchUpFilterConfigXMLFile = XElement.Load(CatchUpFilterConfigXML);

            List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();
            log.Debug("Add " + channels.Count + " channels to cache");
            AddChannelsToCache(channels);
            log.Debug("Done adding channels to cache");

             var EPGParser = new XMLTVEPGParser();
            //var EPGParser = Activator.CreateInstance(System.Type.GetType(TaskConfig.GetConfigParam("EPGParser"))) as IEPGParser;
            Dictionary<UInt64, List<EpgContentInfo>> contents = new Dictionary<UInt64, List<EpgContentInfo>>();
            var feedNodes = EPGChannelConfigXMLFile.XPathSelectElements("Feeds/Feed");
            log.Debug("Found " + feedNodes.Count() + " locations for EPG feeds");
            foreach (XElement feedNode in feedNodes)
            {
                log.Debug("Load EPG feed from " + feedNode.Attribute("uri").Value);
                XElement cElementsFromFile;
                try
                {
                    cElementsFromFile = XElement.Load(feedNode.Attribute("uri").Value);
                    XmlFeedComparer.SaveFeedIfDifferent(cElementsFromFile, feedNode.Attribute("uri").Value, _executeTime);
                }
                catch (Exception ex)
                {
                    log.Warn("Could not load EPG feed from  " + feedNode.Attribute("uri").Value, ex);
                    continue;
                }
               
                TimeZoneInfo feedtimeZone;
                try
                {
                    feedtimeZone = TimeZoneInfo.FindSystemTimeZoneById(feedNode.Attribute("feedTimezone").Value);
                }
                catch (Exception ex)
                {
                    log.Error("Failed to find timeZone with id " + feedNode.Attribute("feedTimezone").Value, ex);
                    continue;
                }

                Dictionary<UInt64, List<EpgContentInfo>> epgContents = EPGParser.ParseEPGXML(cElementsFromFile, EPGChannelConfigXMLFile, CatchUpFilterConfigXMLFile, feedtimeZone, null, channels); // returns all EPG Content from feed

                PopulateContentList(contents, epgContents);
            }
            log.Debug("finish adding new epg contents to list");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            ModifyContent(contents);
            stopwatch.Stop();
            log.Debug("Modify content took + " + stopwatch.ElapsedMilliseconds.ToString() + " ms");

            return contents;
        }

        private void PopulateContentList(Dictionary<ulong, List<EpgContentInfo>> contents, Dictionary<ulong, List<EpgContentInfo>> epgContents)
        {
            var epgCount = 0;
            
            foreach (KeyValuePair<UInt64, List<EpgContentInfo>> kvp in epgContents)
            {
                epgCount += kvp.Value.Count;

                if (!contents.ContainsKey(kvp.Key))
                    contents.Add(kvp.Key, new List<EpgContentInfo>());

                foreach (var epgContentInfo in kvp.Value)
                {
                    var duplicate =
                        contents[kvp.Key].FirstOrDefault(
                            e =>
                                e.Content.GetPropertyValue(CatchupContentProperties.EpgProgrammeId) ==
                                epgContentInfo.Content.GetPropertyValue(CatchupContentProperties.EpgProgrammeId) &&
                                e.Content.EventPeriodFrom == epgContentInfo.Content.EventPeriodFrom);

                    if (duplicate != null)
                        contents[kvp.Key].Remove(duplicate);

                    contents[kvp.Key].Add(epgContentInfo);
                }
            }

            log.Debug("adding " + epgCount + " epgcontents");
        }


        //private IList<IngestItem> FindIngestItemToProcess(List<EpgContentInfo> existingEpgs)
        //{
        //    IList<IngestItem> ingestItems = new List<IngestItem>();

        //    //String EPGChannelConfigXMLUrl = TaskConfig.GetConfigParam("EPGChannelConfigXML");
        //    var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
        //    String EPGChannelConfigXMLUrl = systemConfig.GetConfigParam("EPGChannelConfigXML");
        //    XElement EPGChannelConfigXMLFile = XElement.Load(EPGChannelConfigXMLUrl);

        //    String CatchUpFilterConfigXML = systemConfig.GetConfigParam("CatchUpFilterConfigXML");
        //    XElement CatchUpFilterConfigXMLFile = XElement.Load(CatchUpFilterConfigXML);

        //    IEPGParser EPGParser = Activator.CreateInstance(System.Type.GetType(TaskConfig.GetConfigParam("EPGParser"))) as IEPGParser;

        //    List<EpgContentInfo> contents = new List<EpgContentInfo>();
        //    foreach (XElement feedNode in EPGChannelConfigXMLFile.XPathSelectElements("Feeds/Feed"))
        //    {
        //        log.Debug("Get EPG feed from " + feedNode.Attribute("uri").Value);
        //        XElement cElementsFromFile = XElement.Load(feedNode.Attribute("uri").Value);

        //        TimeZoneInfo feedtimeZone;
        //        try
        //        {
        //            feedtimeZone = TimeZoneInfo.FindSystemTimeZoneById(feedNode.Attribute("feedTimezone").Value);
        //        }
        //        catch (Exception ex)
        //        {
        //            log.Error("Failed to find timeZone with id " + feedNode.Attribute("feedTimezone").Value, ex);
        //            continue;
        //        }

        //        List<EpgContentInfo> epgContents = EPGParser.ParseEPGXML(cElementsFromFile, EPGChannelConfigXMLFile, CatchUpFilterConfigXMLFile, feedtimeZone, existingEpgs); // returns Content either not Existing in Mpp or that exists in Mpp but are not created in Cubiware
        //        log.Debug("adding " + epgContents.Count.ToString() + " epgcontents");
        //        contents.AddRange(epgContents);
        //        log.Debug("finish adding new epg contents to list");
        //    }

        //    // add aditional vaules
        //    Stopwatch stopwatch = new Stopwatch();
        //    stopwatch.Start();
        //    ModifyContent(contents);
        //    stopwatch.Stop();
        //    log.Debug("Modify content took + " + stopwatch.ElapsedMilliseconds.ToString() + " ms");
        //    foreach (EpgContentInfo epgInfo in contents)
        //    {
        //        //log.Debug("Handling epgContents " + content.Name);

        //        IngestItem ingestItem = new IngestItem();
        //        ingestItem.contentData = epgInfo.Content;
        //        ingestItem.Type = IngestType.AddContent;
        //        ingestItem.ExistsInMpp = epgInfo.ExistsInMpp;
        //        if (ingestItem.ExistsInMpp)
        //        {
        //            log.Debug("content exists in Mpp");
        //        }
        //        ingestItems.Add(ingestItem);

        //    }
        //    log.Debug("Done finding items to process, returning " + ingestItems.Count);
        //    return ingestItems;
        //}


        public static Boolean IsExceedEpgHistoryLimit(DateTime eventPeriodFrom, DateTime executeTime)
        {
            var deleteTime = executeTime.AddHours(-1 * CommonUtil.GetEpgHistoryTimeInHours());
            if (eventPeriodFrom <= deleteTime)
                return true;

            return false;
        }

        /// <summary>
        /// Add necessary data
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        private void ModifyContent(Dictionary<UInt64, List<EpgContentInfo>> contents)
        {
            log.Debug("ModifyContent");
            var MPPConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "MPP").SingleOrDefault();
            if (MPPConfig == null)
                throw new Exception("MPP system config is not configured in the ConaxWorkflowManagerConfig.xml.");

            Stopwatch timer = new Stopwatch();
            foreach(KeyValuePair<UInt64, List<EpgContentInfo>> kvp in contents) 
            {
                foreach (EpgContentInfo epg in kvp.Value)
                {
                    try
                    {
                        if (epg.ExistsInMpp)
                            continue;
                        timer.Restart();
                        ContentData content = epg.Content;
                        content.Properties.Add(new Property(CatchupContentProperties.ContentType, ContentType.CatchupTV.ToString("G")));

                      
                        var EnableNPVRProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.EnableNPVR));
                        if (EnableNPVRProperty != null && Boolean.Parse(EnableNPVRProperty.Value))
                        {
                            content.Properties.Add(new Property(CatchupContentProperties.NPVRRecordingsstState, NPVRRecordingsstState.Ongoing.ToString("G")));
                        }
                        content.HostID = MPPConfig.GetConfigParam("HostID");

                        CommonUtil.AddPublishInfoToContent(content, PublishState.Published);
                        if (bool.Parse(content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.EnableNPVR)).Value))
                            CommonUtil.AddEpgHasRecordingsProperty(content, false);
                        String agreementNames = "";
                        foreach (ContentAgreement agreement in content.ContentAgreements)
                            agreementNames += Environment.NewLine + agreement.Name;

                        if (Boolean.Parse(content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.EnableCatchUp)).Value))
                            AddCatchupAssets(content);
                        
                        if (Boolean.Parse(content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.EnableNPVR)).Value))
                            AddNPVRAssets(content);
                    }
                    catch (Exception exc)
                    {
                        log.Warn("Error when modifying content", exc);
                    }
                }
            }
            CatchupHelper.ClearChannelFromConfigCache();
        }


        private void AddNPVRAssets(ContentData content)
        {
            var mppConfig = (MPPConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.MPP);

            EPGChannel epgChannel =  GetCachedEPGChannel(content);
            Dictionary<String, Int32> postFixList = new Dictionary<String, Int32>();
            Int32 postFixCounter = 1;
            foreach (KeyValuePair<UInt64, ServiceEPGConfig> kvp in epgChannel.ServiceEpgConfigs)
            {
                if (kvp.Value.EnableNpvr)
                {
                    foreach (SourceConfig sc in kvp.Value.SourceConfigs)
                    {
                        if (!postFixList.ContainsKey(sc.Stream))
                            postFixList.Add(sc.Stream, postFixCounter++);

                        Asset asset = new Asset();
                        asset.Name = content.ExternalID + "_" + postFixList[sc.Stream];
                        asset.IsTrailer = false;
                        asset.DeliveryMethod = DeliveryMethod.Stream;
                        asset.LanguageISO = kvp.Value.ServiceViewLanugageIso;
                        asset.FileSize = 0;
                        asset.Properties.Add(new Property(CatchupContentProperties.DeviceType, sc.Device.ToString()));
                        asset.Properties.Add(new Property(CatchupContentProperties.AssetType,
                            AssetType.NPVR.ToString()));
                        asset.Properties.Add(new Property(CatchupContentProperties.NPVRAssetStarttime, ""));
                        asset.Properties.Add(new Property(CatchupContentProperties.NPVRAssetEndtime, ""));
                        //asset.Properties.Add(new Property(CatchupContentProperties.NPVRAssetArchiveState, NPVRAssetArchiveState.NotArchived.ToString()));
                        ConaxIntegrationHelper.SetNPVRAssetArchiveState(content, kvp.Value.ServiceViewLanugageIso,
                            sc.Device,
                            NPVRAssetArchiveState.Unknown);
                        AssetFormatType format = CommonUtil.GetAssetFormatTypeFromFileName(sc.Stream);
                        asset.Properties.Add(new Property(CatchupContentProperties.AssetFormatType,
                            format.ToString()));

                        content.Assets.Add(asset);
                    }
                }
            }
        }

        private void PrintToSeparateLogs(String logName, String toLog)
        {
            try
            {
                var systemConfig =
                    Config.GetConfig()
                        .SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager")
                        .SingleOrDefault();
                if (systemConfig.ConfigParams.ContainsKey("ExtraExtraEpgIngestLogging"))
                {
                    String folderPath = systemConfig.GetConfigParam("ExtraExtraEpgIngestLogging");

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                    folderPath = Path.Combine(folderPath, logName + ".log");
                    StreamWriter sw = new StreamWriter(folderPath, true);
                    sw.Write(toLog + Environment.NewLine);
                    sw.Close();
                }
            }
            catch (Exception exc)
            {
                log.Error("Error printing to ExtraExtraEpgIngestLogging", exc);
            }
        }

        private void AddCatchupAssets(ContentData content)
        {
            var mppConfig = (MPPConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.MPP);
            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            BaseEncoderCatchupHandler smoothHandler = managerConfig.SmoothCatchUpHandler;
            BaseEncoderCatchupHandler hlshHandler = managerConfig.HLSCatchUpHandler;

            String devicesString = content.GetPropertyValue("CatchUpDevices");
            List<String> allowDeviceList = new List<String>();
            if (!string.IsNullOrEmpty(devicesString))
            {
                var devices = devicesString.Split(new char[] { ',' });
                allowDeviceList.AddRange(devices);
            }

            EPGChannel epgChannel = GetCachedEPGChannel(content);
            foreach (KeyValuePair<UInt64, ServiceEPGConfig> kvp in epgChannel.ServiceEpgConfigs)
            {
                if (kvp.Value.EnableCatchup)
                {
                    foreach (SourceConfig sc in kvp.Value.SourceConfigs)
                    {
                        if (allowDeviceList.Count > 0)
                        {
                            // filter if we have list
                            if (!allowDeviceList.Contains(sc.Device.ToString()))
                                continue; // skip this devcie, it's not definced in the filter list.
                        }

                        String AssetName = "";
                        AssetFormatType formatType = CommonUtil.GetAssetFormatTypeFromFileName(sc.Stream);
                        if (formatType == AssetFormatType.HTTPLiveStreaming)
                            AssetName = hlshHandler.CreateAssetName(content, kvp.Key, sc.Device, epgChannel);
                        else if (formatType == AssetFormatType.SmoothStreaming)
                            AssetName = smoothHandler.CreateAssetName(content, kvp.Key, sc.Device, epgChannel);

                        Asset asset = new Asset();
                        asset.Name = AssetName;
                        asset.IsTrailer = false;
                        asset.DeliveryMethod = DeliveryMethod.Stream;
                        //asset.contentAssetServerName = mppConfig.DefaultCAS;
                        asset.LanguageISO = kvp.Value.ServiceViewLanugageIso;
                        asset.FileSize = 0;
                        asset.Properties.Add(new Property(CatchupContentProperties.DeviceType, sc.Device.ToString()));
                        asset.Properties.Add(new Property(CatchupContentProperties.AssetType,
                            AssetType.Catchup.ToString()));
                        AssetFormatType format = CommonUtil.GetAssetFormatTypeFromFileName(sc.Stream);
                        asset.Properties.Add(new Property(CatchupContentProperties.AssetFormatType, format.ToString()));

                        content.Assets.Add(asset);
                    }
                }
            }
        }

        //private void CreateAssets(SystemConfig MPPConfig, ContentData content, Boolean IsTraielr)
        //{
        //    //var deviceAndAssetMapping = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "DeviceAndAssetMapping").SingleOrDefault();
        //    //if (deviceAndAssetMapping == null)
        //    //    throw new Exception("DeviceAndAssetMapping is not configured in the ConaxWorkflowManagerConfig.xml.");

        //    var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
        //    BaseEncoderCatchupHandler smoothHandler = managerConfig.SmoothCatchUpHandler;
        //    BaseEncoderCatchupHandler hlshHandler = managerConfig.HLSCatchUpHandler;


        //    EPGChannel epgChannel = CatchupHelper.GetEPGChannel(content);


        //    //IEnumerable<KeyValuePair<String, String>> deviceAssetMap = deviceAndAssetMapping.ConfigParams;

        //    string devicesString = content.GetPropertyValue("CatchUpDevices");
        //    if (!string.IsNullOrEmpty(devicesString))
        //    {
        //        var devices = devicesString.Split(new char[] { ',' });
        //        deviceAssetMap = deviceAssetMap.Where(x => devices.Contains(x.Key));
        //    }

        //    Dictionary<AssetFormatType, Asset> assets = new Dictionary<AssetFormatType, Asset>();
        //    foreach (KeyValuePair<String, String> kvp in deviceAssetMap)
        //    {
        //        Asset asset;
        //        AssetFormatType formatType = (AssetFormatType)Enum.Parse(typeof(AssetFormatType), kvp.Value, true);
        //        if (assets.Keys.Contains(formatType))
        //        {
        //            // add new device type to existing asset
        //            asset = assets[formatType];
        //        }
        //        else
        //        {
        //            // create new asset.
        //            asset = new Asset();
        //            asset.IsTrailer = IsTraielr;
        //            asset.DeliveryMethod = DeliveryMethod.Stream;
        //            asset.contentAssetServerName = MPPConfig.GetConfigParam("DefaultCAS");
        //            assets.Add(formatType, asset);

        //            //String CubiChannelId = content.Properties.FirstOrDefault(p => p.Type.Equals("CubiChannelId", StringComparison.OrdinalIgnoreCase)).Value;
        //            //XElement channelNode = EPGChannelConfigXMLFile.XPathSelectElement("Channels/Channel[@cubiChannelId='" + CubiChannelId + "']");

        //            //String SSManifestGeneratorUrl = channelNode.XPathSelectElement("SS/CompositeWebRoot").Value;
        //            //if (!SSManifestGeneratorUrl.EndsWith("/"))
        //            //    SSManifestGeneratorUrl += "/";
        //            //String AssetName = SSManifestGeneratorUrl + content.ExternalID + ".csm";
        //            String AssetName = "";
        //            if (formatType == AssetFormatType.HTTPLiveStreaming)
        //            {
        //                //String HLSManifestGeneratorUrl = channelNode.XPathSelectElement("HLS/CompositeWebRoot").Value;
        //                //if (!HLSManifestGeneratorUrl.EndsWith("/"))
        //                //    HLSManifestGeneratorUrl += "/";
        //                //AssetName = HLSManifestGeneratorUrl + content.ExternalID + "-index.m3u8";
        //                AssetName = hlshHandler.CreateAssetName(content);
        //            }
        //            else if (formatType == AssetFormatType.SmoothStreaming)
        //            {
        //                AssetName = smoothHandler.CreateAssetName(content);
        //            }

        //            asset.Name = AssetName;
        //            //asset.LanguageISO = "ENG";
        //            asset.FileSize = 0;
        //        }
        //        asset.Properties.Add(new Property("DeviceType", kvp.Key));
        //    }

        //    // add to content
        //    foreach (KeyValuePair<AssetFormatType, Asset> pair in assets)
        //        content.Assets.Add(pair.Value);
        //}
    }
    public class MappedItem
    {
        public EpgContentInfo MppEpgItem { get; set; }
        public EpgContentInfo EpgFeedItem { get; set; }
    }
}
