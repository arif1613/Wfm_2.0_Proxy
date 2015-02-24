using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using log4net.Core;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.Archive;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class GenerateNPVRTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected MPPIntegrationServicesWrapper MppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
        private readonly List<EPG> _contentsWaitingForProcess = new List<EPG>();
        private readonly List<EPG> _contentsInArchiving = new List<EPG>();
        private readonly List<EPG> _contentsWaitingForUpdateRecordings = new List<EPG>();
        private List<ContentData> _contentsInUpdateRecordings = new List<ContentData>();
        static readonly List<EPG> FailedRecordedEpgs = new List<EPG>();

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");
            Console.WriteLine("Generate nPVR task started.....");

            var tplTasks = new List<System.Threading.Tasks.Task<TPLTaskResult>>();

            // Task index 0, dedicated for fetch epg and npvr recordings
            System.Threading.Tasks.Task<TPLTaskResult> fetchNewEPGWithRecordingTask = addFetchNewEPGWithRecordingTask(new List<UInt64>());
            tplTasks.Add(fetchNewEPGWithRecordingTask);

            // Task index 1, dedicated for update npvr_recording 
            System.Threading.Tasks.Task<TPLTaskResult> updateNPVRRecordingTask = addUpdateNPVRRecordingTask(new NPVRRecordingUpdateList());
            tplTasks.Add(updateNPVRRecordingTask);

            // process tasks
            while (tplTasks.Count > 0)
            {
                Int32 taskIndex = System.Threading.Tasks.Task.WaitAny(tplTasks.ToArray());

                // this WFM is no longer master, exist this task.
                if (!this.Scheduler.IsMaster)
                {
                    log.Debug("This WFM is no longer Master, Exit this task");
                    return;
                }

                try
                {
                    var res = tplTasks[taskIndex].Result;
                    // if index 0 hanndele new epg and npvr recording result
                    if (res is FetchNewEPGWithRecordingTPLTaskResult)
                    {
                        _contentsWaitingForProcess.AddRange(((FetchNewEPGWithRecordingTPLTaskResult)res).EPGs);
                        LogQeueSize();
                        PrintQueuesToExtraLogs();
                    }

                    // if index 1 handle update npvr recording result
                    if (res is UpdateNPVRRecordingTPLTaskResult)
                    {
                        // reset list
                        _contentsInUpdateRecordings = new List<ContentData>();
                    }

                    // if index > 1 handle archived asset result                 
                    if (res is ArchiveAssetTPLTaskResult)
                    {
                        EPG epg = ((ArchiveAssetTPLTaskResult)res).EPG;
                        var epgToRemove = _contentsInArchiving.First(e => e.Content.ID == epg.Content.ID);
                        _contentsInArchiving.Remove(epgToRemove);

                        foreach (var e in FailedRecordedEpgs)
                        {
                            if (((ArchiveAssetTPLTaskResult) res).IsArchived)
                            {
                                _contentsWaitingForUpdateRecordings.Add(epg);
                                PrintLogToLog4NetWithThreadContextData("Content " + epg.Content.Name + " with id " +
                                    epg.Content.ID + ", externalId= " + epg.Content.ExternalID + " is Archived", epg.Content);
                                FailedRecordedEpgs.Remove(e);
                            }
                        }
                        if (((ArchiveAssetTPLTaskResult)res).IsArchived)
                        {
                            _contentsWaitingForUpdateRecordings.Add(epg);
                            PrintLogToLog4NetWithThreadContextData("Content " + epg.Content.Name + " with id " +
                                epg.Content.ID + ", externalId= " + epg.Content.ExternalID + " is Archived", epg.Content);
                        }
                        else
                        {
                            FailedRecordedEpgs.Add(epg);
                            PrintLogToLog4NetWithThreadContextData("Content " + epg.Content.Name + " with id " +
                                epg.Content.ID + ", externalId= " + epg.Content.ExternalID +
                                " is not Archived as recording failed. It will rerun in entire process. ", epg.Content);

                        }
                    }
                }
                catch (AggregateException aex)
                {
                    aex = aex.Flatten();
                    if (taskIndex == 0)
                    {
                        log.Error("Failed to get new EPG and npvr recordings due to");
                    }
                    else if (taskIndex == 1)
                    {
                        // reset list
                        _contentsInUpdateRecordings = new List<ContentData>();
                        log.Error("Failed to update npvr recordings due to");
                    }
                    else
                    {
                        log.Error("Failed to archive due to");
                    }
                    foreach (Exception ex in aex.InnerExceptions)
                    {
                        log.Error(ex.Message, ex);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Problem handle Task result  " + ex.Message, ex);
                }


                tplTasks.RemoveAt(taskIndex);


                // add more task                
                if (taskIndex == 0)
                {
                    // add new fetchnew epg task
                    List<UInt64> contentInProcess = new List<UInt64>();
                    contentInProcess.AddRange(_contentsWaitingForProcess.Select(c => c.Content.ID.Value).Distinct().ToList());
                    contentInProcess.AddRange(_contentsInArchiving.Select(c => c.Content.ID.Value).Distinct().ToList());
                    contentInProcess.AddRange(_contentsWaitingForUpdateRecordings.Select(c => c.Content.ID.Value).Distinct().ToList());
                    // we need to loop though this one, since same content might exist in the ContentsWaitingForUpdateRecorings, since recording might need to slipt up in several chunks due to update limit.
                    foreach (ContentData content in _contentsInUpdateRecordings)
                    {

                        if (!contentInProcess.Contains(content.ID.Value))
                        {
                            contentInProcess.Add(content.ID.Value);
                        }
                    }
                    fetchNewEPGWithRecordingTask = addFetchNewEPGWithRecordingTask(contentInProcess);
                    tplTasks.Insert(0, fetchNewEPGWithRecordingTask);
                }

                if (taskIndex == 1)
                {   // add new update npvr recording task
                    NPVRRecordingUpdateList recordingsToUpdateList = GetChunkOfEPGForRecordingUpdate(_contentsWaitingForUpdateRecordings);
                    _contentsInUpdateRecordings = recordingsToUpdateList.Contents;
                    //ContentsInUpdateRecorings
                    updateNPVRRecordingTask = addUpdateNPVRRecordingTask(recordingsToUpdateList);
                    tplTasks.Insert(1, updateNPVRRecordingTask);
                }

                //while (((ContentsInArchiving.Count + failedRecordedEpgs.Count) <= Config.GetConaxWorkflowManagerConfig().MAXArchiveThreads))
                while ((_contentsInArchiving.Count < Config.GetConaxWorkflowManagerConfig().MAXArchiveThreads) &&
                        _contentsWaitingForProcess.Count > 0)
                {
                    // add new archvie asset task
                    List<EPG> epgs = new List<EPG>();
                    EPG epg = _contentsWaitingForProcess[0];
                    //adding failed recorded epg for archiving too
                    if (!CheckIfEpgAlreadyInFailedRecordedQueue(epg))
                    {
                        _contentsInArchiving.Add(epg);
                    }
                    _contentsWaitingForProcess.RemoveAt(0);

                    foreach (var e in FailedRecordedEpgs)
                    {
                        System.Threading.Tasks.Task<TPLTaskResult> archiveAssetTask = addArchiveAssetTask(e);
                        tplTasks.Add(archiveAssetTask);
                    }
                    System.Threading.Tasks.Task<TPLTaskResult> archiveAssetTask1 = addArchiveAssetTask(epg);
                    tplTasks.Add(archiveAssetTask1);

                }
            }
            Console.WriteLine("doexecute ends");
            log.Debug("DoExecute End");
        }

        private bool CheckIfEpgAlreadyInFailedRecordedQueue(EPG epg)
        {
            bool b = false;
            List<EPG> listEPG_in_Failed_RecordingQueue = FailedRecordedEpgs.ToList();
            foreach (var e in listEPG_in_Failed_RecordingQueue)
            {
                if (e.Content.ID != null && (epg.Content.ID != null && e.Content.ID.Value==epg.Content.ID.Value))
                {
                    b=true;
                }
            }
            return b;
        }

        private void LogQeueSize()
        {

            var wfmConfig = Config.GetConaxWorkflowManagerConfig();
            if (_contentsWaitingForProcess.Count > 0 ||
                _contentsInArchiving.Count > 0 ||
                _contentsWaitingForUpdateRecordings.Count > 0 ||
                _contentsInUpdateRecordings.Count > 0)
            {


                if (_contentsWaitingForProcess.Count > wfmConfig.WarningThresholdForNumberOfEPGInWaitList)
                    log.Error(_contentsWaitingForProcess.Count + " contents is waiting to be processed. Exceeding warning threshold something might not be in order.");
                else
                    log.Debug(_contentsWaitingForProcess.Count + " contents is waiting to be processed.");
                log.Debug(_contentsInArchiving.Count + " contents is archving assets.");
                log.Debug(_contentsWaitingForUpdateRecordings.Count + " contents is waiting to update NPVR_Recordings.");
                log.Debug(_contentsInUpdateRecordings.Count + " contents is updating the NPVR_recordings.");
            }
        }

        private void PrintQueuesToExtraLogs()
        {
            String dateString = DateTime.UtcNow.ToString("yyyyMMdd_HH");
            PrintQueueToSeparateLogs(dateString, "ContentsWaitingForProcessList", _contentsWaitingForProcess);
            PrintQueueToSeparateLogs(dateString, "ContentsInArchivingList", _contentsInArchiving);
            PrintQueueToSeparateLogs(dateString, "ContentsWaitingForUpdateRecordingsList", _contentsWaitingForUpdateRecordings);
            PrintQueueToSeparateLogs(dateString, "ContentsInUpdateRecordingLists", _contentsInUpdateRecordings);
        }


        /*-----------------------*/


        private System.Threading.Tasks.Task<TPLTaskResult> addFetchNewEPGWithRecordingTask(List<UInt64> inqueneNProcess)
        {
            System.Threading.Tasks.Task<TPLTaskResult> fetchNewEPGWithRecordingTask = System.Threading.Tasks.Task<TPLTaskResult>.Factory.StartNew(() =>
            {
                ThreadContext.Properties["TaskName"] = "FetchNewEPGWithRecordingTask";
                String dateString = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var wfmConfig = Config.GetConaxWorkflowManagerConfig();
                Thread.Sleep(wfmConfig.NPVRArchiveTaskFetchContentIntervalInSec * 1000);
                FetchNewEPGWithRecordingTPLTaskResult result = new FetchNewEPGWithRecordingTPLTaskResult();
                List<EPG> newEpgs = fetchNewEpgContent(inqueneNProcess);
                FilterOutEPGWithSuccessfulLastTry(newEpgs);
                log.Debug("Start AddNPVRArchiveTimesToContent");
                AddNPVRArchiveTimesToContent(newEpgs);
                PrintQueueToSeparateLogs(dateString + "_Mpp_Cubiware", "NewEpgsFromMppList", newEpgs);
                log.Debug("Start FetchNPVRRecordings");
                FetchNPVRRecordings(newEpgs);
                log.Debug("Start FilterOutEPGWithNoRecordings");
                FilterOutEPGWithNoRecordings(newEpgs);
                PrintQueueToSeparateLogs(dateString + "_Mpp_Cubiware", "NewEpgsWithRecordingsList", newEpgs);
                result.EPGs = newEpgs;
                return result;

            }, TaskCreationOptions.LongRunning);

            return fetchNewEPGWithRecordingTask;
        }

        private void PrintQueueToSeparateLogs(String dateString, string name, List<ContentData> ContentsInUpdateRecordings)
        {
            List<EPG> epgs = new List<EPG>();
            foreach (ContentData content in ContentsInUpdateRecordings)
            {
                EPG epg = new EPG() { Content = content };
                epgs.Add(epg);
            }
            PrintQueueToSeparateLogs(dateString, name, epgs);
        }

        private void PrintQueueToSeparateLogs(String dateString, String name, List<EPG> newEpgs)
        {
            try
            {
                var systemConfig =
                    Config.GetConfig()
                        .SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager")
                        .SingleOrDefault();
                if (systemConfig.ConfigParams.ContainsKey("ExtraNpvrLogging"))
                {
                    String folderPath = systemConfig.GetConfigParam("ExtraNpvrLogging");

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                    folderPath = Path.Combine(folderPath, dateString + ".log");
                    StreamWriter sw = new StreamWriter(folderPath, true);
                    sw.Write(name + " has " + newEpgs.Count + " number of items in list " + Environment.NewLine);
                    foreach (EPG epg in newEpgs)
                    {
                        //PrintLogToLog4NetWithThreadContextData("Content " + epg.Content.Name + ", id= " + epg.Content.ID + " ExternalID= " + epg.Content.ExternalID + " is in " + name, epg.Content);
                        sw.Write(epg.Content.Name + ": id= " + epg.Content.ID + ":ExternalId= " + epg.Content.ExternalID + Environment.NewLine);
                    }
                    sw.Write("-----------------------------------------------------" + Environment.NewLine);
                    sw.Close();
                }
            }
            catch (Exception exc)
            {
                log.Error("Error printing to GenerateNPVRTask Extra logging", exc);
            }
        }

        private void PrintLogToLog4NetWithThreadContextData(string toLog, ContentData content)
        {
            PrintLogToLog4NetWithThreadContextData(toLog, content, Level.Debug, null);
        }

        private void PrintLogToLog4NetWithThreadContextData(string toLog, ContentData content, Level logLevel)
        {
            PrintLogToLog4NetWithThreadContextData(toLog, content, logLevel, null);
        }

        private void PrintLogToLog4NetWithThreadContextData(string toLog, ContentData content, Level logLevel, Exception exception)
        {
            //ThreadContext.Properties["TaskName"] = "GenerateNPVRTask";
            String externalID = "";
            if (ThreadContext.Properties["ExternalId"] != null)
                externalID = ThreadContext.Properties["ExternalId"].ToString();
            ThreadContext.Properties["ExternalId"] = "ExternalId=" + content.ExternalID + ";";
            String id = "";
            if (ThreadContext.Properties["ContentId"] != null)
                ThreadContext.Properties["ContentId"].ToString();
            ThreadContext.Properties["ContentId"] = "Id=" + content.ID + ";";
            if (logLevel == Level.Debug)
            {
                log.Debug(toLog);
            }
            else if (logLevel == Level.Warn)
            {
                if (exception != null)
                {
                    log.Warn(toLog, exception);
                }
                else
                {
                    log.Warn(toLog);
                }
            }
            else if (logLevel == Level.Error)
            {
                log.Error(toLog, exception);
            }

            if (!String.IsNullOrEmpty(externalID))
            {
                ThreadContext.Properties["ExternalId"] = externalID;
            }
            else
            {
                ThreadContext.Properties.Remove("ExternalId");
            }
            if (!String.IsNullOrEmpty(id))
            {
                ThreadContext.Properties["ContentId"] = id;
            }
            else
            {
                ThreadContext.Properties.Remove("ContentId");
            }

        }

        private System.Threading.Tasks.Task<TPLTaskResult> addUpdateNPVRRecordingTask(NPVRRecordingUpdateList recordingsToUpdateList)
        {
            System.Threading.Tasks.Task<TPLTaskResult> updateNPVRRecordingTask = System.Threading.Tasks.Task<TPLTaskResult>.Factory.StartNew(() =>
            {
                ThreadContext.Properties["TaskName"] = "UpdateNPVRRecordingTask";
                UpdateNPVRRecordingsInCubi(recordingsToUpdateList);
                var wfmConfig = Config.GetConaxWorkflowManagerConfig();
                Thread.Sleep(wfmConfig.SleepBetweenNPVRRecordingBulkUpdateInSec * 1000);
                return new UpdateNPVRRecordingTPLTaskResult();
            }, TaskCreationOptions.LongRunning);

            return updateNPVRRecordingTask;
        }


        /*-----------------------*/

        private System.Threading.Tasks.Task<TPLTaskResult> addArchiveAssetTask(EPG epg)
        {
            System.Threading.Tasks.Task<TPLTaskResult> archiveAssetTask = System.Threading.Tasks.Task<TPLTaskResult>.Factory.StartNew(() =>
            {
                ThreadContext.Properties["TaskName"] = "ArchiveAssetTask";
                ArchiveAssetTPLTaskResult result = new ArchiveAssetTPLTaskResult();
                try
                {
                    log.Debug("start archive task for cotnent " + epg.Content.Name + " " + epg.Content.ID + " " + epg.Content.ExternalID);
                    NPVRAssetLoger.WriteLog(epg.Content, "start archive task");
                    //fixing recording retry times for NPVR
                    int retry_times = Config.GetConaxWorkflowManagerConfig().RequestForContentRecordingsRetries;
                    for (int i = 0; i < retry_times; i++)
                    {
                        result.IsArchived = ArchiveNPVRAssets(epg);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Failed to archive " + epg.Content.ID + " " + epg.Content.ExternalID + " due to " + ex.Message, ex);
                    NPVRAssetLoger.WriteLog(epg.Content, " failed to arcvhie " + ex.Message);
                    result.IsArchived = false;
                    //ContentsWaitingForProcess.Add(epg);
                                List<EPG> EPGs_in_Failed_RecordingQueue = FailedRecordedEpgs.ToList();
                    foreach (var e in EPGs_in_Failed_RecordingQueue)
                    {
                        if (e.Content.ID.Value!=epg.Content.ID.Value)
                        {
                            FailedRecordedEpgs.Add(epg);
                        }   
                    }
                }
                NPVRAssetLoger.WriteLog(epg.Content, "archive task done");
                result.EPG = epg;
                return result;
            }, TaskCreationOptions.LongRunning);

            return archiveAssetTask;
        }

        private void AddNPVRArchiveTimesToContent(List<EPG> newContens)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            List<UpdatePropertiesForContentParameter> updates = new List<UpdatePropertiesForContentParameter>();
            foreach (EPG epg in newContens)
            {
                // add new try time                
                List<KeyValuePair<String, Property>> proeprtiesToUpdate = AddNPVRArchiveTimes(epg.Content);
                if (proeprtiesToUpdate != null)
                {
                    UpdatePropertiesForContentParameter updateParameter = new UpdatePropertiesForContentParameter();
                    updateParameter.Content = epg.Content;
                    updateParameter.Properties = proeprtiesToUpdate;
                    updates.Add(updateParameter);
                }

                List<DateTime> archiveTries = ConaxIntegrationHelper.GetNPVRArchiveTimes(epg.Content);
                log.Debug("Epg content " + epg.Content.Name + " " + epg.Content.ID.Value + " " + epg.Content.ExternalID + " On try " + archiveTries.Count);
                NPVRAssetLoger.WriteLog(epg.Content, epg.Content.Name + " " + epg.Content.ID.Value + " " + epg.Content.ExternalID + " from " + epg.Content.EventPeriodFrom.Value.ToString("yyyy-MM-dd HH:mm:ss") + " to " + epg.Content.EventPeriodTo.Value.ToString("yyyy-MM-dd HH:mm:ss") + " On try " + archiveTries.Count);
            }
            MppWrapper.UpdateContentsPropertiesInChunks(updates);
            stopwatch.Stop();
            log.Debug("AddNPVRArchiveTimesToContent for " + newContens.Count + " content in " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
        }

        private List<EPG> fetchNewEpgContent(List<UInt64> inQueneNProcess)
        {
            var wfmConfig = Config.GetConaxWorkflowManagerConfig();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            log.Debug("Start fetch from MPP.");
            List<ContentData> newContens = GetAllUnprocessedMPVRContents(inQueneNProcess, wfmConfig.RequestForContentRecordingsTimeout);
            stopwatch.Stop();
            log.Debug("fetched " + newContens.Count + " EPG contents from MPP. in " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

            List<EPG> newEpgs = new List<EPG>();
            foreach (ContentData content in newContens)
            {
                // check if content already fetched previously and still in process.
                if (!inQueneNProcess.Contains(content.ID.Value))
                {

                    // check retry time
                    List<DateTime> archiveTries = ConaxIntegrationHelper.GetNPVRArchiveTimes(content);
                    // Check if it's time to execute again
                    if (archiveTries.Count != 0)
                    {
                        DateTime nextRun = archiveTries.Last().AddMinutes(wfmConfig.RequestForContentRecordingsTimeout);
                        if (nextRun >= DateTime.UtcNow)
                            continue;
                    }

                    EPG newEPG = new EPG();
                    newEPG.Content = content;
                    newEpgs.Add(newEPG);
                }
            }
            log.Debug("Ffiltering out content not yet ready to start, " + newEpgs.Count + " EPG contents are ready.");

            return newEpgs;
        }

        private void FilterOutEPGWithSuccessfulLastTry(List<EPG> newEPG)
        {
            Int32 index = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (index < newEPG.Count)
            {
                ContentData content = newEPG[index].Content;
                // check and change the state of the content if it's on last try
                HandleArchiveResult(content);
                // check if it's still ok to go
                var NPVRRecordingsstStateProperty = ConaxIntegrationHelper.GetNPVRRecordingsstState(content);
                if (NPVRRecordingsstStateProperty == null || String.IsNullOrWhiteSpace(NPVRRecordingsstStateProperty.Value))
                {
                    log.Error("Missing NPVRRecordingsstStateProperty value for content " + content.Name + " " + content.ID + " " + content.ExternalID + " skip processing it.");
                    newEPG.RemoveAt(index);
                    continue;
                }

                if (!NPVRRecordingsstStateProperty.Value.Equals(NPVRRecordingsstState.Ongoing.ToString()))
                    newEPG.RemoveAt(index); // remove epg on this index, index remain same to check next one.
                else
                    index++; // move to next index.
            }
            stopwatch.Stop();
            log.Debug(newEPG.Count + " EPG contents left in the fetched content list after filtering out cotnent that has successful last try. in " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

        }

        private void FilterOutEPGWithNoRecordings(List<EPG> newEPG)
        {

            log.Debug("Start FilterOutEPGWithNoRecordings for " + newEPG.Count + " content.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Int32 index = 0;
            while (index < newEPG.Count)
            {
                if (newEPG[index].Recordings.Count == 0)
                {
                    ContentData content = newEPG[index].Content;
                    // if it's at the last attempt, 
                    // update all unknown state to NotArchived
                    if (MaxTriesReached(content))
                    {
                        List<Property> unKnownProeprties = ConaxIntegrationHelper.GetAllNPVRAssetArchiveStateByState(content,
                            NPVRAssetArchiveState.Unknown);
                        foreach (Property unKnownProeprty in unKnownProeprties)
                            unKnownProeprty.Value = NPVRAssetArchiveState.NotArchived.ToString();

                        MppWrapper.UpdateContentProperties(content.ID.Value, unKnownProeprties);
                    }
                    PrintLogToLog4NetWithThreadContextData("No new recordings found for content " + content.ID + " " +
                                content.ExternalID, content);

                    newEPG.RemoveAt(index); // remove epg on this index, index remain same to check next one.
                }
                else
                    index++; // move to next index.
            }

            stopwatch.Stop();
            log.Debug(newEPG.Count + " EPG contents left in the fetched content list after filtering out cotnent that doesn't have any NPVR_Recordings. in " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
        }

        public void FetchNPVRRecordings(List<EPG> newEPG)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // sorting npvr enabled epg per servcie, since we only need get npvr rec for epg that is npvr enabled for that servcie
            log.Debug("Start sorting npvr enabled epg per servcie");
            Dictionary<UInt64, List<EPG>> NPVREnabledForServcies = new Dictionary<UInt64, List<EPG>>();
            Dictionary<String, EPGChannel> channelTable = new Dictionary<String, EPGChannel>();
            foreach (EPG epg in newEPG)
            {
                String channelId = epg.Content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;
                EPGChannel epgChannel = null;
                if (channelTable.ContainsKey(channelId))
                {
                    epgChannel = channelTable[channelId];
                }
                else
                {
                    epgChannel = CatchupHelper.GetEPGChannel(epg.Content);
                    channelTable.Add(channelId, epgChannel);
                }
                if (epg.Content.ContentAgreements == null || epg.Content.ContentAgreements.Count == 0)
                {
                    log.Warn("Can't process content " + epg.Content.Name + " " + epg.Content.ID + " " + epg.Content.ExternalID + " Agreement not found, skiping this content.");
                    continue;
                }
                foreach (MultipleContentService service in epg.Content.ContentAgreements[0].IncludedServices)
                {
                    if (!epgChannel.ServiceEpgConfigs[service.ObjectID.Value].EnableNpvr ||
                        !ConaxIntegrationHelper.IsPublishedToService(service.ObjectID.Value, epgChannel))
                    {   // NPVR is not enable for this servcie/outlet,
                        // or this channel is not yet published to this servcie/outlet, just ignore it and set it as succeded.
                        UpdateLastAttemptState(epg.Content, service.ObjectID.Value, LastAttemptState.Succeeded);
                    }
                    else
                    {
                        // load list with npvr enabled epg
                        if (!NPVREnabledForServcies.ContainsKey(service.ObjectID.Value))
                            NPVREnabledForServcies.Add(service.ObjectID.Value, new List<EPG>());
                        NPVREnabledForServcies[service.ObjectID.Value].Add(epg);
                    }
                }
            }
            stopwatch.Stop();
            log.Debug("sorting npvr enabled epg per servcie in " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

            var stopwatch2 = new Stopwatch();
            stopwatch2.Start();
            // get npvr recordings per sercvie and update last attempt state
            var wfmConfig = Config.GetConaxWorkflowManagerConfig();
            Int32 maxNumOfEPG = wfmConfig.FetchNPVRRecordingsForNumberOfEPGPerCall;
            Int32 PagesToFetchNPVRRecordingsInParallel = wfmConfig.PagesToFetchNPVRRecordingsInParallel;
            // kick off paraellel thread for each servcie
            Parallel.ForEach(NPVREnabledForServcies, kvp =>
            {
                ThreadContext.Properties["TaskName"] = "FetchNewEPGWithRecordingTask";
                var stopwatch3 = new Stopwatch();
                stopwatch3.Start();
                log.Debug("Fetching NPVR_Recordings for " + kvp.Value.Count + " contents from service " + kvp.Key);
                ICubiTVMWServiceWrapper cubiWrapper = CubiTVMiddlewareManager.Instance(kvp.Key);

                //List<String> externalIdsToFetch = new List<String>();
                // get recordings in chunks, since there is a limit of how many epg we can fetch at the samet ime.

                List<List<EPG>> epgChunks = CommonUtil.SplitIntoChunks<EPG>(kvp.Value, maxNumOfEPG);
                //Parallel.ForEach(epgChunks, epgChunk =>
                //{
                foreach (List<EPG> epgChunk in epgChunks)
                {
                    List<String> externalIdsToFetch = new List<String>();
                    foreach (EPG epg in epgChunk)
                    {
                        NPVRAssetLoger.WriteLog(epg.Content, "Fetching npvr_recordings from service " + kvp.Key);
                        externalIdsToFetch.Add(epg.Content.ExternalID);
                    }
                    try
                    {
                        List<NPVRRecording> recordings = cubiWrapper.GetNPVRRecording(externalIdsToFetch, PagesToFetchNPVRRecordingsInParallel);
                        // sort the recordings back to the epg object
                        AddRecordingIntoEPGList(kvp.Key, recordings, kvp.Value, externalIdsToFetch);
                    }
                    catch (Exception exc)
                    {
                        log.Warn("Error fetching recordings from service with id " + kvp.Key, exc);
                        foreach (EPG epg in epgChunk)
                        {
                            // handle state at the last attempt only
                            if (MaxTriesReached(epg.Content))
                                UpdateLastAttemptState(epg.Content, kvp.Key, LastAttemptState.Failed);
                        }
                    }
                }
                //});
                Int32 totalFetchedRecordings = (from e in kvp.Value
                                                select e.Recordings).Count();
                stopwatch3.Stop();
                log.Debug(totalFetchedRecordings + " NPVR_Recordings fetched for " + kvp.Value.Count + " contents from service " + kvp.Key + " in " + stopwatch3.ElapsedMilliseconds.ToString() + "ms");
            });

            stopwatch2.Stop();
            log.Debug("NPVR_Recordings fetched in " + stopwatch2.ElapsedMilliseconds.ToString() + "ms");
        }

        private void FetchNPVRRecordings_OLD(List<EPG> newEPG)
        {

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // sorting npvr enabled epg per servcie, since we only need get npvr rec for epg that is npvr enabled for that servcie
            log.Debug("Start sorting npvr enabled epg per servcie");
            Dictionary<UInt64, List<EPG>> NPVREnabledForServcies = new Dictionary<UInt64, List<EPG>>();
            Dictionary<String, EPGChannel> channelTable = new Dictionary<String, EPGChannel>();
            foreach (EPG epg in newEPG)
            {
                String channelId = epg.Content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;
                EPGChannel epgChannel = null;
                if (channelTable.ContainsKey(channelId))
                {
                    epgChannel = channelTable[channelId];
                }
                else
                {
                    epgChannel = CatchupHelper.GetEPGChannel(epg.Content);
                    channelTable.Add(channelId, epgChannel);
                }
                if (epg.Content.ContentAgreements == null || epg.Content.ContentAgreements.Count == 0)
                {
                    log.Warn("Can't process content " + epg.Content.Name + " " + epg.Content.ID + " " + epg.Content.ExternalID + " Agreement not found, skiping this content.");
                    continue;
                }
                foreach (MultipleContentService service in epg.Content.ContentAgreements[0].IncludedServices)
                {
                    if (!epgChannel.ServiceEpgConfigs[service.ObjectID.Value].EnableNpvr ||
                        !ConaxIntegrationHelper.IsPublishedToService(service.ObjectID.Value, epgChannel))
                    {   // NPVR is not enable for this servcie/outlet,
                        // or this channel is not yet published to this servcie/outlet, just ignore it and set it as succeded.
                        UpdateLastAttemptState(epg.Content, service.ObjectID.Value, LastAttemptState.Succeeded);
                    }
                    else
                    {
                        // load list with npvr enabled epg
                        if (!NPVREnabledForServcies.ContainsKey(service.ObjectID.Value))
                            NPVREnabledForServcies.Add(service.ObjectID.Value, new List<EPG>());
                        NPVREnabledForServcies[service.ObjectID.Value].Add(epg);
                    }
                }
            }
            stopwatch.Stop();
            log.Debug("sorting npvr enabled epg per servcie in " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

            var stopwatch2 = new Stopwatch();
            stopwatch2.Start();
            // get npvr recordings per sercvie and update last attempt state
            var wfmConfig = Config.GetConaxWorkflowManagerConfig();
            Int32 maxNumOfEPG = wfmConfig.FetchNPVRRecordingsForNumberOfEPGPerCall;
            foreach (KeyValuePair<UInt64, List<EPG>> kvp in NPVREnabledForServcies)
            {

                var stopwatch3 = new Stopwatch();
                stopwatch3.Start();
                log.Debug("Fetching NPVR_Recordings for " + kvp.Value.Count + " contents from service " + kvp.Key);
                ICubiTVMWServiceWrapper cubiWrapper = CubiTVMiddlewareManager.Instance(kvp.Key);

                List<String> externalIdsToFetch = new List<String>();
                // get recordings in chunks, since there is a limit of how many epg we can fetch at the samet ime.
                for (Int32 x = 0; x < kvp.Value.Count; )
                {
                    NPVRAssetLoger.WriteLog(kvp.Value[x].Content, "Fetching npvr_recordings from service " + kvp.Key);
                    externalIdsToFetch.Add(kvp.Value[x++].Content.ExternalID);
                    if ((x % maxNumOfEPG) == 0)
                    {
                        try
                        {
                            List<NPVRRecording> recordings = cubiWrapper.GetNPVRRecording(externalIdsToFetch);
                            // sort the recordings back to the epg object
                            AddRecordingIntoEPGList(kvp.Key, recordings, kvp.Value, externalIdsToFetch);
                        }
                        catch (Exception exc)
                        {
                            log.Warn("Error fetching recordings from service with id " + kvp.Key, exc);

                            foreach (String externalId in externalIdsToFetch)
                            {
                                var epg = kvp.Value.First(c => c.Content.ExternalID.Equals(externalId));
                                // handle state at the last attempt only
                                if (MaxTriesReached(epg.Content))
                                    UpdateLastAttemptState(epg.Content, kvp.Key, LastAttemptState.Failed);
                            }
                        }

                        // reset the list
                        externalIdsToFetch = new List<String>();
                    }
                }
                // rest of the list that didn't fit into the last mod
                if (externalIdsToFetch.Count > 0)
                {
                    try
                    {
                        List<NPVRRecording> recordings = cubiWrapper.GetNPVRRecording(externalIdsToFetch);
                        // sort the recordings back tot he epg object
                        AddRecordingIntoEPGList(kvp.Key, recordings, kvp.Value, externalIdsToFetch);
                    }
                    catch (Exception exc)
                    {
                        log.Warn("Error fetching recordings from service with id " + kvp.Key, exc);

                        foreach (String externalId in externalIdsToFetch)
                        {
                            var epg = kvp.Value.First(c => c.Content.ExternalID.Equals(externalId));
                            // handle state at the last attempt only
                            if (MaxTriesReached(epg.Content))
                                UpdateLastAttemptState(epg.Content, kvp.Key, LastAttemptState.Failed);
                        }
                    }
                }
                Int32 totalFetchedRecordings = (from e in kvp.Value
                                                select e.Recordings).Count();
                stopwatch3.Stop();
                log.Debug(totalFetchedRecordings + " NPVR_Recordings fetched for " + kvp.Value.Count + " contents from service " + kvp.Key + " in " + stopwatch3.ElapsedMilliseconds.ToString() + "ms");
            }
            stopwatch2.Stop();
            log.Debug("NPVR_Recordings fetched in " + stopwatch2.ElapsedMilliseconds.ToString() + "ms");
        }

        private void AddRecordingIntoEPGList(UInt64 serviceObjId, List<NPVRRecording> recordings, List<EPG> epgs, List<String> externalIds)
        {
            // sort the recordings back to the epg object
            foreach (String externalId in externalIds)
            {
                var rec = recordings.Where(r => r.EPGExternalID.Equals(externalId)).ToList();
                var epg = epgs.First(c => c.Content.ExternalID.Equals(externalId));
                NPVRAssetLoger.WriteLog(epg.Content, rec.Count + " npvr_recordings found.");
                if (rec.Count > 0)
                {
                    if (!epg.Recordings.ContainsKey(serviceObjId))
                        epg.Recordings.Add(serviceObjId, new List<NPVRRecording>());
                    epg.Recordings[serviceObjId].AddRange(rec);
                }

                // if this is the last try and no recordings form this cubi. set this to succeeded, no longer need to handle
                if (MaxTriesReached(epg.Content) && rec.Count == 0)
                    UpdateLastAttemptState(epg.Content, serviceObjId, LastAttemptState.Succeeded);
            }
        }

        private Boolean ArchiveNPVRAssets(EPG epg)
        {

            ContentData content = epg.Content;
            Dictionary<UInt64, List<NPVRRecording>> allRecordings = epg.Recordings;
            DateTime minStart = DateTime.MaxValue;
            DateTime maxEnd = DateTime.MinValue;
            GetMinStartNMaxEnd(allRecordings, out minStart, out maxEnd);
            if (DateTime.UtcNow < maxEnd)
            {
                PrintLogToLog4NetWithThreadContextData("DateTime.UtcNow: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " is lesser maxEnd:" +
                          maxEnd.ToString("yyyy-MM-dd HH:mm:ss") +
                          ", not ready yet skip archive content " + content.ID + " " + content.ExternalID + " for now.", content);
                NPVRAssetLoger.WriteLog(epg.Content, "not ready yet to archive, post guard it not passed yet. ");
                List<Property> NPVRAssetProperties = ConaxIntegrationHelper.SetALLNPVRAssetArchiveState(content,
                    NPVRAssetArchiveState.Pending);
                MppWrapper.UpdateContentProperties(content.ID.Value, NPVRAssetProperties);

                RemoveFirstNPVRArchiveTime(content);

                return false;
            }
            List<UInt64> servicewithRecordings = new List<UInt64>();
            foreach (KeyValuePair<UInt64, List<NPVRRecording>> kvp in epg.Recordings)
            {
                if (kvp.Value != null && kvp.Value.Count > 0)
                    servicewithRecordings.Add(kvp.Key);
            }
            ArchiveAssets(content, servicewithRecordings, minStart, maxEnd);
            return true;
        }

        private NPVRRecordingUpdateList GetChunkOfEPGForRecordingUpdate(List<EPG> epgs)
        {

            Int32 MaxNumberNPVRRecordingsForUpdate = Config.GetConaxWorkflowManagerConfig().MaxNumberNPVRRecordingsForUpdate;

            NPVRRecordingUpdateList recordingForUpdate = new NPVRRecordingUpdateList();
            // look for all servcies in use in the epg list.
            // and load the dictionary table.
            var serviceObjIds = (from e in epgs
                                 from r in e.Recordings
                                 select r.Key).Distinct();

            foreach (UInt64 serviceObjId in serviceObjIds)
                recordingForUpdate.Recordings.Add(serviceObjId, new List<NPVRRecording>());


            // load recording to the recordingForUpdate table from the epg list
            Int32 epgIndex = 0;
            while (epgs.Count > 0 && epgIndex < epgs.Count)
            {
                Boolean recordingsAddedToUpdateList = false;
                foreach (KeyValuePair<UInt64, List<NPVRRecording>> kvp in epgs[epgIndex].Recordings)
                {

                    UInt64 serviceObjectId = kvp.Key;
                    while (kvp.Value.Count > 0)
                    {
                        if (recordingForUpdate.Recordings[serviceObjectId].Count < MaxNumberNPVRRecordingsForUpdate)
                        {
                            recordingForUpdate.Recordings[serviceObjectId].Add(kvp.Value[0]);
                            kvp.Value.RemoveAt(0);
                            recordingsAddedToUpdateList = true;
                        }
                        else
                            break; // this service already reached the max number.
                    }
                }

                // keep a copy of the cotnetn that need added to  the update list
                if (recordingsAddedToUpdateList)
                    recordingForUpdate.Contents.Add(epgs[epgIndex].Content);

                //check if all recordigns been removed, if so remove from the epg list
                Int32 totalRecordingsRemains = (from r in epgs[epgIndex].Recordings
                                                select r.Value.Count).Sum();
                if (totalRecordingsRemains == 0)
                    epgs.RemoveAt(epgIndex);
                else
                    epgIndex++; // move index to next epg, since we still have reocrdings left for this indexed epg


                // check if all services already at max limit
                Int32 numberOfServiceHasSpaceLeft = (from r in recordingForUpdate.Recordings
                                                     where r.Value.Count < MaxNumberNPVRRecordingsForUpdate
                                                     select r).Count();
                if (numberOfServiceHasSpaceLeft == 0)
                    break; // no servcie left that has space for more recording update.
            }

            return recordingForUpdate;
        }


        /*---------------------*/

        private void UpdateNPVRRecordingsInCubi(NPVRRecordingUpdateList recordingsToUpdateList)
        {
            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            BaseEncoderCatchupHandler smoothHandler = managerConfig.SmoothCatchUpHandler;
            BaseEncoderCatchupHandler hlshHandler = managerConfig.HLSCatchUpHandler;
            // update all recordings in Cubi

            Parallel.ForEach(recordingsToUpdateList.Recordings, kvp =>
            {
                ThreadContext.Properties["TaskName"] = "UpdateNPVRRecordingTask";
                UInt64 serviceObjectId = kvp.Key;
                List<NPVRRecording> allRecordings = kvp.Value;
                log.Debug(allRecordings.Count + " NPVR_Recordings needs to be updated for Service " + serviceObjectId);

                ICubiTVMWServiceWrapper cubiWrapper = CubiTVMiddlewareManager.Instance(serviceObjectId);
                // sort recordings per content
                Dictionary<ContentData, List<NPVRRecording>> recordingPerContent = new Dictionary<ContentData, List<NPVRRecording>>();
                foreach (ContentData content in recordingsToUpdateList.Contents)
                {
                    ContentData c = content;
                    var recordingsPerContent = (from r in allRecordings
                                                where r.EPGExternalID.Equals(c.ExternalID)
                                                select r).ToList();
                    recordingPerContent.Add(c, recordingsPerContent);
                }

                // process recording per content, add uri and state to recordings
                foreach (KeyValuePair<ContentData, List<NPVRRecording>> contentRecordings in recordingPerContent)
                {
                    ContentData content = contentRecordings.Key;
                    List<NPVRRecording> recordings = contentRecordings.Value;
                    PrintLogToLog4NetWithThreadContextData("In UpdateNPVRRecordingsInCubiware for content with name " + content.Name + ", id= " + content.ID + ", it has " + recordings.Count + " recordings!", content);
                    log.Debug("Recordings = " + recordings.Count);

                    EPGChannel epgChannel = CatchupHelper.GetEPGChannel(content);
                    String serviceViewIso = epgChannel.ServiceEpgConfigs[serviceObjectId].ServiceViewLanugageIso;

                    if (ArchivingFailedForAllDevices(content, serviceViewIso))
                    {
                        SetEmptyRecordingUrlsOnSources(epgChannel, recordings, serviceObjectId);
                        //cubiWrapper.UpdateNPVRRecording(content, recordings, NPVRRecordStateInCubiware.failed);
                        continue;
                    }
                    foreach (NPVRRecording recording in recordings)
                    {
                        foreach (SourceConfig sc in epgChannel.ServiceEpgConfigs[serviceObjectId].SourceConfigs)
                        {
                            // Check if asset is archived
                            var NPVRAssetStateProperty = ConaxIntegrationHelper.GetNPVRAssetArchiveState(content,
                                                                                epgChannel.ServiceEpgConfigs[serviceObjectId].ServiceViewLanugageIso, sc.Device);

                            if (NPVRAssetStateProperty == null ||
                                !NPVRAssetStateProperty.Value.Equals(NPVRAssetArchiveState.Archived.ToString()))
                                continue; // not archived skip update in cubi.

                            String assetUrl = "";
                            // TODO: this logic should move out
                            if (hlshHandler is SeaWellHLSCatchupHandler)
                            {// special case for columbus/seawell
                                if (sc.Device == DeviceType.iPad ||
                                    sc.Device == DeviceType.iPhone)
                                    assetUrl = hlshHandler.GetAssetUrl(content, serviceObjectId, epgChannel.ServiceEpgConfigs[serviceObjectId].ServiceViewLanugageIso, sc.Device, recording, epgChannel);
                                else
                                    assetUrl = smoothHandler.GetAssetUrl(content, serviceObjectId, epgChannel.ServiceEpgConfigs[serviceObjectId].ServiceViewLanugageIso, sc.Device, recording, epgChannel);
                            }
                            else
                            {
                                if (CommonUtil.GetAssetFormatTypeFromFileName(sc.Stream) == AssetFormatType.SmoothStreaming)
                                    assetUrl = smoothHandler.GetAssetUrl(content, serviceObjectId, epgChannel.ServiceEpgConfigs[serviceObjectId].ServiceViewLanugageIso, sc.Device, recording, epgChannel);
                                else if (CommonUtil.GetAssetFormatTypeFromFileName(sc.Stream) == AssetFormatType.HTTPLiveStreaming)
                                    assetUrl = hlshHandler.GetAssetUrl(content, serviceObjectId, epgChannel.ServiceEpgConfigs[serviceObjectId].ServiceViewLanugageIso, sc.Device, recording, epgChannel);
                            }

                            NPVRRecordingSource nPVRRecordingSource = new Util.ValueObjects.Catchup.NPVRRecordingSource();
                            nPVRRecordingSource.Device = sc.Device;
                            nPVRRecordingSource.Url = assetUrl;
                            recording.RecordState = NPVRRecordStateInCubiware.completed;
                            recording.Sources.Add(nPVRRecordingSource);
                        }
                    }
                }

                try
                {
                    // bulk update
                    cubiWrapper.UpdateNPVRRecording(recordingPerContent);
                    log.Debug("NPVR_Recordings updated for Service " + serviceObjectId);

                    // udpaet has recording state in mpp
                    foreach (KeyValuePair<ContentData, List<NPVRRecording>> rpcKeyValuePair in recordingPerContent)
                    {
                        // check if any recordings.
                        List<NPVRRecording> recordings = rpcKeyValuePair.Value;
                        Property serviceHasRecordingProperty = null;
                        if (recordings.Count > 0)
                        {
                            // has recordings, mark this servcie in mpp
                            serviceHasRecordingProperty = ConaxIntegrationHelper.SetServiceHasRecordingProperty(serviceObjectId, rpcKeyValuePair.Key, true);
                            try
                            {
                                MppWrapper.UpdateContentProperty(rpcKeyValuePair.Key.ID.Value, serviceHasRecordingProperty);
                                NPVRAssetLoger.WriteLog(rpcKeyValuePair.Key, recordings.Count + " npvr_recordings updated on servcie " + serviceObjectId);
                            }
                            catch (Exception ex)
                            {
                                log.Error("Failed to updated serviceHasRecording property for content " + rpcKeyValuePair.Key.ID.Value + " " + rpcKeyValuePair.Key.ExternalID + " for service " + +serviceObjectId, ex);
                            }
                        }
                    }
                }
                catch (Exception ex)
                { //keep update for next servcie 
                    log.Error("Failed to updated NPVR_Recordings for Service " + serviceObjectId, ex);
                }
            });
        }

        private void HandleArchiveResult(ContentData content)
        {

            // Update states only at the last attempt
            if (MaxTriesReached(content))
            {
                PrintLogToLog4NetWithThreadContextData("Content " + content.Name + ", Id= " + content.ID + ", ExternalID= " + content.ExternalID + " has reached MaxTries", content);

                if (ConaxIntegrationHelper.HasNoFailedAttempts(content))
                {
                    PrintLogToLog4NetWithThreadContextData("Content " + content.Name + ", Id= " + content.ID + ", ExternalID= " + content.ExternalID + " have no failed attempts to call Cubiware, update", content);
                    log.Debug("There is no failed attempts to call Cubiware, update");
                    List<Asset> notArchivedAssets = ConaxIntegrationHelper.GetAllNPVRAssetByState(content,
                        NPVRAssetArchiveState.NotArchived);
                    List<Asset> allNPVRAssets = ConaxIntegrationHelper.GetAllNPVRAsset(content);
                    if (allNPVRAssets.Count == notArchivedAssets.Count)
                    {
                        PrintLogToLog4NetWithThreadContextData("Not all assets are archived for Content " + content.Name + ", Id= " + content.ID + ", ExternalID= " + content.ExternalID, content);
                        // All NPVR assets are not archived, means no user recordings at all.                            
                        var NPVRRecordingsstStateProperty = ConaxIntegrationHelper.GetNPVRRecordingsstState(content);
                        NPVRRecordingsstStateProperty.Value = NPVRRecordingsstState.NoRecordings.ToString("G");
                        MppWrapper.UpdateContentProperty(content.ID.Value, NPVRRecordingsstStateProperty);

                        Property readyToNPVRPurgeProperty =
                            ConaxIntegrationHelper.SetReadyToNPVRPurgeProperty(content, true);
                        MppWrapper.AddContentProperty(content.ID.Value, readyToNPVRPurgeProperty);
                        PrintLogToLog4NetWithThreadContextData("No recordings found for Content " + content.Name + ", Id= " + content.ID + ", ExternalID= " + content.ExternalID + ", this content will be removed by purge task later on.", content);
                        log.Debug("No recordings found for content " + content.Name + " " +
                                    content.ID.Value + " " + content.ExternalID +
                                    " this content will be removed by purge task later on.");
                        NPVRAssetLoger.WriteLog(content, "No npvr_recording, Changing NPVRRecordingsstState to 'NoRecordings' and add set ReadyToNPVRPurge to 'true'.");
                    }
                    else
                    {
                        PrintLogToLog4NetWithThreadContextData("Content " + content.Name + ", Id= " + content.ID + ", ExternalID= " + content.ExternalID + " had some had recordings", content);
                        // not all NPVR asset had not archvied state, some had recordings
                        var NPVRRecordingsstStateProperty2 = ConaxIntegrationHelper.GetNPVRRecordingsstState(content);
                        NPVRRecordingsstStateProperty2.Value = NPVRRecordingsstState.Archived.ToString("G");
                        content.SlimDownAndExtraxtAssetInfoToProperty(); // since this epg will be kept, translate all asset into properteis to save storage.
                        MppWrapper.UpdateContent(content, false);
                        //mppWrapper.UpdateContentProperty(content.ID.Value, NPVRRecordingsstStateProperty2);
                        PrintLogToLog4NetWithThreadContextData("Archive done for content " + content.Name + ", " + content.ID.Value + ", " + content.ExternalID, content);
                        NPVRAssetLoger.WriteLog(content, "Changing NPVRRecordingsstState to 'Archived'.");
                    }
                }
                else
                {
                    PrintLogToLog4NetWithThreadContextData("Some failed attempts to call Cubiware Exists, updating UpdateNPVRArchiveTimes for Content " + content.Name + ", Id= " + content.ID + ", ExternalID= " + content.ExternalID, content);
                    //Property updatedNpVRArchiveTimes = ConaxIntegrationHelper.RemoveFirstNPVRArchiveTime(res.content);
                    //mppWrapper.UpdateContentProperty(res.content.ID.Value, updatedNpVRArchiveTimes);
                    RemoveFirstNPVRArchiveTime(content);
                }
            }
        }

        private List<KeyValuePair<String, Property>> AddNPVRArchiveTimes(ContentData content)
        {
            try
            {
                var NPVRArchiveTimesProeprty = ConaxIntegrationHelper.GetNPVRArchiveTimesProperty(content);
                if (NPVRArchiveTimesProeprty == null)
                {
                    // add new proerpty
                    Property newNPVRArchiveTimesProeprty = ConaxIntegrationHelper.AddNPVRArchiveTimes(content, DateTime.UtcNow);
                    //mppWrapper.AddContentProperty(content.ID.Value, newNPVRArchiveTimesProeprty);
                    List<KeyValuePair<String, Property>> proerptiesToUpdate = new List<KeyValuePair<String, Property>>();
                    proerptiesToUpdate.Add(new KeyValuePair<String, Property>("ADD", newNPVRArchiveTimesProeprty));
                    return proerptiesToUpdate;
                }
                else
                {
                    // delete one proerpty and add the new peroty.
                    // copy old proerty first
                    Property oldNPVRArchiveTimesProeprty = new Property(NPVRArchiveTimesProeprty.Type, NPVRArchiveTimesProeprty.Value);

                    if (MaxTriesReached(content))
                    {
                        // in case for some reason the counter keeps increasing
                        ConaxIntegrationHelper.RemoveFirstNPVRArchiveTime(content);
                    }
                    Property newNPVRArchiveTimesProeprty = ConaxIntegrationHelper.AddNPVRArchiveTimes(content, DateTime.UtcNow);
                    List<KeyValuePair<String, Property>> proerptiesToUpdate = new List<KeyValuePair<String, Property>>();
                    if (String.IsNullOrWhiteSpace(oldNPVRArchiveTimesProeprty.Value))
                    {
                        proerptiesToUpdate.Add(new KeyValuePair<String, Property>("UPDATE", newNPVRArchiveTimesProeprty));
                    }
                    else
                    {
                        proerptiesToUpdate.Add(new KeyValuePair<String, Property>("ADD", newNPVRArchiveTimesProeprty));
                        proerptiesToUpdate.Add(new KeyValuePair<String, Property>("DELETE", oldNPVRArchiveTimesProeprty));
                    }
                    return proerptiesToUpdate;
                    //mppWrapper.UpdateContentProperties(content.ID.Value, proerptiesToUpdate);
                }
            }
            catch (Exception ex)
            {
                log.Warn("Failed to add NPVRArchiveTimes to " + content.Name + " " + content.ID.Value + " " + content.ExternalID);
                return null;
            }
        }

        private void RemoveFirstNPVRArchiveTime(ContentData content)
        {
            try
            {
                var NPVRArchiveTimesProeprty = ConaxIntegrationHelper.GetNPVRArchiveTimesProperty(content);
                // delete one proerpty and add the new peroty.
                // copy old proerty first
                Property oldNPVRArchiveTimesProeprty = new Property(NPVRArchiveTimesProeprty.Type, NPVRArchiveTimesProeprty.Value);
                Property newNPVRArchiveTimesProeprty = ConaxIntegrationHelper.RemoveFirstNPVRArchiveTime(content);

                List<KeyValuePair<String, Property>> proerptiesToUpdate = new List<KeyValuePair<String, Property>>();

                if (String.IsNullOrWhiteSpace(oldNPVRArchiveTimesProeprty.Value))
                {
                    proerptiesToUpdate.Add(new KeyValuePair<String, Property>("UPDATE", newNPVRArchiveTimesProeprty));
                }
                else
                {
                    proerptiesToUpdate.Add(new KeyValuePair<String, Property>("ADD", newNPVRArchiveTimesProeprty));
                    proerptiesToUpdate.Add(new KeyValuePair<String, Property>("DELETE", oldNPVRArchiveTimesProeprty));
                }
                MppWrapper.UpdateContentProperties(content.ID.Value, proerptiesToUpdate);
            }
            catch (Exception ex)
            {
                log.Warn("Failed to RemoveFirstNPVRArchiveTime for " + content.Name + " " + content.ID, ex);
            }
        }

        private void UpdateLastAttemptState(ContentData content, UInt64 serviceObjectId, LastAttemptState state)
        {
            Property lastAttemptStateInServiceProperty = ConaxIntegrationHelper.GetLastAttemptStateInService(content, serviceObjectId);
            // check if this property exist or not, if yes, then it must comes from previous last tries
            if (lastAttemptStateInServiceProperty == null)
            {
                lastAttemptStateInServiceProperty = ConaxIntegrationHelper.SetLastAttemptStateInService(content, serviceObjectId, state);
                MppWrapper.AddContentProperty(content.ID.Value, lastAttemptStateInServiceProperty);
            }
            else
            {
                if (!lastAttemptStateInServiceProperty.Value.Equals(state.ToString()))
                {   // update only if it's a new value.
                    lastAttemptStateInServiceProperty = ConaxIntegrationHelper.SetLastAttemptStateInService(content, serviceObjectId, state);
                    MppWrapper.UpdateContentProperty(content.ID.Value, lastAttemptStateInServiceProperty);
                }
            }
        }

        private void SetEmptyRecordingUrlsOnSources(EPGChannel epgChannel, List<NPVRRecording> recordings, ulong serviceObjectId)
        {
            foreach (NPVRRecording recording in recordings)
            {
                foreach (SourceConfig sc in epgChannel.ServiceEpgConfigs[serviceObjectId].SourceConfigs)
                {
                    NPVRRecordingSource nPvrRecordingSource = new Util.ValueObjects.Catchup.NPVRRecordingSource();
                    nPvrRecordingSource.Device = sc.Device;
                    nPvrRecordingSource.Url = "";
                    recording.RecordState = NPVRRecordStateInCubiware.failed;
                    recording.Sources.Add(nPvrRecordingSource);
                }
            }
        }

        private bool MaxTriesReached(ContentData content)
        {
            List<DateTime> archiveTries = ConaxIntegrationHelper.GetNPVRArchiveTimes(content);
            var configurationManager = Config.GetConaxWorkflowManagerConfig();
            return archiveTries.Count >= configurationManager.RequestForContentRecordingsRetries;
        }

        public bool ArchivingFailedForAllDevices(ContentData content, string serviceViewIso)
        {
            bool ret = false;
            int noOfFailedArchivings = 0;
            List<Asset> assetWithMatchingLanguage = content.Assets.Where(a => a.LanguageISO.Equals(serviceViewIso)).ToList();
            PrintLogToLog4NetWithThreadContextData("Content " + content.Name + ", Id= " + content.ID + ", ExternalID= " + content.ExternalID + " has " + assetWithMatchingLanguage.Count + " assetWithMatchingLanguage", content);

            var deviceTypes = assetWithMatchingLanguage.Select(a => ConaxIntegrationHelper.GetDeviceType(a)).Distinct();
            foreach (Asset asset in assetWithMatchingLanguage)
            {
                DeviceType deviceType = ConaxIntegrationHelper.GetDeviceType(asset);
                Property archiveStateProperty = ConaxIntegrationHelper.GetNPVRAssetArchiveState(content, serviceViewIso,
                    deviceType);
                NPVRAssetArchiveState archiveState = (NPVRAssetArchiveState)Enum.Parse(typeof(NPVRAssetArchiveState),
                    archiveStateProperty.Value);
                if (archiveState == NPVRAssetArchiveState.Failed)
                    noOfFailedArchivings++;
            }
            PrintLogToLog4NetWithThreadContextData("Content " + content.Name + ", Id= " + content.ID + ", ExternalID= " + content.ExternalID + " has " + noOfFailedArchivings + " failed devices", content);
            if (deviceTypes.Count() == noOfFailedArchivings)
                ret = true;
            return ret;

        }

        public static Dictionary<String, List<NPVRRecording>> GroupRecordingsPerGuardTimes(List<NPVRRecording> recordings)
        {
            Dictionary<String, List<NPVRRecording>> ret = new Dictionary<string, List<NPVRRecording>>();
            foreach (NPVRRecording recording in recordings)
            {
                String key = recording.Start.Value.ToString() + ":" + recording.End.Value.ToString();
                if (!ret.ContainsKey(key))
                {
                    List<NPVRRecording> l = new List<NPVRRecording>();
                    l.Add(recording);
                    ret.Add(key, l);
                }
                else
                {
                    ret[key].Add(recording);
                }
            }
            return ret;
        }

        private void ArchiveAssets(ContentData content, List<UInt64> serviceobjectId, DateTime startTime, DateTime endTime)
        {
            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            BaseEncoderCatchupHandler smoothHandler = managerConfig.SmoothCatchUpHandler;
            BaseEncoderCatchupHandler hlshHandler = managerConfig.HLSCatchUpHandler;
            if (hlshHandler is SeaWellHLSCatchupHandler || hlshHandler is NullHLSCatchupHandler)
            {
                SmoothThenHLSArchiveOrder archiveOrder = new SmoothThenHLSArchiveOrder(smoothHandler, hlshHandler);
                archiveOrder.ArchiveAssets(content, serviceobjectId, startTime, endTime);
            }
            else
            {   // add more later
                throw new NotImplementedException("No Archive order implementation is done for " + managerConfig.GetConfigParam("HLSCatchUpHandler"));
            }
        }

        private void GetMinStartNMaxEnd(Dictionary<UInt64, List<NPVRRecording>> allRecordings, out DateTime minStart, out DateTime maxEnd)
        {
            minStart = DateTime.MaxValue;
            maxEnd = DateTime.MinValue;
            foreach (KeyValuePair<UInt64, List<NPVRRecording>> kvp in allRecordings)
            {
                // check in cubi if any recordings.
                List<NPVRRecording> recordings = kvp.Value;
                if (recordings.Count == 0)
                {
                    // no recordings on this cubi.
                    continue;
                }

                DateTime start = recordings.Min(r => r.Start.Value);
                if (minStart > start)
                    minStart = start;
                DateTime end = recordings.Max(r => r.End.Value);
                if (maxEnd < end)
                    maxEnd = end;
            }
        }

        private List<ContentData> GetAllUnprocessedMPVRContents(List<UInt64> contentToIgnore, Int32 retryIntervall)
        {

            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            log.Debug("start GetAllEPGChannels.");
            List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();
            log.Debug("Done GetAllEPGChannels.");
            List<ContentData> allcontents = new List<ContentData>();


            log.Debug("Start loading contents from all channels.");
            foreach (EPGChannel channel in channels)
            {

                DateTime UTCMaxEndtime = DateTime.UtcNow;
                // minus fixed guard time, since we won't have segments untill the time has
                // passed the endtime with fixed post guard, so no need to fetch content untill then.
                UTCMaxEndtime.AddSeconds(-1 * managerConfig.NPVRRecordingPostGuardInSec);

                List<ContentData> contents = GetUnprocessedNPVRContents(UTCMaxEndtime, channel, contentToIgnore, retryIntervall);
                allcontents.AddRange(contents);
            }
            log.Debug("Done loading contents from all channels.");
            return allcontents;
        }

        private List<ContentData> GetUnprocessedNPVRContents(DateTime UTCMaxEndtime, EPGChannel channel, List<UInt64> contentToIgnore, Int32 retryIntervall)
        {
            List<ContentData> contents = new List<ContentData>();

            String xmlStr = "<contentIdsToIgnore>";
            foreach (UInt64 id in contentToIgnore)
                xmlStr += "<id>" + id + "</id>";
            xmlStr += "</contentIdsToIgnore>";

            XmlDocument ContenToIgnore = new XmlDocument();
            ContenToIgnore.LoadXml(xmlStr);

            log.Debug("Start Get EPG for channel " + channel.MppContentId);
            contents = MppWrapper.GetOngoingEpgs(ContenToIgnore, channel.MppContentId, UTCMaxEndtime, retryIntervall);
            log.Debug("Done Get EPG for channel " + channel.MppContentId + ",  " + contents.Count + " content found.");
            log.Debug("Start loading agreements");
            foreach (ContentData content in contents)
            {
                List<ContentAgreement> agreements = MppWrapper.GetAllServicesForContent(content);
                content.ContentAgreements = agreements;
            }
            log.Debug("Done loading agreements");
            return contents;
        }
    }






    internal class ArchiveTaskResult
    {
        public ContentData content { get; set; } // content to process
        public List<ContentData> newContents { get; set; } // fetching new contents from mpp
    }

    internal interface TPLTaskResult
    {
    }

    internal class UpdateNPVRRecordingTPLTaskResult : TPLTaskResult { }

    internal class FetchNewEPGWithRecordingTPLTaskResult : TPLTaskResult
    {
        public FetchNewEPGWithRecordingTPLTaskResult()
        {
            EPGs = new List<EPG>();
        }

        public List<EPG> EPGs { get; set; }
    }

    internal class ArchiveAssetTPLTaskResult : TPLTaskResult
    {
        public EPG EPG { get; set; }
        public Boolean IsArchived { get; set; }
    }

    public class EPG
    {

        public EPG()
        {
            Recordings = new Dictionary<UInt64, List<NPVRRecording>>();
        }
        public ContentData Content { get; set; }
        public Dictionary<UInt64, List<NPVRRecording>> Recordings { get; set; }
    }

    internal class NPVRRecordingUpdateList
    {
        public NPVRRecordingUpdateList()
        {
            Contents = new List<ContentData>();
            Recordings = new Dictionary<ulong, List<NPVRRecording>>();
        }
        public List<ContentData> Contents { get; set; }
        public Dictionary<UInt64, List<NPVRRecording>> Recordings { get; set; }
    }
}
