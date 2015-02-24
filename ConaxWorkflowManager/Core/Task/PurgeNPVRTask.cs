using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Services.Description;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using System.Collections;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class PurgeNPVRTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        BaseEncoderCatchupHandler smoothHandler = null;
        BaseEncoderCatchupHandler hlshHandler = null;
        MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");

            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            smoothHandler = managerConfig.SmoothCatchUpHandler;
            hlshHandler = managerConfig.HLSCatchUpHandler;

            // change how ti fetcg from Cubiware, skickar in en tidstämpel och får ut de epgitems som deletats sen tidsstämpel
            // Get Deleted EPG since last time, Mark these EPG in MPP as ready to Purge.
            DateTime executeTime = DateTime.UtcNow;
            IDBWrapper dbWrapper = DBManager.Instance;
            List<UInt64> services = new List<UInt64>();
            List<EPGChannel> epgChannels = CatchupHelper.GetAllEPGChannels();
            foreach (EPGChannel channel in epgChannels)
            {
                foreach (ulong serviceObjectId in channel.ServiceExtContentIDs.Keys)
                {
                    if (!services.Contains(serviceObjectId))
                        services.Add(serviceObjectId);
                }
            }
            string logName = "PurgeNpvrTask_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmm");
            PrintToSeparateLogs(logName, "Checking " + services.Count + " for ready To Purge items");
            List<ulong> servicesSuccessfullyCalled = new List<ulong>();
            Hashtable cachedContents = new Hashtable();
            foreach (ulong serviceObjectId in services)
            {
                DateTime lastExecuted = DateTime.MinValue;
                List<CubiEPG> cubiEPGs = new List<CubiEPG>();
                List<CubiEPG> epgsToKeep = new List<CubiEPG>();
                try
                {
                    lastExecuted = dbWrapper.LastOccurredDateForTask("PurgeNPVRTask", serviceObjectId);
                    if (lastExecuted == DateTime.MinValue)
                        lastExecuted = new DateTime(2000, 01, 01);
                    lastExecuted = lastExecuted.AddMinutes(-1); // add some margin from last execution.
                    PrintToSeparateLogs(logName, "for service with objectId " + serviceObjectId +", Using lastExecuted= " + lastExecuted.ToString("yyyyMMdd_HHmm"));
                    cubiEPGs = CubiTVMiddlewareManager.Instance(serviceObjectId)
                        .GetEPGsReadyForPurge(lastExecuted, executeTime);
                    foreach (CubiEPG epg in cubiEPGs)
                    {
                        if (String.IsNullOrWhiteSpace(epg.ExternalID))
                        {
                            log.Warn("Cubi EPG " + epg.ID +
                                     " doesn't have External ID, this might not been created by WFM, skip processing it.");
                            PrintToSeparateLogs(logName, "Cubi EPG " + epg.ID +
                                     " doesn't have External ID, this might not been created by WFM, skip processing it.");
                            continue;
                        }
                        ContentData content = null;
                        if (!cachedContents.ContainsKey(epg.ExternalID))
                        {
                            PrintToSeparateLogs(logName, "Epg with ExternalID " + epg.ExternalID +
                                    " added to list");
                            content = mppWrapper.GetContentDataByExternalID(serviceObjectId, epg.ExternalID);
                            if (content != null)
                            {
                                log.Debug("Found EPG " + content.Name + " " + content.ID.Value + " " + epg.ExternalID);
                                PrintToSeparateLogs(logName, "Found EPG " + content.Name + " " + content.ID.Value + " " + epg.ExternalID);
                                //if (
                                //    content.Properties.Exists(
                                //        p =>
                                //            p.Type.Equals(CatchupContentProperties.EpgIsSynked) &&
                                //            p.Value.Equals(bool.FalseString)))
                                //{
                                //    log.Debug("Content externalId " + epg.ExternalID + " is not synked yet, skipping!");
                                //    PrintToSeparateLogs(logName, "Content externalId " + epg.ExternalID + " is not synked yet, skipping!");
                                //    continue;
                                //}

                                //if (
                                //    content.Properties.Exists(
                                //        p =>
                                //            p.Type.Equals(CatchupContentProperties.EpgIsSynked) &&
                                //            p.Value.Equals(bool.FalseString)))
                                //{
                                //    log.Debug("Content externalId " + epg.ExternalID + " is not synked yet, skipping!");
                                //    continue;
                                //}
                                if (content.EventPeriodTo.Value < DateTime.UtcNow)
                                    cachedContents.Add(epg.ExternalID, content);
                                else
                                {
                                    log.Debug("Content externalId " + epg.ExternalID + " is a future event, skipping!");
                                    PrintToSeparateLogs(logName, "Content externalId " + epg.ExternalID + " is a future event, skipping!");
                                    continue;
                                }
                            }
                            else
                            {
                                log.Debug("No content matching externalId " + epg.ExternalID + " was found, skipping!");
                                PrintToSeparateLogs(logName, "No content matching externalId " + epg.ExternalID + " was found, skipping!");
                                continue;
                            }
                        }
                        else
                        {
                            content = cachedContents[epg.ExternalID] as ContentData;
                        }
                        log.Debug("Mark servcie " + serviceObjectId + " for EPG contetn " + content.Name + " " + content.ID.Value + " " + content.ExternalID);
                        PrintToSeparateLogs(logName, "Mark service " + serviceObjectId + " for EPG contetn " + content.Name + " " + content.ID.Value + " " + content.ExternalID);
                        ConaxIntegrationHelper.SetServiceHasRecordingProperty(serviceObjectId, content, false);

                    }
                }
                catch (Exception ex)
                {
                    log.Error("Failed to get EPGs from cubi " + serviceObjectId, ex);
                    PrintToSeparateLogs(logName, "Failed to get EPGs from cubi " + serviceObjectId + ", message= " + ex.Message);
                    continue;
                }
                servicesSuccessfullyCalled.Add(serviceObjectId);
                //List<NPVRRecording> reocrdings = CubiTVMiddlewareManager.Instance(serviceObjectId).GetRecordingsDeletedSince(lastExecuted);
                //var externalIds = reocrdings.Select(r => r.EPGExternalID).Distinct();
            }
            try
            {
                Boolean allSuccessfullyUpdated = true;
                log.Debug("EPG content to udpate " + cachedContents.Values.Count);
                foreach (ContentData content in cachedContents.Values)
                {
                    try
                    {
                        if (ConaxIntegrationHelper.ServiceHasRecordingPropertyStateCount(content, true) == 0) // it has not been recorded yet but it might still be someone to decide to record later so ignore until startdate has passed
                        {
                            log.Debug("Content " + content.Name + " with id " + content.ID + " have no recordings in any service, deleting");
                            PrintToSeparateLogs(logName, "Content " + content.Name + " with id " + content.ID + " have no recordings in any service, deleting");
                            Property readyToNPVRPurgeProperty = ConaxIntegrationHelper.SetReadyToNPVRPurgeProperty(content, true);
                        }
                        log.Debug("Update Content " + content.Name + " " + content.ID + " " + content.ExternalID);

                        PrintToSeparateLogs(logName, "ReadyToPurge Update Content" + content.Name + " " + content.ID + " " + content.ExternalID);

                        content.Assets.Clear();// quick fix, since assets already stored in the proerpties, these has to be removed so it doesn't creaets again.

                        mppWrapper.UpdateContent(content, false);

                    }
                    catch (Exception ex)
                    {
                        log.Error("Failed to check service for content with External ID " + content.ExternalID, ex);
                        PrintToSeparateLogs(logName, "Failed to check service for content with External ID " + content.ExternalID + " message= " + ex.Message);
                        allSuccessfullyUpdated = false;
                    }
                }

                if (allSuccessfullyUpdated)
                {   // Upate timestamp if all epg items handled Successfully
                    log.Debug("all EPG items Successfully Updated, update timestamp " + executeTime.ToString("yyyyMMdd HH:mm:ss"));
                    PrintToSeparateLogs(logName, "all EPG items Successfully Updated, update timestamp " + executeTime.ToString("yyyyMMdd HH:mm:ss"));
                    foreach (ulong serviceObjectId in servicesSuccessfullyCalled)
                    {
                        Int32 res = dbWrapper.UpdateOccuredDateForTask("PurgeNPVRTask", serviceObjectId, executeTime);
                        if (res == 0)
                            dbWrapper.AddOccuredDateForTask("PurgeNPVRTask", serviceObjectId, executeTime);
                    }
                }
                else
                {
                    log.Debug("some EPG items was not Updated Successfully, not udateing timestamp to " + executeTime.ToString("yyyyMMdd HH:mm:ss"));
                    PrintToSeparateLogs(logName, "some EPG items was not Updated Successfully, not udateing timestamp to " + executeTime.ToString("yyyyMMdd HH:mm:ss"));
                }
            }
            catch (Exception ex)
            {
                log.Error("Failed to process EPG from Cubi ", ex);
                PrintToSeparateLogs(logName, "Failed to process EPG from Cubi message= " + ex.Message);
            }


            // Fetch All EPG ready to purge from MPP, Purge them.
            List<ContentData> contents = GetNPVRPurgeReadyContents();        
            log.Debug(contents.Count + " EPG content ready for purge.");
            PrintToSeparateLogs(logName, contents.Count + " EPG content ready for purge.");
            foreach (ContentData content in contents)
            {
                log.Debug("Start purge EPG content " + content.Name + " " + content.ID.Value + " " + content.ExternalID);
                PrintToSeparateLogs(logName, "Start purge EPG content " + content.Name + " " + content.ID.Value + " " + content.ExternalID);
                if (
                    content.Properties.Exists(
                        p =>
                            p.Type.Equals(CatchupContentProperties.EpgIsSynked) &&
                            p.Value.Equals(bool.FalseString)))
                {
                    log.Debug("Content externalId " + content.ExternalID + " is not synked yet, skipping!");
                    continue;
                }

                if (!ConaxIntegrationHelper.HasNoFailedAttempts(content))
                {
                    log.Debug("Content " + content.Name + " with id " + content.ID + " has not been fully checked in GenerateNPVRTask, skipping");
                    PrintToSeparateLogs(logName, "Content " + content.Name + " with id " + content.ID + " has not been fully checked in GenerateNPVRTask, skipping");
                    continue;
                }

                // it is possible that it now have recordings in a service that was previously down so need to check again
                if (ConaxIntegrationHelper.ServiceHasRecordingPropertyStateCount(content, true) > 0)
                {
                    log.Debug("Content now have recordings in a service, setting readyToPurge to false");
                    PrintToSeparateLogs(logName, "Content now have recordings in a service, setting readyToPurge to false");
                    Property readyToNPVRPurgeProperty = ConaxIntegrationHelper.SetReadyToNPVRPurgeProperty(content, false);
                    mppWrapper.UpdateContentProperty(content.ID.Value, readyToNPVRPurgeProperty);
                    continue;
                }

                Boolean failToDeleteAsset = false;
                try
                {
                    var enableCatchUpProperty = content.Properties.First(p => p.Type.Equals(CatchupContentProperties.EnableCatchUp));
                    // Delete Assets
                    // Get smooth assets to delete
                    List<Asset> ArchivedNPVRAssets = ConaxIntegrationHelper.GetAllNPVRAssetByState(content, NPVRAssetArchiveState.Archived);

                    var archivedNPRSmoothAssets = ArchivedNPVRAssets.Where(a => a.Properties.Count(p => p.Type.Equals(CatchupContentProperties.AssetFormatType) &&
                                                                                                        p.Value.Equals(AssetFormatType.SmoothStreaming.ToString())) > 0);
                    List<Asset> archivedNPRSmoothAssetList = new List<Asset>();
                    archivedNPRSmoothAssetList.AddRange(archivedNPRSmoothAssets);
                    List<String> assetNameDeleted = new List<String>();
                    if (archivedNPRSmoothAssetList.Count > 0)
                    {
                        PrintToSeparateLogs(logName, archivedNPRSmoothAssetList.Count + " smoothAssets to delete");
                        foreach (Asset asset in archivedNPRSmoothAssetList)
                        {

                            if (assetNameDeleted.Contains(asset.Name))
                                continue;// already start detele
                            assetNameDeleted.Add(asset.Name);

                            try
                            {
                                log.Debug("Delete asset " + asset.Name + " for content " + content.Name + " " +
                                          content.ID.Value + " " + content.ExternalID);
                                smoothHandler.DeleteNPVR(content, asset);
                                //UpdateAssetState(content, asset.Name);
                            }
                            catch (WebException wex)
                            {
                                //log.Error("Failed to delete asset for content with asset name " + asset.Name, wex);
                                //PrintToSeparateLogs(logName, "Failed to delete asset for content with asset name " + asset.Name + "error= " + wex.Message);
                                if (wex.Response != null)
                                {
                                    var res = (HttpWebResponse) wex.Response;
                                    if (res.StatusCode != HttpStatusCode.BadRequest &&
                                        res.StatusCode != HttpStatusCode.NotFound)
                                    {
                                        failToDeleteAsset = true;
                                        log.Error("Failed to delete asset for content with asset name " + asset.Name,
                                            wex);
                                        PrintToSeparateLogs(logName,
                                            "Failed to delete asset for content with asset name " + asset.Name +
                                            "error= " + wex.Message);
                                    }
                                    //else
                                    //    UpdateAssetState(content, asset.Name); // 404 is ok.
                                }
                                else
                                {
                                    failToDeleteAsset = true;
                                    log.Error("Failed to delete asset for content with asset name " + asset.Name, wex);
                                    PrintToSeparateLogs(logName,
                                        "Failed to delete asset for content with asset name " + asset.Name + "error= " +
                                        wex.Message);
                                }
                            }
                            catch (Exception exc)
                            {
                                failToDeleteAsset = true;
                                PrintToSeparateLogs(logName,
                                    "Failed to delete asset for content with asset name " + asset.Name + "error= " +
                                    exc.Message);
                            }
                        }
                    }

                    // Get HLS assets to delete
                    var archivedNPRHLSAssets = ArchivedNPVRAssets.Where(a => a.Properties.Count(p => p.Type.Equals(CatchupContentProperties.AssetFormatType) &&
                                                                                                    p.Value.Equals(AssetFormatType.HTTPLiveStreaming.ToString())) > 0);
                    List<Asset> archivedNPRHLSAssetsList = new List<Asset>();
                    archivedNPRHLSAssetsList.AddRange(archivedNPRHLSAssets);
                    assetNameDeleted = new List<String>();
                    if (archivedNPRHLSAssetsList.Count > 0)
                    {
                        PrintToSeparateLogs(logName, archivedNPRHLSAssetsList.Count + " HLS assets to delete");
                        foreach (Asset asset in archivedNPRHLSAssetsList)
                        {
                            if (assetNameDeleted.Contains(asset.Name))
                                continue;// already start detele
                            assetNameDeleted.Add(asset.Name);

                            try
                            {
                                log.Debug("Delete asset " + asset.Name + " for content " + content.Name + " " + content.ID.Value + " " + content.ExternalID);
                                hlshHandler.DeleteNPVR(content, asset);
                                //UpdateAssetState(content, asset.Name);
                            }
                            catch (WebException wex)
                            {
                                //log.Error("Failed to delete asset for content with asset name " + asset.Name, wex);
                                if (wex.Response != null)
                                {
                                    var res = (HttpWebResponse)wex.Response;
                                    if (res.StatusCode != HttpStatusCode.BadRequest && res.StatusCode != HttpStatusCode.NotFound) {
                                        failToDeleteAsset = true;
                                        log.Error("Failed to delete asset for content with asset name " + asset.Name, wex);
                                    }
                                    //else
                                    //    UpdateAssetState(content, asset.Name); // 404 is ok.
                                }
                                else {
                                    failToDeleteAsset = true;
                                    log.Error("Failed to delete asset for content with asset name " + asset.Name, wex);
                                }
                            }
                        }
                    }


                    if (!failToDeleteAsset)
                    {
                        // Delete epg items in MPP
                        // check if content is catchup enabled
                        // if yes disabled npvr, 
                        // if no delete cotent
                        if (Boolean.Parse(enableCatchUpProperty.Value))
                        {
                            var enableNPVRProperty =
                                content.Properties.First(p => p.Type.Equals(CatchupContentProperties.EnableNPVR));
                            enableNPVRProperty.Value = false.ToString();
                            mppWrapper.UpdateContentProperty(content.ID.Value, enableNPVRProperty);
                        }
                        else
                        {
                            mppWrapper.DeleteContent(content);
                            log.Debug("Deleted " + content.Name + " " + content.ID + " " + content.ExternalID +
                                      " from MPP.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Failed to delete asset for content with External ID " + content.ExternalID, ex);
                    continue;
                }

                //}
            }

            log.Debug("DoExecute End");
        }

        private List<CubiEPG> RemoveEpgsNotToDelete(List<CubiEPG> cubiEPGs, List<CubiEPG> epgsToKeep)
        {
            foreach (CubiEPG epg in epgsToKeep)
            {
                cubiEPGs.Remove(epg);
            }
            return cubiEPGs;
        }

        private void UpdateAssetState(ContentData content, String assetName)
        {
            var assetsToUpdate = content.Assets.Where(a => a.Name.Equals(assetName));
            List<Asset> assetsToUpdateList = new List<Asset>();
            assetsToUpdateList.AddRange(assetsToUpdate);
            List<Property> NPVRAssetStateProperties = new List<Property>();

            foreach (Asset asset in assetsToUpdateList)
            {
                DeviceType device = ConaxIntegrationHelper.GetDeviceType(asset);
                Property NPVRAssetStateProperty = ConaxIntegrationHelper.GetNPVRAssetArchiveState(content, asset.LanguageISO, device);
                NPVRAssetStateProperty.Value = NPVRAssetArchiveState.Purged.ToString();
                NPVRAssetStateProperties.Add(NPVRAssetStateProperty);
            }
            mppWrapper.UpdateContentProperties(content.ID.Value, NPVRAssetStateProperties);
        }

        private List<ContentData> GetNPVRPurgeReadyContents()
        {
            List<ContentData> allContens = new List<ContentData>();
            List<EPGChannel> epgChannels = CatchupHelper.GetAllEPGChannels();
            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);

            DateTime epgHistoryDate = DateTime.UtcNow.AddHours(-1 * CommonUtil.GetEpgHistoryTimeInHours());

            //var cros = epgChannels.Select(c => c.ContentRightOwner).Distinct();
            //foreach (String cro in cros)
            //{
            //    ContentSearchParameters csp = new ContentSearchParameters();
            //    csp.ContentRightsOwner = cro;
            //    csp.Properties.Add(CatchupContentProperties.ReadyToNPVRPurge, true.ToString());
            //    csp.Properties.Add(CatchupContentProperties.EpgIsSynked, true.ToString());
            //    csp.MaxReturn = managerConfig.FetchNumberOfReadyToPurgeEPG;
            //    csp.EventPeriodTo = epgHistoryDate;
            //    //List<ContentData> contents = mppWrapper.GetContent(csp, true);
            //    List<ContentData> contents = mppWrapper.GetContentFromProperties(csp, true);
            //    if (contents.Count > 0)
            //        log.Debug(contents.Count + " epg content found for CRO " + cro + " that are ready for purge.");
            //    allContens.AddRange(contents);
            //}
            foreach (EPGChannel channel in epgChannels)
            {
                ContentSearchParameters csp = new ContentSearchParameters();
                csp.ContentRightsOwner = channel.ContentRightOwner;
                //csp.Properties.Add(SearchProperty.S_ChannelIdEpgIsSynked, channel.MppContentId.ToString("G") + ":" + "True");
                csp.Properties.Add(CatchupContentProperties.ReadyToNPVRPurge, true.ToString());
                csp.MaxReturn = managerConfig.FetchNumberOfReadyToPurgeEPG;
                csp.EventPeriodTo = epgHistoryDate;
                List<ContentData> contents = mppWrapper.GetContentFromProperties(csp, true);                
                allContens.AddRange(contents);
            }

            return allContens;
        }

        private void PrintToSeparateLogs(String logName, String toLog)
        {
            try
            {
                var systemConfig =
                    Config.GetConfig()
                        .SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager")
                        .SingleOrDefault();
                if (systemConfig.ConfigParams.ContainsKey("ExtraPurgeNPVRLogging"))
                {
                    String folderPath = systemConfig.GetConfigParam("ExtraPurgeNPVRLogging");

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
    }

    class CubiEPGForService : CubiEPG
    {
        public UInt64 ServiceObjectID { get; set; }
    }
}
