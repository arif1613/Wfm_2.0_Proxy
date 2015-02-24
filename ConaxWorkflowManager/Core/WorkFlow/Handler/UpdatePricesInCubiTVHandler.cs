using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class UpdatePricesInCubiTVHandler : ResponsibilityHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");
            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            foreach (MultipleContentService service in services)
            {
                ICubiTVMWServiceWrapper wrapper = CubiTVMiddlewareManager.Instance(service.ObjectID.Value);
                foreach (MultipleServicePrice servicePrice in service.Prices)
                {
                    if (servicePrice.IsRecurringPurchase.Value)
                    {
                        try
                        {
                            String cubiTVPriceID = ConaxIntegrationHelper.GetCubiTVOfferID(servicePrice);
                            if (!String.IsNullOrEmpty(cubiTVPriceID))
                            {
                                wrapper.UpdateSubscriptionPrice(servicePrice);
                            }
                            else
                            {
                                string message = "Failed to update Price, no CubiTV priceID on price with ID = " + servicePrice.ID.ToString();
                                log.Error(message);
                                return new RequestResult(RequestResultState.Failed, message);
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error("Something went wrong when updating servicePrice", e);
                            return new RequestResult(RequestResultState.Exception, e);
                        }
                    }
                    else
                    {
                        try
                        {
                            String cubiTVOfferID = ConaxIntegrationHelper.GetCubiTVOfferID(servicePrice);
                            if (!String.IsNullOrEmpty(cubiTVOfferID))
                            {
                                wrapper.UpdateContentPrice(ulong.Parse(cubiTVOfferID), servicePrice, content);
                            }
                            else
                            {
                                string message = "Failed to update Price in Cubiware, no CubiTV priceID on price with ID = " + servicePrice.ID.ToString() + " price probably doesn't exist yet, ignoring";
                                log.Warn(message);
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error("Something went wrong when updating contentPrice", e);
                            return new RequestResult(RequestResultState.Exception, e);
                        }
                    }

                }
            }

            return new RequestResult(RequestResultState.Successful);
        }
    }
}
