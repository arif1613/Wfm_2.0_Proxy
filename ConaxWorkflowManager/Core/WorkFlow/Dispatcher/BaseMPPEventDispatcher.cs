using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.System;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Dispatcher
{
    public abstract class BaseMPPEventDispatcher
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IDBWrapper DBWrapper;
        public TaskConfig taskConfig;

        public BaseMPPEventDispatcher(IDBWrapper dbWrapper, TaskConfig taskConfig)
        {
            this.DBWrapper = dbWrapper;
            this.taskConfig = taskConfig;
        }

        public abstract void DispatchMPPEvent();

        protected virtual void UpdateResult(RequestResult result, WorkFlowJob workFlowJob)
        {
            if (result.State == RequestResultState.Successful)
                UpdateEventState(workFlowJob, WorkFlowJobState.Processed);
            else if (result.State == RequestResultState.Failed ||
                     result.State == RequestResultState.Exception)
                UpdateEventState(workFlowJob, WorkFlowJobState.Failed);
            else if (result.State == RequestResultState.Revoke)
                UpdateEventState(workFlowJob, WorkFlowJobState.UnProcessed);
        }

        protected virtual void UpdateEventState(WorkFlowJob workFlowJob, WorkFlowJobState sate)
        {
            workFlowJob.State = sate;
            workFlowJob.LastModified = DateTime.UtcNow;
            DBWrapper.UpdateWorkFlowJob(workFlowJob);
            //DBWrapper.UpdateMPPStationServerEvent(workFlowJob);
        }

        protected virtual void HandleProcessWorkFlow(WorkFlowType action, List<WorkFlowProcess> workFlowProcesses, WorkFlowJob workFlowJob)
        {
            try
            {
                //UpdateEventState(workFlowJob, WorkFlowJobState.Processing);
                // Add historical workFlowProcesses
                List<WorkFlowProcess> historicalWP = DBWrapper.GetWorkFlowProcessesByWorkFlowJobId(workFlowJob.Id.Value);
                workFlowProcesses.AddRange(historicalWP);
                RequestResult result = ProcessWorkFlow(action, workFlowProcesses, workFlowJob);

                if (result.State != RequestResultState.Ignored)
                    log.Debug("RequestResultState " + result.State.ToString("G") + " " + result.Message);
                UpdateResult(result, workFlowJob);
                if (result.State == RequestResultState.Successful ||
                    result.State == RequestResultState.Revoke ||
                    result.State == RequestResultState.Ignored) // success processed| revoked process , remove workflow data
                    DBWrapper.DeleteWorkFlowProcessesByWorkFlowJobId(workFlowJob.Id.Value);
            }
            catch (Exception ex)
            {
                log.Error("Failed to execute " + action.ToString("G") + " action :"+ ex.Message);
                UpdateResult(new RequestResult(RequestResultState.Exception, ex), workFlowJob);
            }
        }

        protected virtual RequestResult ProcessWorkFlow(WorkFlowType action, IList<WorkFlowProcess> workFlowProcesses, WorkFlowJob workFlowJob)
        {
            if (!ShouldExectuteTask(action) && action != WorkFlowType.NoAction)
                return new RequestResult(RequestResultState.Ignored, action.ToString("G") + " Skipped by this task, retrying next task");
            UpdateEventState(workFlowJob, WorkFlowJobState.Processing);

            log.Debug("ProcessWorkFlow, tasks to run are, " + taskConfig.GetConfigParam("EventToProcess") + " action is " + action.ToString("G"));

            
           // log.Info("Processing " + action.ToString("G") + ", identifier: "  + taskConfig.GetConfigParam("EventToProcess"));

            RequestParameters requestParameters = new RequestParameters();
            requestParameters.Action = action;
            requestParameters.Config = this.taskConfig;
            requestParameters.HistoricalWorkFlowProcesses = workFlowProcesses;

            switch (action)
            {
                case WorkFlowType.AddVODContent:                    
                    AddVODContentFlow addVODContentFlow = new AddVODContentFlow();
                    return addVODContentFlow.Process(requestParameters);
                case WorkFlowType.UpdateVODContent:
                    UpdateVODContentFlow updaetVODContentFlow = new UpdateVODContentFlow();
                    return updaetVODContentFlow.Process(requestParameters);
                case WorkFlowType.DeleteVODContent:
                    DeleteVODContentFlow deleteVODContentFlow = new DeleteVODContentFlow();
                    return deleteVODContentFlow.Process(requestParameters);
                case WorkFlowType.PublishVODContent:                    
                    PublishVODContentStandardFlow publishVODContentFlow = new PublishVODContentStandardFlow();
                    return publishVODContentFlow.Process(requestParameters);
                case WorkFlowType.PublishVODContentToSeaChange:
                    PublishVODContentSeaChangeFlow publishVODContentSeaChangeFlow = new PublishVODContentSeaChangeFlow();
                    return publishVODContentSeaChangeFlow.Process(requestParameters);
                case WorkFlowType.UpdatePublishedVODContent:
                    UpdatePublishedVODContentStandardFlow updatePublishedVODContentStandardFlow = new UpdatePublishedVODContentStandardFlow();
                    return updatePublishedVODContentStandardFlow.Process(requestParameters);
                case WorkFlowType.AddChannelContent:
                    AddChannelContentFlow addChannelContentFlow = new AddChannelContentFlow();
                    return addChannelContentFlow.Process(requestParameters);
                case WorkFlowType.UpdateChannelContent:
                    UpdateChannelContentFlow updateChannelContentFlow = new UpdateChannelContentFlow();
                    return updateChannelContentFlow.Process(requestParameters);
                case WorkFlowType.DeleteChannelContent:
                    //DeleteLiveContentFlow deleteLiveContentFlow = new DeleteLiveContentFlow(this.taskConfig);
                    //return deleteLiveContentFlow.Process(requestParameters);
                    // TODO: NOT YET SUPPORTED
                    return new RequestResult(RequestResultState.Failed);
                case WorkFlowType.PublishChannelContent:
                    PublishChannelContentStandardFlow publishChannelContentStandardFlow = new PublishChannelContentStandardFlow();
                    return publishChannelContentStandardFlow.Process(requestParameters);
                case WorkFlowType.AddCatchUpContent:
                    //AddCatchUpContentFlow addCatchUpContentFlow = new AddCatchUpContentFlow(this.taskConfig);
                    //return addCatchUpContentFlow.Process(workFlowProcesses);
                    // Catchup ingest will be bulk ingest via EPG ignest eask, it doesn't follow the standard process.
                    return new RequestResult(RequestResultState.Failed);
                case WorkFlowType.UpdateServicePrice:
                    UpdatePriceFlow updatePriceFlow = new UpdatePriceFlow();
                    return updatePriceFlow.Process(requestParameters);
                case WorkFlowType.UpdatePublishedServicePrice:
                    UpdatePublishedServicePriceStandardFlow updatePublishedServicePriceStandardFlow = new UpdatePublishedServicePriceStandardFlow();
                    return updatePublishedServicePriceStandardFlow.Process(requestParameters);
                case WorkFlowType.NoAction:
                    return new RequestResult(RequestResultState.Successful);
                default:
                    var workFlowProcess = workFlowProcesses.FirstOrDefault();
                    String objectId = "";
                    if (workFlowProcess != null) {
                        try {
                            objectId = workFlowProcess.WorkFlowParameters.Content.ObjectID.Value.ToString();
                        } catch(Exception ex) {}
                    }
                    throw new NotImplementedException("Work flow " + action.ToString("G") + " is not implemented. failed for conetnt objectid " + objectId);
            }
        }

        public bool ShouldExectuteTask(WorkFlowType action)
        {
            if (taskConfig.IsIgnoredWorkFlow(action))
            {
                //log.Info(action.ToString("G") + " was set to ignore for this task! skipping, identifier:" + taskConfig.GetId());
                return false;
            }
            if (!taskConfig.ShouldProcessWorkflow(action))
            {
                //log.Info(action.ToString("G") + " should not be processed by this task! skipping, identifier: " + taskConfig.GetId());
                return false;
                // return new RequestResult(RequestResultState.Revoke, "Skipped by this task, retrying next task");
            }
            return true;
        }

        protected virtual List<WorkFlowJob> GetEventsForProcess()
        {
            String eventToProcess = "";
            try
            {
                eventToProcess = taskConfig.GetConfigParam("EventToProcess");
            } catch (Exception ex) {
            }
            log.Debug("EventToProcess " + eventToProcess);
            List<EventType> eventTypes = new List<EventType>();
            if (!String.IsNullOrEmpty(eventToProcess)) {
                foreach (String eventType in eventToProcess.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                {
                    EventType et = (EventType)Enum.Parse(typeof(EventType), eventType, true);
                    eventTypes.Add(et);
                }
            }

            //var systemConfig = (MPPConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.MPP).SingleOrDefault();
            List<WorkFlowJob> unProcessedEvents = DBWrapper.GetWorkFlowJobs(WorkFlowJobState.UnProcessed, eventTypes);
            List<WorkFlowJob> processingEvents = DBWrapper.GetWorkFlowJobs(WorkFlowJobState.Processing, eventTypes);

            List<WorkFlowJob> unProcEvents = new List<WorkFlowJob>();
            unProcEvents.AddRange(processingEvents);
            unProcEvents.AddRange(unProcessedEvents);
            // Ascendent sort by object id, proccess lowest Created datetime  first.
            unProcEvents.Sort(delegate(WorkFlowJob a1, WorkFlowJob a2) { return a1.Created.CompareTo(a2.Created); });

            return unProcEvents;
        }
    }
}
