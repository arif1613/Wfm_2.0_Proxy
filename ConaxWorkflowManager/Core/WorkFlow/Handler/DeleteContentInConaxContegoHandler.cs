using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ConaxContegoOnDemandContentService;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ConaxContegoPPVProductService;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class DeleteContentInConaxContegoHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");
            try
            {
                ConaxContegoServicesWrapper CCWrapper = new ConaxContegoServicesWrapper();
                TaskConfig contextConfig = parameters.Config;
                ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
                List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;


                String conaxContegoContentID = ConaxIntegrationHelper.GetConaxContegoContentID(content);
                OnDemandContentResponseType responseType = CCWrapper.DeleteVODContent(conaxContegoContentID);
                if (responseType.TransactionStatus.StatusCode.Equals("OK"))
                {
                    log.Debug("delete conaxContego Content " + conaxContegoContentID + " successfull.");
                }
                else
                {
                    log.Warn("Failed to delete conaxContego Content " + conaxContegoContentID);
                    //return false;
                }

                foreach (MultipleContentService service in services)
                {
                    foreach (MultipleServicePrice servicePrice in service.Prices)
                    {
                        if (!servicePrice.IsRecurringPurchase.Value)
                        {
                            // content price, delete
                            PpvProductResponseType ppvProductResponseType = CCWrapper.DeleteServicePrice(servicePrice);
                            if (!ppvProductResponseType.TransactionStatus.StatusCode.Equals("OK"))
                            {
                                log.Warn("Failed to delete conaxContego product for service ID:" + servicePrice.ID.ToString());
                                //return false;
                            }
                            else
                            {
                                log.Debug("Successfully deleted conaxContego product for service ID:" + servicePrice.ID.ToString());
                            }
                        }
                        // no api for subscription product.
                    }
                }
            }
            catch (Exception exc)
            {
                log.Warn("Error when deleting in Contego", exc);
            }

            return new RequestResult(RequestResultState.Successful);
        }
    }
}
