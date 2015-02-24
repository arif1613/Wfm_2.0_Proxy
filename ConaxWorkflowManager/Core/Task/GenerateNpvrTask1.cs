using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    internal class GenerateNpvrTask1 : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
        private List<EPG> ContentsWaitingForProcess = new List<EPG>();
        private List<EPG> ContentsInArchiving = new List<EPG>();
        private List<EPG> ContentsWaitingForUpdateRecordings = new List<EPG>();
        private List<ContentData> ContentsInUpdateRecordings = new List<ContentData>();
        private static List<EPG> failedRecordedEpgs = new List<EPG>();

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
                        ContentsWaitingForProcess.AddRange(((FetchNewEPGWithRecordingTPLTaskResult)res).EPGs);
                        LogQeueSize();
                        PrintQueuesToExtraLogs();
                    }

                    // if index 1 handle update npvr recording result
                    if (res is UpdateNPVRRecordingTPLTaskResult)
                    {
                        // reset list
                        ContentsInUpdateRecordings = new List<ContentData>();
                    }

                    // if index > 1 handle archived asset result                 
                    if (res is ArchiveAssetTPLTaskResult)
                    {
                        EPG epg = ((ArchiveAssetTPLTaskResult)res).EPG;
                        var epgToRemove = ContentsInArchiving.First(e => e.Content.ID == epg.Content.ID);
                        ContentsInArchiving.Remove(epgToRemove);

                        foreach (var e in failedRecordedEpgs)
                        {
                            if (((ArchiveAssetTPLTaskResult)res).IsArchived)
                            {
                                ContentsWaitingForUpdateRecordings.Add(epg);
                                PrintLogToLog4NetWithThreadContextData("Content " + epg.Content.Name + " with id " +
                                    epg.Content.ID + ", externalId= " + epg.Content.ExternalID + " is Archived", epg.Content);
                                failedRecordedEpgs.Remove(e);
                            }
                        }
                        if (((ArchiveAssetTPLTaskResult)res).IsArchived)
                        {
                            ContentsWaitingForUpdateRecordings.Add(epg);
                            PrintLogToLog4NetWithThreadContextData("Content " + epg.Content.Name + " with id " +
                                epg.Content.ID + ", externalId= " + epg.Content.ExternalID + " is Archived", epg.Content);
                        }
                        else
                        {
                            failedRecordedEpgs.Add(epg);
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
                        ContentsInUpdateRecordings = new List<ContentData>();
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
                    contentInProcess.AddRange(ContentsWaitingForProcess.Select(c => c.Content.ID.Value).Distinct().ToList());
                    contentInProcess.AddRange(ContentsInArchiving.Select(c => c.Content.ID.Value).Distinct().ToList());
                    contentInProcess.AddRange(ContentsWaitingForUpdateRecordings.Select(c => c.Content.ID.Value).Distinct().ToList());
                    // we need to loop though this one, since same content might exist in the ContentsWaitingForUpdateRecorings, since recording might need to slipt up in several chunks due to update limit.
                    foreach (ContentData content in ContentsInUpdateRecordings)
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
                    NPVRRecordingUpdateList recordingsToUpdateList = GetChunkOfEPGForRecordingUpdate(ContentsWaitingForUpdateRecordings);
                    ContentsInUpdateRecordings = recordingsToUpdateList.Contents;
                    //ContentsInUpdateRecorings
                    updateNPVRRecordingTask = addUpdateNPVRRecordingTask(recordingsToUpdateList);
                    tplTasks.Insert(1, updateNPVRRecordingTask);
                }

                while (((ContentsInArchiving.Count + failedRecordedEpgs.Count) < Config.GetConaxWorkflowManagerConfig().MAXArchiveThreads))
                {
                    // add new archvie asset task
                    List<EPG> epgs = new List<EPG>();
                    EPG epg = ContentsWaitingForProcess[0];
                    //adding failed recorded epg for archiving too
                    if (!CheckIfEpgAlreadyInFailedRecordedQueue(epg))
                    {
                        ContentsInArchiving.Add(epg);
                    }
                    ContentsWaitingForProcess.RemoveAt(0);

                    foreach (var e in failedRecordedEpgs)
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
    }
}
