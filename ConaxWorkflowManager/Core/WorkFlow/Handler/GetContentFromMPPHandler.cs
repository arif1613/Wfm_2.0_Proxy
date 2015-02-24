using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class GetContentFromMPPHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Info("OnProcess"); 
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

            // Load content from MPP
            UInt64 objectID = 0;
            if (parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content.ObjectID.HasValue)
            {
                objectID = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content.ObjectID.Value;
            }
            else
            {
                string message = "Missing Content ObjectID, can't fetch any content from MPP";
                log.Error(message);
                return new RequestResult(RequestResultState.Failed, message);
            }

            ContentData content = mppWrapper.GetContentDataByObjectID(objectID);
            log.Debug("Content " + content.Name + " " + content.ObjectID + " fetched from MPP.");
            parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content = content;


            // load Service prices for content from MPP
            List<MultipleContentService> allServices = new List<MultipleContentService>();
            if (parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices.Count != 0)
            {
                // load specified servcie
                foreach (MultipleContentService service in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices) {
                    MultipleContentService loadService = mppWrapper.GetServiceForObjectId(service.ObjectID.Value);
                    allServices.Add(loadService);
                }
                
            } else {
                // load all connected servcies
                // content should only have one agreement for conax solution.
                List<ContentAgreement> contentAgreements = mppWrapper.GetAllServicesForContent(content);
                foreach (ContentAgreement contentAgreement in contentAgreements)
                    allServices.AddRange(contentAgreement.IncludedServices);
            }

            // load prices
            foreach (MultipleContentService service in allServices)
            {
                List<MultipleServicePrice> servicePrices = mppWrapper.GetServicePricesForContent(content, service.ObjectID.Value);
                log.Debug(servicePrices.Count + " service prices found for content " + content.ObjectID + " in service " + service.ID.Value);
                service.Prices = servicePrices;

                List<ServiceViewMatchRule> matchRules = mppWrapper.GetServiceViewMatchRules(service);
                service.ServiceViewMatchRules = matchRules;
            }

            parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices = allServices;
            return new RequestResult(RequestResultState.Successful);
        }
    }
}
