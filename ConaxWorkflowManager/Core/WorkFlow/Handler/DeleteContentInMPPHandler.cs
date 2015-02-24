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
    public class DeleteContentInMPPHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;

            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

            foreach (MultipleContentService service in services)
            {
                foreach (MultipleServicePrice MSP in service.Prices)
                {
                    if (MSP.IsRecurringPurchase.HasValue && !MSP.IsRecurringPurchase.Value)
                    {   // only delete content prices.
                        try
                        {
                            log.Debug("Start to delete price " + MSP.ID.Value + " " + MSP.Title);
                            mppWrapper.DeleteServicePrice(MSP);
                        }
                        catch (Exception ex)
                        {
                            log.Warn("Failed to delete price " + MSP.ID.Value + " " + MSP.Title);
                        }
                    }
                }
            }

            if (content.ID.HasValue)
            {
                try
                {
                    log.Debug("Start deleting content " + content.ID + " " + content.Name);
                    mppWrapper.DeleteContent(content);
                }
                catch (Exception ex)
                {
                    log.Warn("Failed to delete content " + content.Name + " " + content.ID.Value);
                }
            }

            return new RequestResult(RequestResultState.Successful);
        }
    }
}
