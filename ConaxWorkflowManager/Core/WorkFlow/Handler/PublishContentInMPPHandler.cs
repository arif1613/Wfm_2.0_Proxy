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
    public class PublishContentInMPPHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithActiveEvent;

            var proeprty = content.Properties.FirstOrDefault(p => p.Type.Equals(VODnLiveContentProperties.EnableQA, StringComparison.OrdinalIgnoreCase));
            if (proeprty != null) {
                if (proeprty.Value.Equals("false", StringComparison.OrdinalIgnoreCase)) { 
                    // auto publish, change state.
                    foreach (PublishInfo publishInfo in content.PublishInfos) {
                        publishInfo.PublishState = PublishState.Published;
                    }
                    log.Debug("EnableQA is false, change publishstate to published for auto publishing.");
                    content = MPPIntegrationServiceManager.InstanceWithActiveEvent.UpdateContent(content);
                } else {
                    // NeedQa State
                    foreach (PublishInfo publishInfo in content.PublishInfos) {
                        publishInfo.PublishState = PublishState.NeedsQA;
                    }
                    log.Debug("EnableQA is true, change publishstate to NeedQA for Manual publishing.");
                    content = MPPIntegrationServiceManager.InstanceWithPassiveEvent.UpdateContent(content);
                }                
                parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content = content;
            }


            return new RequestResult(RequestResultState.Successful);
        }
    }
}
