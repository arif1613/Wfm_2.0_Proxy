using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ConaxContegoOnDemandContentService;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class UpdateContentInConaxContegoHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            
            log.Debug("OnProcess");
            ConaxContegoServicesWrapper CCWrapper = new ConaxContegoServicesWrapper();
            TaskConfig contextConfig = parameters.Config;
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;

            List<MultipleServicePrice> allservicePrices = new List<MultipleServicePrice>();
            foreach (MultipleContentService service in services)
                allservicePrices.AddRange(service.Prices);

            RegisterContentInConaxContegoHandler.CheckForDoubleConfiguredContegoIDs(allservicePrices); // check if same contegoID is configured on more then one subscriptionPrice

            OnDemandContentResponseType result = CCWrapper.UpdateVODContent(content, allservicePrices, contextConfig);
            if (result.TransactionStatus.StatusCode != "OK") {
                string message = "Failed to update content " + content.Name + " " + content.ID.Value + " in Conax contego, statuscode:" + result.TransactionStatus.StatusCode + " Message:" + result.TransactionStatus.Message;
                log.Error(message);
                // TODO: udpate alla publishinfo from published to NeedQA

                return new RequestResult(RequestResultState.Failed, message);
            }

            log.Debug("Conax contego content successfully updated.");
            return new RequestResult(RequestResultState.Successful);
        }


        public override void OnChainFailed(RequestParameters parameters)
        {
            log.Debug("OnChainFailed");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            log.Debug("Checking publishing");
            HandleContentPublished(content);
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            mppWrapper.UpdateContent(content, false);
        }

        private void HandleContentPublished(ContentData content)
        {
            foreach (PublishInfo pi in content.PublishInfos)
            {
                if (!ContentIsAlreadyPublishedToRegion(pi, content))
                {
                    log.Debug("Content wasn't already published to region " + pi.Region + " setting to NeedsQA");
                    pi.PublishState = PublishState.NeedsQA;
                }
            }
        }

        private bool ContentIsAlreadyPublishedToRegion(PublishInfo publishInfo, ContentData content)
        {
            bool ret = false;
            ulong serviceObjectID = 0;
            foreach (ContentAgreement ca in content.ContentAgreements)
            {
                foreach (MultipleContentService service in ca.IncludedServices)
                {
                    String regionName = "";
                    if (service.ServiceViewMatchRules[0] != null)
                        regionName = service.ServiceViewMatchRules[0].Region;
                    if (publishInfo.Region.Equals(regionName, StringComparison.OrdinalIgnoreCase))
                    {
                        serviceObjectID = service.ObjectID.Value;
                        break;
                    }
                }
            }
            Property publishedTo = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals("PublishedToService") && p.Value.Equals(serviceObjectID.ToString()));
            if (publishedTo != null)
            {
                ret = true;
            }
            return ret;
        }
    }
}
