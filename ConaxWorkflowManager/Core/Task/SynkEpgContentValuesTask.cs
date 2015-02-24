using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Services.Description;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using System.Diagnostics;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.Collections;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.EPG;
using System.IO;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class SynkEpgContentValuesTask : BaseTask
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ICubiTVMWServiceWrapper wrapper = CubiTVMiddlewareManager.Instance(0);
        private MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        private SystemConfig managerConfig =
            Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

        private Hashtable fetchStatus = new Hashtable();
        private List<ulong> totallySynkedForEpgIds = new List<ulong>();

        private Hashtable cachedChannels = new Hashtable();

        private string retriesLogName;

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");

            retriesLogName = "SynkRetries_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmm");

            try
            {
                Double keepCatchupAliveInHour = Double.Parse(managerConfig.GetConfigParam("KeepCatchupAliveInHour"));

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                log.Debug("Fetching contents from Mpp");
                List<ContentData> contentToSync = mppWrapper.GetAllNonSynkedEpgContents();
                log.Debug("Fetched " + contentToSync.Count + " contents from Mpp");
                ContentLocker.LockContentList(contentToSync);
                //SetAllContentToSynked(contentToSync);
                contentToSync = contentToSync.OrderBy(p => p.EventPeriodFrom).ToList();
                Dictionary<UInt64, List<ContentData>> sortedContents = new Dictionary<UInt64, List<ContentData>>();

                IDictionary<String, ContentData> totalContentToUpdate = new Dictionary<String, ContentData>();
               List<ContentData> toGetCatchupDataFor = new List<ContentData>();
                sortedContents = CommonUtil.SortContentByServices(contentToSync);
                SetAllContentToNonSynked(sortedContents);
                List<ContentData> failedContents = new List<ContentData>();
                String unpublishedChannelIgnoresLogName = "UnpublishedChannelsIngoredContent " +
                                                          DateTime.UtcNow.ToString("yyyyMMdd_HHmm");
                foreach (KeyValuePair<UInt64, List<ContentData>> kvp in sortedContents)
                {
                    //var localContentsToUpdate = new List<ContentData>();
                    try
                    {
                        wrapper = CubiTVMiddlewareManager.Instance(kvp.Key);
                        List<string> externalIds = new List<string>();
                        Hashtable contentMappedToExternalId = new Hashtable();
                        foreach (ContentData content in kvp.Value)
                        {
                            EPGChannel channel = GetCachedEPGChannel(content);
                            if (channel == null)
                                continue;
                            ;
                            if (!ConaxIntegrationHelper.IsPublishedToService(kvp.Key, channel))
                            {
                                PrintQueueToSeparateLogs(unpublishedChannelIgnoresLogName,
                                    "Skipping content " + content.Name + ". id = " + content.ID +
                                    " beloning to channel " + channel.Name + " since it's not published to service, setting to synked for service" +
                                    kvp.Key);
                                SetSynkedForService(content.ID.Value, kvp.Key, true);
                                continue;
                            }
                            //if (!channel.ServiceEPGConfigs[kvp.Key].EnableCatchup)
                            //{
                            //    PrintQueueToSeparateLogs(unpublishedChannelIgnoresLogName,
                            //       "Skipping content " + content.Name + ". id = " + content.ID +
                            //       " beloning to channel " + channel.Name + " since it's not catchupEnabled for service, setting to synked for service" +
                            //       kvp.Key);
                            //    SetSynkedForService(content.ID.Value, kvp.Key, true);
                            //}
                            if (String.IsNullOrWhiteSpace(ConaxIntegrationHelper.GetCubiEpgId(kvp.Key, content)))
                            {
                                if (!externalIds.Contains(content.ExternalID))
                                    externalIds.Add(content.ExternalID);
                                if (!contentMappedToExternalId.ContainsKey(content.ExternalID))
                                    contentMappedToExternalId.Add(content.ExternalID, content);
                            }
                            else
                            {
                                if (!toGetCatchupDataFor.Contains(content)) // since it have epgId but still comes out it probably needs synking from catchup
                                    toGetCatchupDataFor.Add(content);
                                SetSynkedForService(content.ID.Value, kvp.Key, true);
                                if (!totalContentToUpdate.ContainsKey(content.ExternalID))
                                    totalContentToUpdate.Add(content.ExternalID, content);
                            }

                        }
                        log.Debug("Fetching epgids for  " + externalIds.Count + " contents from Mpp");
                        Hashtable externalMappedToCubiEpgId = wrapper.GetListOfCubiEpgIds(externalIds);
                        log.Debug("got" + externalMappedToCubiEpgId.Values.Count + " epgIds back from Cubiware");

                        // update EpgID
                        foreach (String externalId in externalIds)
                        {
                            ContentData content = contentMappedToExternalId[externalId] as ContentData;
                          //  log.Debug("checking" + content.Name + " with id " + content.ID + " for epgid");
                            try
                            {
                                String cubiepgid = externalMappedToCubiEpgId[externalId] as String;
                                
                                if (!String.IsNullOrWhiteSpace(cubiepgid))
                                {
                                    log.Debug("checking" + content.Name + " with id " + content.ID + " had epgid " + cubiepgid);
                                    ConaxIntegrationHelper.SetCubiEpgId(kvp.Key, content, cubiepgid);
                                    SetSynkedForService(content.ID.Value, kvp.Key, true);
                                    if (!totalContentToUpdate.ContainsKey(content.ExternalID))
                                        totalContentToUpdate.Add(content.ExternalID, content);
                                }
                                else
                                {
                                    if (!failedContents.Contains(content))
                                    {
                                        HandleFailedSynk(content, "SynkTask_");
                                        failedContents.Add(content);
                                    }
                                    continue;
                                }

                            }
                            catch (Exception ex)
                            {
                                log.Warn("Error fetching epgID for content + " + content.Name, ex);
                                try
                                {
                                    if (!failedContents.Contains(content))
                                    {
                                        HandleFailedSynk(content, "SynkTask_");
                                        failedContents.Add(content);
                                    }
                                }
                                catch (Exception)
                                {
                                    log.Warn("Error setting failed synk", ex);
                                }
                            }
                        }
                        log.Debug("Done fetching from service with id " + kvp.Key);
                    }
                    catch (Exception exc)
                    {
                        log.Warn("Error sync EPG data on servcie " + kvp.Key, exc);
                    }

                }
                log.Debug("Done fetching epgIds for all services");
                log.Info("Bulkupdating properties for " + failedContents.Count + " contents");
                BulkUpdateIsSynkedProperties(failedContents);
                log.Info("Done Bulkupdating"); 
                failedContents.Clear();
                
                try
                {
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    List<ContentData> contentsToUpdate = AddSynkedIfDone(totalContentToUpdate.Values.ToList());
                    log.Debug("Done AddingSynkedIfDone, took " + timer.Elapsed + " ms");
                    timer.Restart();
                    log.Debug("Updating epgIds in mpp");
                    UpdateContentsWithIds(contentsToUpdate);
                  //  mppWrapper.UpdateContentsInChunks(contentsToUpdate);
                    log.Debug("Done updating " + contentsToUpdate.Count + " contents in mpp, took= " + timer.ElapsedMilliseconds + "ms");
                    //Add Content that had epgId already but probably needs catchupData
                    AddNonCatchupSynkedData(totalContentToUpdate, toGetCatchupDataFor);
                }
                catch (Exception exc)
                {
                    log.Error("Error updating contents in Mpp with epgIds", exc);
                    throw;
                }
                SetAllContentToNonSynked(sortedContents);
                log.Debug("Start fetching for catchup");
                if (!AnyContentHaveCatchup(sortedContents))
                    return;
                foreach (KeyValuePair<UInt64, List<ContentData>> kvp in sortedContents)
                {
                    var localContentsToUpdate = new List<ContentData>();
                    wrapper = CubiTVMiddlewareManager.Instance(kvp.Key);
                    foreach (ContentData content in kvp.Value)
                    {
                        EPGChannel epgChannel =  GetCachedEPGChannel(content);
                        if (epgChannel == null)
                            continue;
                        if (!epgChannel.ServiceEPGConfigs[kvp.Key].EnableCatchup)
                            continue;
                        if (!totalContentToUpdate.Values.Contains(content))
                            continue;
                        if (!ConaxIntegrationHelper.IsPublishedToService(kvp.Key, epgChannel))
                        {
                            SetSynkedForService(content.ID.Value, kvp.Key, true);
                            continue;
                        }
                        // update catchup event in cubi and update catchup id in MPP
                        XmlDocument catchupdoc = new XmlDocument();
                        try
                        {
                            // check if content and channel is catchup enabled first
                            
                            if (!epgChannel.ServiceEPGConfigs[kvp.Key].EnableCatchup)
                                continue;
                            var ContentEnableCatchUp =
                                content.Properties.FirstOrDefault(
                                    p =>
                                        p.Type.Equals(CatchupContentProperties.EnableCatchUp,
                                            StringComparison.OrdinalIgnoreCase));
                            if (ContentEnableCatchUp == null || !Boolean.Parse(ContentEnableCatchUp.Value))
                                continue;
                            log.Debug("check Catchup " + content.Name + " " + content.ExternalID + " in Cubi " +
                                      kvp.Key);
                            catchupdoc = wrapper.GetCatchUpContent(content.ExternalID);
                            if (catchupdoc == null)
                            {
                                log.Warn("Could not find catchup with externalId " + content.ExternalID +
                                         " trying synk next run");
                                if (!failedContents.Contains(content))
                                {
                                    HandleFailedSynk(content, "SynkTask_");
                                    failedContents.Add(content);
                                };
                                continue;
                            }

                            log.Debug("update Catchup " + content.Name + " " + content.ExternalID + " in Cubi");
                            // update cubit content with assets

                            double catchupHour = keepCatchupAliveInHour;
                            var CatchUpHoursproperty =
                                content.Properties.FirstOrDefault(
                                    p => p.Type.Equals("CatchUpHours", StringComparison.OrdinalIgnoreCase));
                            if (CatchUpHoursproperty != null)
                                catchupHour = double.Parse(CatchUpHoursproperty.Value);

                            // Get cover id
                            XmlDocument CCDoc = wrapper.GetCatchupChannelByCatchupEvent(catchupdoc);
                            Int32 coverID =
                                Int32.Parse(CCDoc.SelectSingleNode("catchup-channel/cover/id").InnerText);

                            wrapper.UpdateCatchUpContent(content, catchupdoc, catchupHour, coverID);
                            String cubiContentID = catchupdoc.SelectSingleNode("catchup-event/id").InnerText;
                            log.Debug("Setting cubiTVID to " + cubiContentID);
                            ConaxIntegrationHelper.SetCubiTVContentID(kvp.Key, content, cubiContentID);
                            SetSynkedForService(content.ID.Value, kvp.Key, true);
                            //if (!localContentsToUpdate.Contains(content))
                            //    localContentsToUpdate.Add(content);
                            //if (!contentToUpdate.ContainsKey(content.ExternalID))
                            //    contentToUpdate.Add(content.ExternalID, content);
                        }
                        catch (Exception ex)
                        {
                            log.Error(
                                "Failed to get Catchup " + content.Name + " " + content.ExternalID + " in Cubi",
                                (ex.InnerException != null) ? ex.InnerException : ex);
                            try
                            {
                                if (!failedContents.Contains(content))
                                {
                                    HandleFailedSynk(content, "SynkTask_");
                                    failedContents.Add(content);
                                }
                            }
                            catch (Exception exc)
                            {

                            }
                            continue;
                        }

                    }
                    //try
                    //{
                    //    mppWrapper.UpdateContentsInChunks(localContentsToUpdate);
                    //}
                    //catch (Exception exc)
                    //{
                    //    log.Error("Error updating contents in Mpp with epgId", exc);
                    //    throw;
                    //}
                }

                stopwatch.Stop();
                log.Debug("fetched info from cubi and update catchup on contents, took " +
                          stopwatch.ElapsedMilliseconds.ToString() + "ms");
                stopwatch.Reset();
                log.Debug("Updating " + totalContentToUpdate.Count.ToString() + " contents in Mpp");
                stopwatch.Start();

                List<ContentData> contents = AddSynkedIfDoneForAll(totalContentToUpdate.Values.ToList());
                log.Debug("Updating of ids on epgs in mpp");
                UpdateContentsWithIds(contents);
                log.Debug("HandleFailedEpgs");
                if (failedContents.Any())
                    HandleFailedSynks(sortedContents,failedContents);
                // bulk update in MPP
                ContentLocker.UnLockContentList(contentToSync);
                stopwatch.Stop();

                log.Debug("Update done, took " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                log.Debug("DoExecuteEnd SynkEpgTask");
            }
            catch (Exception exc)
            {
                log.Error("Error synking contents", exc);
                ContentLocker.ClearLockedContentList();
                throw;
            }

            
        }

        private bool AnyContentHaveCatchup(Dictionary<ulong, List<ContentData>> sortedContents)
        {
            foreach (ulong serviceId in sortedContents.Keys)
            {
                foreach (ContentData content in sortedContents[serviceId])
                {
                    EPGChannel epgChannel = GetCachedEPGChannel(content);
                    if (epgChannel == null)
                        continue;
                    if (epgChannel.ServiceEPGConfigs[serviceId].EnableCatchup)
                    {
                        //sortedContents[serviceId].Remove(content);
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateContentsWithIds(List<ContentData> contentsToUpdate)
        {
            List<UpdatePropertiesForContentParameter> updates = new List<UpdatePropertiesForContentParameter>();
            foreach (ContentData content in contentsToUpdate)
            {
                UpdatePropertiesForContentParameter updateParameter = new UpdatePropertiesForContentParameter();
                updateParameter.Content = content;
                Property property =
                    content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.EpgIsSynked));
                if (bool.Parse(property.Value))
                {
                    updateParameter.Properties.Add(new KeyValuePair<string, Property>("UPDATE", property));
                }
                var cubiEpgIdsProperties =
                    content.Properties.Where(p => p.Type.Equals(CatchupContentProperties.CubiEpgId));
                foreach (Property epgProperty in cubiEpgIdsProperties)
                {
                    updateParameter.Properties.Add(new KeyValuePair<string, Property>("ADD", epgProperty));
                }
                var cubiCatchupIdsProperties =
                    content.Properties.Where(p => p.Type.Equals(CatchupContentProperties.ServiceExtContentID));
                foreach (Property catchupIdProperties in cubiCatchupIdsProperties)
                {
                    updateParameter.Properties.Add(new KeyValuePair<string, Property>("ADD", catchupIdProperties));
                }
                updates.Add(updateParameter);
            }
            mppWrapper.UpdateContentsPropertiesInChunks(updates);
        }

        private void BulkUpdateIsSynkedProperties(List<ContentData> failedContents)
        {
            PrintQueueToSeparateLogs(retriesLogName, "BulkUpdateIsSynkedProperties of " + failedContents.Count + " content");   
            List<UpdatePropertiesForContentParameter> updates = new List<UpdatePropertiesForContentParameter>();
            foreach (ContentData content in failedContents)
            {
                UpdatePropertiesForContentParameter updateParameter = new UpdatePropertiesForContentParameter();
                updateParameter.Content = content;
                Property property =
                    content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.EpgIsSynked));
                updateParameter.Properties.Add(new KeyValuePair<string, Property>("UPDATE", property));
                PrintQueueToSeparateLogs(retriesLogName, "Content " + content.Name + ": id= " + content.ID + " has IsEpgSynked= " + property.Value);
                if (property.Value.Equals(bool.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    PrintQueueToSeparateLogs(retriesLogName, "Adding NoOfEpgSynkRetries propety to update for " + content.Name + ": id= " + content.ID);
                    Property retryProperty =
                   content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.NoOfEpgSynkRetries));
                    if (retryProperty == null)
                    {
                        PrintQueueToSeparateLogs(retriesLogName, "NoOfEpgSynkRetries propety is null for " + content.Name + ": id= " + content.ID + " adding new with value 1");
                        retryProperty = new Property(CatchupContentProperties.NoOfEpgSynkRetries, "1");
                        updateParameter.Properties.Add(new KeyValuePair<string, Property>("ADD", retryProperty));
                    }
                    else
                    {
                        PrintQueueToSeparateLogs(retriesLogName, "Value of NoOfEpgSynkRetries propety for " + content.Name + ": id= " + content.ID + " is " + retryProperty.Value);
                        updateParameter.Properties.Add(new KeyValuePair<string, Property>("UPDATE", retryProperty));
                    }
                    var cubiEpgIdsProperties =
                    content.Properties.Where(p => p.Type.Equals(CatchupContentProperties.CubiEpgId));
                    foreach (Property epgProperty in cubiEpgIdsProperties)
                    {
                        updateParameter.Properties.Add(new KeyValuePair<string, Property>("ADD", epgProperty));
                    }
                   

                }
                updates.Add(updateParameter);
            }
            mppWrapper.UpdateContentsPropertiesInChunks(updates);
        }

        private void AddNonCatchupSynkedData(IDictionary<string, ContentData> totalContentToUpdate, List<ContentData> toGetCatchupDataFor)
        {
            foreach (ContentData content in toGetCatchupDataFor)
            {
                CheckTotallySynkedForAllServices(content);
                if (!totalContentToUpdate.ContainsKey(content.ExternalID))
                {
                    totalContentToUpdate.Add(content.ExternalID, content);
                }
            }
        }

        private void HandleFailedSynks(Dictionary<ulong, List<ContentData>> sortedContents, List<ContentData> failedContents)
        {
            List<ContentData> contents = new List<ContentData>();
            foreach (KeyValuePair<UInt64, List<ContentData>> kvp in sortedContents)
            {
                foreach (ContentData content in kvp.Value)
                {
                    if (!contents.Contains(content))
                    {
                        contents.Add(content);
                    }
                }
            }
            
            foreach (ContentData content in contents)
            {
                if (failedContents.Contains(content)) // no need to call HandleFailedSynk again since it has already been done on this
                    continue;
                if (!totallySynkedForEpgIds.Contains(content.ID.Value) || !IsSynkedInAllServices(content.ID.Value))
                {
                    PrintQueueToSeparateLogs("SynkTask_", "Setting failed for content " + content.Name + ", id= " + content.ID);
                    HandleFailedSynk(content, "SynkTask_");
                }
            }
            BulkUpdateIsSynkedProperties(contents);
        }

        private List<ContentData> AddSynkedIfDoneForAll(List<ContentData> contents)
        {
            foreach (ContentData content in contents)
            {
                if (totallySynkedForEpgIds.Contains(content.ID.Value) && IsSynkedInAllServices(content.ID.Value))
                {
                    Property property = content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.EpgIsSynked));
                    property.Value = bool.TrueString;
                }
            }
            return contents;
        }

        private List<ContentData> AddSynkedIfDone(List<ContentData> contents)
        {
            foreach (ContentData content in contents)
            {

                if (!HaveCatchupInAnyService(content))
                {
                    if (IsSynkedInAllServices(content.ID.Value))
                    {
                        NPVRHelper.SetSynked(content, true);
                    }
                }
                else
                {
                    CheckTotallySynkedForAllServices(content);
                }
               
            }
            return contents;
        }

        private void CheckTotallySynkedForAllServices(ContentData content)
        {
            if (IsSynkedInAllServices(content.ID.Value))
            {
                totallySynkedForEpgIds.Add(content.ID.Value);
            }
        }


        private bool HaveCatchupInAnyService(ContentData content)
        {

            EPGChannel epgChannel = GetCachedEPGChannel(content);// CatchupHelper.GetEPGChannel(content);
            if (epgChannel == null)
                return false;
            return epgChannel.ServiceEPGConfigs.Values.Where(p => p.EnableCatchup == true).Any();
        }

        private EPGChannel GetCachedEPGChannel(ContentData content)
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


        private void SetAllContentToNonSynked(Dictionary<ulong, List<ContentData>> sortedContents)
        {
            foreach (KeyValuePair<UInt64, List<ContentData>> kvp in sortedContents)
            {
                foreach (ContentData content in kvp.Value)
                {
                    SetSynkedForService(content.ID.Value, kvp.Key, false);
                }
            }
        }

        private void SetSynkedForService(ulong contentId, ulong serviceObjectId, bool isSynked)
        {
            if (!fetchStatus.ContainsKey(contentId))
            {
                Hashtable contentTable = new Hashtable();
                contentTable.Add(serviceObjectId, isSynked);
                fetchStatus.Add(contentId, contentTable);
            }
            else
            {
                Hashtable contentTable = fetchStatus[contentId] as Hashtable;
                if (!contentTable.ContainsKey(serviceObjectId))
                {
                    contentTable.Add(serviceObjectId, isSynked);
                }
                else
                {
                    contentTable[serviceObjectId] = isSynked;
                }
            }
        }

        private bool IsSynkedForService(ulong contentId, ulong serviceObjectId)
        {
            if (!fetchStatus.ContainsKey(contentId))
            {
                return false;
            }
            else
            {
                Hashtable contentTable = fetchStatus[contentId] as Hashtable;
                if (!contentTable.ContainsKey(serviceObjectId))
                    return false;
                else
                {
                    return (bool) contentTable[serviceObjectId];
                }
            }
        }

        private bool IsSynkedInAllServices(ulong contentId)
        {
            if (!fetchStatus.ContainsKey(contentId))
            {
                return false;
            }
            else
            {
                Hashtable contentTable = fetchStatus[contentId] as Hashtable;
                return !contentTable.ContainsValue(false);
            }
        }


        private ContentData HandleFailedSynk(ContentData content, String logName)
        {
            int noOfAttemptsToDo = GetNoOfTriesToDo();
            int noOfRetries = NPVRHelper.IncreaseEpgSynkRetries(content);
            PrintQueueToSeparateLogs(logName, "no of retries to synk is " + noOfRetries);
            PrintQueueToSeparateLogs(retriesLogName, "no of retries to synk is " + noOfRetries + " for content " + content.Name + " with id= " + content.ID);
            if (noOfRetries > noOfAttemptsToDo)
            {
                log.Warn("Tried to synk " + content.Name + " " + content.ID + " " + content.ExternalID + " more then " +
                         noOfAttemptsToDo + "! will not synk again");
                PrintQueueToSeparateLogs(retriesLogName, "Tried to synk " + content.Name + " " + content.ID + " " + content.ExternalID + " more then " +
                         noOfAttemptsToDo + "! will not synk again");
                NPVRHelper.SetSynked(content, true);
                //Property property =
                //    content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.EpgIsSynked));
                //property.Value = bool.TrueString;
                //mppWrapper.UpdateContentProperty(content.ID.Value, property);
            }
            else
            {
                PrintQueueToSeparateLogs(retriesLogName, "Tried to synk " + content.Name + " " + content.ID + " " + content.ExternalID + " but failed, setting synked false");
                NPVRHelper.SetSynked(content, false);
            }
            return content;
        }

        private int GetNoOfTriesToDo()
        {
            int noOfAttemptsToSynkItem = 5;
            if (managerConfig.ConfigParams.ContainsKey("EPGItemSynkRetries") && !String.IsNullOrEmpty(managerConfig.GetConfigParam("EPGItemSynkRetries")))
                noOfAttemptsToSynkItem = int.Parse(managerConfig.GetConfigParam("EPGItemSynkRetries"));
            return noOfAttemptsToSynkItem;
        }

        private void SetAllContentToSynked(List<ContentData> filterList)
        {
            
            foreach (ContentData content in filterList)
            {
                NPVRHelper.SetSynked(content, true);
            }
        }

        private Boolean HasAssets(XmlDocument catchupdoc)
        {
            XmlNodeList assetnodes = catchupdoc.SelectNodes("catchup-event/contents/content");

            return (assetnodes.Count > 0);
        }

        private void PrintQueueToSeparateLogs(String logName, String toLog)
        {
            try
            {
                var systemConfig =
                    Config.GetConfig()
                        .SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager")
                        .SingleOrDefault();
                if (systemConfig.ConfigParams.ContainsKey("ExtraSynkEpgTaskLogging"))
                {
                    String dateString = DateTime.UtcNow.ToString("yyyyMMdd_HH");
                    logName += dateString;
                    String folderPath = systemConfig.GetConfigParam("ExtraSynkEpgTaskLogging");

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
                log.Error("Error printing to GenerateNPVRTask Extra logging", exc);
            }
        }
    }



}




