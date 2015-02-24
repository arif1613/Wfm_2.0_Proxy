using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    /// <summary>
    /// This handler will do nothing on the normal process, and on error will change the publishinfo state back to qa.
    /// </summary>
    public class UnPublishContentOnFailHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        public override RequestResult OnProcess(RequestParameters parameters)
        {   // do nothing here,
            return new RequestResult(RequestResultState.Successful);
        }

        public override void OnChainFailed(RequestParameters parameters)
        {
            try
            {
                ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

                if (parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices.Count == 0) {
                    log.Error("No servcie was loaded");
                    return;
                }
                MultipleContentService service = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0];

                if (service.ServiceViewMatchRules.Count != 1) {
                    log.Warn("Service " + service.Name + service.ID.Value + " has ServiceViewMatchRules count " + service.ServiceViewMatchRules.Count + " it should only have one, this should be fixed.");
                    return;
                }

                foreach(PublishInfo publishInfo in  content.PublishInfos) {
                    if (publishInfo.Region.Equals(service.ServiceViewMatchRules[0].Region, StringComparison.OrdinalIgnoreCase)) {
                        publishInfo.PublishState = PublishState.NeedsQA;
                        log.Debug("Change publish state to NeedQA for content " + content.Name + " " + content.ID.Value + " with publish region " + publishInfo.Region + " / service " + service.Name + " " + service.ID.Value);
                    }
                }

                MPPIntegrationServiceManager.InstanceWithPassiveEvent.UpdateContent(content, false);
                


            } catch (Exception ex) {
                log.Warn("Failed to revert the publishinfo state back to NeedQA state.", ex);
            }

        }
    }
}
