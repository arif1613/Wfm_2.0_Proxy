using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.System;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class RegisterPublishWorkFlowJobHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IDBWrapper dbwrapper = DBManager.Instance;

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            

            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;

            List<MultipleContentService> publishToService = new List<MultipleContentService>();
            // check if service has publish stet for publish
            foreach (MultipleContentService service in services)
            {
                List<ServiceViewMatchRule> matchRules = service.ServiceViewMatchRules;
                foreach (ServiceViewMatchRule matchRule in matchRules) {
                    var publishInfo = content.PublishInfos.FirstOrDefault(p => p.Region.Equals(matchRule.Region, StringComparison.OrdinalIgnoreCase) &&
                                                                               p.PublishState == PublishState.Published);
                    if (publishInfo != null && !publishToService.Contains(service))
                        publishToService.Add(service);  // add this servcie for pubhlish.
                }
            }

            // create pubilsh jobs
            foreach (MultipleContentService service in publishToService)
            {                        
                PublishEvent publishEvent = new PublishEvent();
                publishEvent.RelatedObjectId = content.ObjectID.Value;
                publishEvent.ServiceObjectId = service.ObjectID.Value;

                WorkFlowJob wfj = new WorkFlowJob();
                wfj.SourceId = 0;
                wfj.Type = EventType.ContentPublished;
                wfj.Message = publishEvent;
                wfj.MessageType = publishEvent.GetType().FullName;
                wfj.Created = DateTime.UtcNow;
                wfj.LastModified = DateTime.UtcNow;
                wfj.NotUntil = DateTime.UtcNow;
                wfj.State = WorkFlowJobState.UnProcessed;
                // save jobs
                dbwrapper.AddWorkFlowJob(wfj);
                log.Debug("Create publish job for service " + service.Name + " " + service.ID.Value + " " + service.ObjectID.Value + " content " + content.Name + " " + content.ID.Value + " " + content.ObjectID.Value);
            }

            return new RequestResult(RequestResultState.Successful);
        }
    }
}
