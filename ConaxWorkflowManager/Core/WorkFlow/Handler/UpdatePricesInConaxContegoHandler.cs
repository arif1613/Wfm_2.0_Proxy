using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ConaxContegoPPVProductService;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class UpdatePricesInConaxContegoHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {

            log.Debug("OnProcess");
            ConaxContegoServicesWrapper CCWrapper = new ConaxContegoServicesWrapper();
            TaskConfig contextConfig = parameters.Config;
            foreach (MultipleContentService servcie in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices)
            {
                foreach (MultipleServicePrice price in servcie.Prices)
                {
                    if (price.IsRecurringPurchase.Value)
                    {
                        log.Debug("Ignoring recurring price with mppID + " + price.ID.ToString() + " when updating in Conax Contego");
                        continue;
                    }
                    log.Debug("Updating price with mppID + " + price.ID.ToString() + " in Conax Contego");
                    PpvProductResponseType result = CCWrapper.UpdateContentPrice(price, contextConfig);
                    if (result.TransactionStatus.StatusCode != "OK")
                    {
                        string message = "Failed to Update content price in Conax contego, statuscode:" + result.TransactionStatus.StatusCode + " Message:" + result.TransactionStatus.Message;
                        log.Error(message);
                        return new RequestResult(RequestResultState.Failed, message);
                    }
                }
            }
            log.Debug("Conax contego content price successfully updated.");
            return new RequestResult(RequestResultState.Successful);
        }
    }
}
