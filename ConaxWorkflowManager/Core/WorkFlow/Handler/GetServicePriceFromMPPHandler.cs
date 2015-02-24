using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class GetServicePriceFromMPPHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;


            MultipleServicePrice servicePrice = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0].Prices[0];

            List<ContentData> contents = new List<ContentData>();
            foreach(UInt64 contentObjectId in servicePrice.ContentsIncludedInPrice) {
                ContentData content = null;
                try
                {
                    content = mppWrapper.GetContentDataByObjectID(contentObjectId);
                }
                catch (Exception ex) { }

                if (content != null)
                    contents.Add(content);
            }

            MultipleContentService matchService = new MultipleContentService();
            foreach(ContentData content in contents) {
                List<MultipleContentService> allServices = new List<MultipleContentService>();
                // load all connected servcies
                // content should only have one agreement for conax solution.
                List<ContentAgreement> contentAgreements = mppWrapper.GetAllServicesForContent(content);
                foreach (ContentAgreement contentAgreement in contentAgreements)
                    allServices.AddRange(contentAgreement.IncludedServices);
            
                // load prices
                foreach (MultipleContentService service in allServices)
                {
                    List<MultipleServicePrice> servicePrices = mppWrapper.GetServicePricesForContent(content, service.ObjectID.Value);
                    log.Debug(servicePrices.Count + " service prices found for content " + content.ObjectID + " in service " + service.ID.Value);
                    // find price
                    foreach(MultipleServicePrice price in servicePrices) {
                        if (servicePrice.ID == price.ID) {
                            // make sure only one copy of it. since service might comes from cacn, so this price might already been included by somehting else.
                            if (!service.Prices.Contains(servicePrice)) 
                                service.Prices.Add(servicePrice);
                            break;
                        }
                    }
                    List<ServiceViewMatchRule> matchRules = mppWrapper.GetServiceViewMatchRules(service);
                    service.ServiceViewMatchRules = matchRules;

                    if (service.Prices.Count > 0) {
                        matchService = service;
                        break; // found price already
                    }
                }
            }

            parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices = new List<MultipleContentService>();
            parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices.Add(matchService);
            return new RequestResult(RequestResultState.Successful);
        }
    }
}
