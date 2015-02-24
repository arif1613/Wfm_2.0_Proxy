using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;
using System.Collections;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.System;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Dispatcher
{
    public class ConaxDispatcher : BaseMPPEventDispatcher
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public ConaxDispatcher(IDBWrapper dbWrapper, TaskConfig taskConfig)
            : base(dbWrapper, taskConfig)
        {
        }

        /// <summary>
        /// Handles events to process, and update event state in DB
        /// </summary>
        public override void DispatchMPPEvent()
        {

            //var systemConfig = (MPPConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.MPP).SingleOrDefault();
      
            List<WorkFlowJob> unProcEvents = GetEventsForProcess();
            log.Debug(unProcEvents.Count + " WorkFlow jobs to process.");

            //ulong userID = ulong.Parse(systemConfig.GetConfigParam("UserID"));
            UInt64 userID = MPPIntegrationServiceManager.InstanceWithPassiveEvent.User.Id;

            // prepare for proccess flow and execute.
            foreach (WorkFlowJob unProcEvent in unProcEvents)
            {
                try
                {
                    switch (unProcEvent.Type)
                    {
                        case EventType.ContentCreated:
                            //((MPPStationServerEvent)unProcEvent.Message).RelatedPersistentObjectId = 2712062977;
                            //log.Debug("Start process ContentCreated event - RelatedPersistentObjectId:" + ((MPPStationServerEvent) unProcEvent.Message).RelatedPersistentObjectId);
                            HandleContentCreated(unProcEvent);
                            break;
                        case EventType.ContentUpdated:
                            if (((MPPStationServerEvent)unProcEvent.Message).UserId == 0 || ((MPPStationServerEvent)unProcEvent.Message).UserId == userID)
                            {
                                // this event is not triggered within MPP, might be triggered by this Conax manager. set it to ignored and skip.
                              //  log.Debug("ContentUpdated event with RelatedPersistentObjectId " + ((MPPStationServerEvent)unProcEvent.Message).RelatedPersistentObjectId.ToString() + " and user ID " + ((MPPStationServerEvent)unProcEvent.Message).UserId.ToString() + ". Will ignore this event.");
                                UpdateEventState(unProcEvent, WorkFlowJobState.Ignored);
                            }
                            else
                            {
                                //log.Debug("Start process ContentUpdated event");
                                HandleContentUpdated(unProcEvent);
                            }
                            break;
                        case EventType.MultipleServicePriceUpdated:
                            if (((MPPStationServerEvent)unProcEvent.Message).UserId == 0 || ((MPPStationServerEvent)unProcEvent.Message).UserId == userID)
                            {
                                // this event is not trigger within MPP, might be triggered by this Conax manager. set it to ignored and skip.
                                //log.Debug("MultipleContentServiceUpdated event with RelatedPersistentObjectId " + ((MPPStationServerEvent)unProcEvent.Message).RelatedPersistentObjectId.ToString() + " and user ID " + ((MPPStationServerEvent)unProcEvent.Message).UserId + ". Will ignore this event.");
                                UpdateEventState(unProcEvent, WorkFlowJobState.Ignored);
                            }
                            else
                            {
                                //log.Debug("Start process MultipleContentServiceUpdated event");
                                HandleMultipleServicePriceUpdated(unProcEvent);
                            }
                            break;
                        case EventType.PublishedMultipleServicePriceUpdated:
                            //log.Debug("Start process PublishedMultipleServicePriceUpdated event");
                            HandlePublishedPriceUpdated(unProcEvent);
                            break;
                        case EventType.ContentPublished:
                            //log.Debug("Start process ContentPublished event");
                            HandleContentPublished(unProcEvent);
                            break;
                        default:
                            // set state to ignore, this state is not handled
                            log.Debug(unProcEvent.Type.ToString("G") + " is not handled. set to Ignore state.");
                            UpdateEventState(unProcEvent, WorkFlowJobState.Ignored);
                            break;
                    }
                }catch (Exception ex) {
                    log.Warn("Failed to proccess event " + unProcEvent.Type.ToString("G") + " for object " + ((MPPStationServerEvent)unProcEvent.Message).RelatedPersistentObjectId, ex);
                    UpdateEventState(unProcEvent, WorkFlowJobState.Failed);
                }
            }
        }

        public void HandlePublishedPriceUpdated(WorkFlowJob workFlowJob) {

            MultipleServicePrice price = mppWrapper.GetpriceDataByObjectID(((PublishEvent)workFlowJob.Message).RelatedObjectId);
            if (price == null)
            {
                log.Debug("price with object id " + ((PublishEvent)workFlowJob.Message).RelatedObjectId + " not found, price no longer exist, invalidate this event.");
                UpdateEventState(workFlowJob, WorkFlowJobState.Invalid);
                return;
            }

            WorkFlowType action = WorkFlowType.NoAction;
            var serviceConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == ((PublishEvent)workFlowJob.Message).ServiceObjectId);
            if (serviceConfig == null) {
                log.Warn("Can't find workflowtype for servcie " + ((PublishEvent)workFlowJob.Message).ServiceObjectId.ToString());
                action = WorkFlowType.NoAction;
            }
            else {
                if (serviceConfig.ConfigParams.ContainsKey("UpdatePublishedServicePriceWorkFlowType"))
                    action = (WorkFlowType)Enum.Parse(typeof(WorkFlowType), serviceConfig.GetConfigParam("UpdatePublishedServicePriceWorkFlowType"), true);
            }

            
            List<WorkFlowProcess> workFlowProcesses = new List<WorkFlowProcess>();

            //// Create work process parameters
            WorkFlowProcess workFlowProcess = new WorkFlowProcess();
            workFlowProcess.WorkFlowJobId = workFlowJob.Id.Value;
            workFlowProcess.MethodName = this.GetType().Name;
            workFlowProcess.State = WorkFlowProcessState.Init;
            workFlowProcess.TimeStamp = DateTime.UtcNow;
            WorkFlowParameters wp = new WorkFlowParameters();

            MultipleContentService servcie = new MultipleContentService();
            servcie.ObjectID = ((PublishEvent)workFlowJob.Message).ServiceObjectId;
            servcie.Prices.Add(price);

            wp.MultipleContentServices.Add(servcie);
            workFlowProcess.WorkFlowParameters = wp;
            workFlowProcesses.Add(workFlowProcess);

            // execute workflow
            HandleProcessWorkFlow(action, workFlowProcesses, workFlowJob);
        }

        public void HandleContentPublished(WorkFlowJob workFlowJob)
        {
            ContentData content = mppWrapper.GetContentDataByObjectID(((PublishEvent)workFlowJob.Message).RelatedObjectId);
            if (content == null)
            {
                log.Debug("Content with object id " + ((PublishEvent)workFlowJob.Message).RelatedObjectId + " not found, content no longer exist, invalidate this event.");
                UpdateEventState(workFlowJob, WorkFlowJobState.Invalid);
                return;
            }
            Property ingestIdentifierProperty = content.Properties.FirstOrDefault<Property>(p => p.Type.Equals(VODnLiveContentProperties.IngestIdentifier));
            if (ingestIdentifierProperty != null)
            {
                ThreadContext.Properties["IngestIdentifier"] = ingestIdentifierProperty.Value;
            }
            else
            {
                String guid = Guid.NewGuid().ToString();
                log.Debug("No IngestIdentifierProperty found for content with id " + content.ID.ToString() + " using newly created guid " + guid);
                ThreadContext.Properties["IngestIdentifier"] = guid;
            }
            // check content type for action
            WorkFlowType action = WorkFlowType.NoAction;
            ContentType contentType = ConaxIntegrationHelper.GetContentType(content);
            

            switch (contentType)
            {
                case ContentType.NotSpecified:
                    // No type was specified, do nothing for now.
                    log.Warn("content " + content.Name + " " + content.ObjectID + " is missing contentType property, do nothing for now. try it again in next run.");
                    return;
                case ContentType.VOD:
                    action = WorkFlowType.NoAction;
                    var serviceConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == ((PublishEvent)workFlowJob.Message).ServiceObjectId);
                    if (serviceConfig == null) {
                        log.Warn("Can't find workflowtype for servcie " + ((PublishEvent)workFlowJob.Message).ServiceObjectId.ToString());
                        action = WorkFlowType.NoAction;
                    }
                    else {
                        var property = content.Properties.FirstOrDefault(p => p.Type.Equals("PublishedToService", StringComparison.OrdinalIgnoreCase) &&
                                                                              p.Value.Equals(serviceConfig.ServiceObjectId.ToString()));
                        if (property == null) { // never published to this service before.
                            if (serviceConfig.ConfigParams.ContainsKey("PublishWorkFlowType"))
                                action = (WorkFlowType)Enum.Parse(typeof(WorkFlowType), serviceConfig.GetConfigParam("PublishWorkFlowType"), true);
                        }
                        else {  // already published before, trigger udpate.
                            if (serviceConfig.ConfigParams.ContainsKey("UpdatePublishedWorkFlowType"))
                                action = (WorkFlowType)Enum.Parse(typeof(WorkFlowType), serviceConfig.GetConfigParam("UpdatePublishedWorkFlowType"), true);
                        }
                    }
                    break;
                case ContentType.Channel:
                    action = WorkFlowType.NoAction;
                    var serviceConfig2 = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == ((PublishEvent)workFlowJob.Message).ServiceObjectId);
                    if (serviceConfig2 == null) {
                        log.Warn("Can't find workflowtype for servcie " + ((PublishEvent)workFlowJob.Message).ServiceObjectId.ToString());
                        action = WorkFlowType.NoAction;
                    }
                    else {
                        var property = content.Properties.FirstOrDefault(p => p.Type.Equals(VODnLiveContentProperties.PublishedToService, StringComparison.OrdinalIgnoreCase) &&
                                                                              p.Value.Equals(serviceConfig2.ServiceObjectId.ToString()));
                        if (property == null) { // never published to this service before.
                            if (serviceConfig2.ConfigParams.ContainsKey("PublishChannelWorkFlowType"))
                                action = (WorkFlowType)Enum.Parse(typeof(WorkFlowType), serviceConfig2.GetConfigParam("PublishChannelWorkFlowType"), true);
                        }
                        else {  // already published before, trigger udpate.
                            log.Debug("UpdatePublishedChannelWorkFlowType not supported yet, skip");
                            action = WorkFlowType.NoAction;
                            //if (serviceConfig2.ConfigParams.ContainsKey("UpdatePublishedLiveWorkFlowType"))
                            //    action = (WorkFlowType)Enum.Parse(typeof(WorkFlowType), serviceConfig2.GetConfigParam("UpdatePublishedLiveWorkFlowType"), true);
                        }
                    }
                    break;
                default:
                    log.Warn("content " + content.Name + " " + content.ObjectID + " contentType:" + contentType.ToString("G") + " not handled. do nothing for now. try it again in next run.");
                    action = WorkFlowType.NoAction;
                    break;
            }
            

            List<WorkFlowProcess> workFlowProcesses = new List<WorkFlowProcess>();
            // Create work process parameters
            WorkFlowProcess workFlowProcess = new WorkFlowProcess();
            workFlowProcess.WorkFlowJobId = workFlowJob.Id.Value;
            workFlowProcess.MethodName = this.GetType().Name;
            workFlowProcess.State = WorkFlowProcessState.Init;
            workFlowProcess.TimeStamp = DateTime.UtcNow;
            WorkFlowParameters wp = new WorkFlowParameters();
            wp.Content = new ContentData();
            wp.Content.ObjectID = content.ObjectID.Value;
            MultipleContentService service = new MultipleContentService();
            service.ObjectID = ((PublishEvent)workFlowJob.Message).ServiceObjectId;
            wp.MultipleContentServices.Add(service);
            workFlowProcess.WorkFlowParameters = wp;
            workFlowProcesses.Add(workFlowProcess);

            // execute workflow
            HandleProcessWorkFlow(action, workFlowProcesses, workFlowJob);
        }

        public void HandleMultipleServicePriceUpdated(WorkFlowJob workFlowJob)
        {
            MultipleServicePrice price = mppWrapper.GetpriceDataByObjectID(((MPPStationServerEvent)workFlowJob.Message).RelatedPersistentObjectId);
            if (price == null)
            {
                log.Debug("price with object id " + ((MPPStationServerEvent)workFlowJob.Message).RelatedPersistentObjectId + " not found, price no longer exist, invalidate this event.");
                UpdateEventState(workFlowJob, WorkFlowJobState.Invalid);
                return;
            }
            /*
            if (ConaxIntegrationHelper.GetCubiTVOfferID(price).Equals(""))
            {
                log.Debug("price " + price.Title + " " + price.ID + " is missing in CubiTV, set this to Invalid state.");
                UpdateEventState(unProcEvent, MPPEventProcessState.Invalid);
                return;
            }
            */
            log.Debug("In HandleMultipleServicePriceUpdated, starting update of price");
            WorkFlowType action = WorkFlowType.UpdateServicePrice;
            List<WorkFlowProcess> workFlowProcesses = new List<WorkFlowProcess>();
            log.Debug("workflowprocesses");
            //// Create work process parameters
            WorkFlowProcess workFlowProcess = new WorkFlowProcess();
            workFlowProcess.WorkFlowJobId = workFlowJob.Id.Value;
            workFlowProcess.MethodName = this.GetType().Name;
            workFlowProcess.State = WorkFlowProcessState.Init;
            workFlowProcess.TimeStamp = DateTime.UtcNow;
            WorkFlowParameters wp = new WorkFlowParameters();

            MultipleContentService servcie = new MultipleContentService();
            servcie.ID = 0;
            servcie.ObjectID = 0;
            servcie.Prices.Add(price);

            wp.MultipleContentServices.Add(servcie);
            log.Debug("Added price to parameters");
            //if (!price.IsRecurringPurchase.Value)
            //{
            //    wp.Content.ObjectID = price.ContentsIncludedInPrice[0];
            //}
            workFlowProcess.WorkFlowParameters = wp;
            workFlowProcesses.Add(workFlowProcess);

            // execute workflow
            HandleProcessWorkFlow(action, workFlowProcesses, workFlowJob);
        }

        public void HandleContentUpdated(WorkFlowJob unProcEvent)
        {
            ContentData content = mppWrapper.GetContentDataByObjectID(((MPPStationServerEvent)unProcEvent.Message).RelatedPersistentObjectId);
            if (content == null)
            {
                log.Debug("Content with object id " + ((MPPStationServerEvent)unProcEvent.Message).RelatedPersistentObjectId + " not found, content no longer exist, invalidate this event.");
                UpdateEventState(unProcEvent, WorkFlowJobState.Invalid);
                return;
            }
            Property ingestIdentifierProperty = content.Properties.FirstOrDefault<Property>(p => p.Type.Equals(VODnLiveContentProperties.IngestIdentifier));
            if (ingestIdentifierProperty != null)
            {
                ThreadContext.Properties["IngestIdentifier"] = ingestIdentifierProperty.Value;
            }
            else
            {
                Guid guid = new Guid();
                log.Debug("No IngestIdentifierProperty found for content with id " + content.ID.ToString() + " using newly created guid " + guid.ToString());
                ThreadContext.Properties["IngestIdentifier"] = guid.ToString();
            }
            if (content.PublishInfos.Count == 0)
            {
                log.Debug("content:" + content.Name + " id:" + content.ID.Value + " has 0 PublishInfo state no workflow action will be taken. set this to Invalid and move to next MPP event.");
                UpdateEventState(unProcEvent, WorkFlowJobState.Invalid);
                return;
            }

            // content has service perices, lets do the UpdateVODContent or DeleteVODContent(unpubilsh)
            WorkFlowType action = WorkFlowType.UpdateVODContent;
            List<WorkFlowProcess> workFlowProcesses = new List<WorkFlowProcess>();

            ContentType contentType = ConaxIntegrationHelper.GetContentType(content);

            // Check Action type
            if (content.PublishInfos.Count(p => p.PublishState == PublishState.Deleted)
                == content.PublishInfos.Count)
            {
                // take Delete action
                if (contentType == ContentType.VOD)
                {
                    action = WorkFlowType.DeleteVODContent; ;
                }
                else if (contentType == ContentType.Channel)
                {
                    action = WorkFlowType.DeleteChannelContent;
                }
                else if (contentType == ContentType.CatchupTV)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    // not handled content type. set to invalid and skip this eevnt.
                    log.Warn("content " + content.Name + " " + content.ObjectID + " contentType:" + contentType.ToString("G") + " not handled. this event will be set to Invalid.");
                    UpdateEventState(unProcEvent, WorkFlowJobState.Invalid);
                    return;
                }
            }
            else 
            {
                // take updaet action
                if (contentType == ContentType.VOD)
                {
                    action = WorkFlowType.UpdateVODContent; ;
                }
                else if (contentType == ContentType.Channel)
                {
                    action = WorkFlowType.UpdateChannelContent;
                }
                else if (contentType == ContentType.CatchupTV)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    // not handled content type. set to invalid and skip this eevnt.
                    log.Warn("content " + content.Name + " " + content.ObjectID + " contentType:" + contentType.ToString("G") + " not handled. this event will be set to Invalid.");
                    UpdateEventState(unProcEvent, WorkFlowJobState.Invalid);
                    return;
                }
            }

            // Create work process parameters
            WorkFlowProcess workFlowProcess = new WorkFlowProcess();
            workFlowProcess.WorkFlowJobId = unProcEvent.Id.Value;
            workFlowProcess.MethodName = this.GetType().Name;
            workFlowProcess.State = WorkFlowProcessState.Init;
            workFlowProcess.TimeStamp = DateTime.UtcNow;
            WorkFlowParameters wp = new WorkFlowParameters();
            wp.Content = new ContentData();
            wp.Content.ObjectID = content.ObjectID.Value;
            workFlowProcess.WorkFlowParameters = wp;
            workFlowProcesses.Add(workFlowProcess);

            // execute workflow
            HandleProcessWorkFlow(action, workFlowProcesses, unProcEvent);
        }

        public void HandleContentCreated(WorkFlowJob workFlowJob)
        {

            ContentData content = mppWrapper.GetContentDataByObjectID(((MPPStationServerEvent)workFlowJob.Message).RelatedPersistentObjectId);
            if (content == null)
            {
                log.Debug("Content with object id " + ((MPPStationServerEvent)workFlowJob.Message).RelatedPersistentObjectId + " not found, content no longer exist, invalidate this event.");
                UpdateEventState(workFlowJob, WorkFlowJobState.Invalid);
                return;
            }
            ContentType contentType = ConaxIntegrationHelper.GetContentType(content);
            if (contentType != ContentType.CatchupTV)
            {
                Property ingestIdentifierProperty =
                    content.Properties.FirstOrDefault<Property>(
                        p => p.Type.Equals(VODnLiveContentProperties.IngestIdentifier));
                if (ingestIdentifierProperty != null)
                {
                    ThreadContext.Properties["IngestIdentifier"] = ingestIdentifierProperty.Value;
                }
                else
                {
                    String guid = Guid.NewGuid().ToString();
                    log.Debug("No IngestIdentifierProperty found for content with id " + content.ID.ToString() +
                              " using newly created guid " + guid);
                    ThreadContext.Properties["IngestIdentifier"] = guid;
                }
            }
           
            // check content type for action
            WorkFlowType action = WorkFlowType.NoAction;
                       
            switch (contentType)
            {
                case ContentType.NotSpecified:
                    // No type was specified, do nothing for now.
                    log.Warn("content " + content.Name + " " + content.ObjectID + " is missing contentType property, do nothing for now. try it again in next run.");
                    return;
                case ContentType.VOD:
                    action = WorkFlowType.AddVODContent;
                    break;
                case ContentType.Channel:
                    action = WorkFlowType.AddChannelContent;
                    break;
                case ContentType.CatchupTV: 
                    action = WorkFlowType.AddCatchUpContent;
                    UpdateEventState(workFlowJob, WorkFlowJobState.Ignored);
                    return; // not handled
                default:
                    log.Warn("content " + content.Name + " " + content.ObjectID + " contentType:" + contentType.ToString("G") + " not handled. do nothing for now. try it again in next run.");
                    break;
            }
            

            List<WorkFlowProcess> workFlowProcesses = new List<WorkFlowProcess>();
            // Create work process parameters
            WorkFlowProcess workFlowProcess = new WorkFlowProcess();
            workFlowProcess.WorkFlowJobId = workFlowJob.Id.Value;
            workFlowProcess.MethodName = this.GetType().Name;
            workFlowProcess.State = WorkFlowProcessState.Init;
            workFlowProcess.TimeStamp = DateTime.UtcNow;
            WorkFlowParameters wp = new WorkFlowParameters();
            wp.Content = new ContentData();
            wp.Content.ObjectID = content.ObjectID.Value;
            workFlowProcess.WorkFlowParameters = wp;
            workFlowProcesses.Add(workFlowProcess);

            // execute workflow
            HandleProcessWorkFlow(action, workFlowProcesses, workFlowJob);
        }

        public Boolean IsContentValid(ContentData content, List<MultipleServicePrice> servicePrices)
        {
            //ContentType
            var contentTypeProperty = content.Properties.FirstOrDefault(p => p.Type.Equals("ContentType", StringComparison.OrdinalIgnoreCase));

            if (contentTypeProperty != null &&
                contentTypeProperty.Value.Equals("VOD", StringComparison.OrdinalIgnoreCase) &&
                servicePrices.Count == 0)
            {
                // no service price, leave it for now.
                log.Warn("content " + content.Name + " " + content.ObjectID + " doesn't have any service prices. No action will be taken for now.");
                return false;
            }
            //foreach (Asset asset in content.Assets)
            //{
            //    if (String.IsNullOrEmpty(asset.contentAssetServerName))
            //    {
            //        log.Warn("content " + content.Name + " " + content.ObjectID + " Asset is missing CAS.");
            //        return false;
            //    }
            //}

            // check PublishInfos
            if (content.PublishInfos.Count != 1)
            {
                // a content must only have one PublishInfo, this PublishInfo is used for determine contents delete sate.
                log.Warn("content " + content.Name + " " + content.ObjectID + " must only have one PublishInfos, PublishInfos.Count " + content.PublishInfos.Count);
                return false;
            }
            if (String.IsNullOrEmpty(content.PublishInfos[0].Region))
            {
                log.Warn("content " + content.Name + " " + content.ObjectID + " PublishInfo is missing Region.");
                return false;
            }
            //if (content.PublishInfos[0].PublishState != PublishState.Published)
            //{
            //    log.Warn("Create Content event has to have Pubilshed state, content:" + content.Name + " id:" + content.ID.Value + " has PublishInfo state " + content.PublishInfos[0].PublishState.ToString("G") + " no workflow action will be taken. skip to next MPP event.");
            //    return false;
            //}            

            return true;
        }

        public Boolean IsMaxtriesExceeded(MPPStationServerEvent unProcEvent)
        {
            var workFlowConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

            Int32 tries = DBWrapper.CountWorkFlowTries(unProcEvent.ObjectId);
            Int32 maxTries = 3;
            try
            {
                maxTries = Int32.Parse(workFlowConfig.GetConfigParam("MaxWorkFlowTries"));
            }
            catch (Exception ex)
            {
            }

            return (maxTries <= tries);
        }
    }
}
