using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.System;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class RegisterUpdatePublishedPriceWorkFlowJobHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IDBWrapper dbwrapper = DBManager.Instance;

        public override RequestResult OnProcess(RequestParameters parameters)
        {

            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;

            // create pubilsh jobs
            foreach (MultipleContentService service in services)
            {
                log.Debug("Create " + service.Prices.Count.ToString() + " price publish job for service " + service.Name + " " + service.ID.Value);
                foreach (MultipleServicePrice price in service.Prices)
                {
                    // check if price has reference id
                    String cubiTVOfferID = ConaxIntegrationHelper.GetServiceExtPriceID(price);
                    if (String.IsNullOrEmpty(cubiTVOfferID))
                    {
                        log.Warn("Service Price " + price.Title + " " + price.ID.Value + " don't have external reference id in service " + service.Name + " " + service.ID.Value + " " + service.ObjectID.Value);
                        continue;
                    }


                    PublishEvent publishEvent = new PublishEvent();
                    publishEvent.RelatedObjectId = price.ObjectID.Value;
                    publishEvent.ServiceObjectId = service.ObjectID.Value;

                    WorkFlowJob wfj = new WorkFlowJob();
                    wfj.SourceId = 0;
                    wfj.Type = EventType.PublishedMultipleServicePriceUpdated;
                    wfj.Message = publishEvent;
                    wfj.MessageType = publishEvent.GetType().FullName;
                    wfj.Created = DateTime.UtcNow;
                    wfj.LastModified = DateTime.UtcNow;
                    wfj.NotUntil = DateTime.UtcNow;
                    wfj.State = WorkFlowJobState.UnProcessed;
                    // save jobs
                    dbwrapper.AddWorkFlowJob(wfj);
                    log.Debug("Create publish job for service " + service.Name + " " + service.ID.Value + " " + service.ObjectID.Value + " Price " + price.Title + " " + price.ID.Value + " " + price.ObjectID.Value);
                }
            }
            return new RequestResult(RequestResultState.Successful);
        }
    }
}
