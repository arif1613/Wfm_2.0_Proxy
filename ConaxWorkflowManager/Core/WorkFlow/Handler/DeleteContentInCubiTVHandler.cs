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
    public class DeleteContentInCubiTVHandler : ResponsibilityHandler
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

                String cubiTVContentID = ConaxIntegrationHelper.GetCubiTVContentID(service.ObjectID.Value, content);
                if (String.IsNullOrEmpty(cubiTVContentID))
                    continue; // no reference id to cubi for this service. skip.

                ICubiTVMWServiceWrapper wrapper = CubiTVMiddlewareManager.Instance(service.ObjectID.Value);
                log.Debug("Initializing deletion of content with name= " + content.Name + " for servcie " + service.Name + " " + service.ID.ToString() + " " + service.ObjectID.ToString());

                try
                {
                    if (!String.IsNullOrEmpty(cubiTVContentID))
                    {
                        log.Debug("Deleting content prices belonging to content with CubiTVContentID= " + cubiTVContentID);

                        foreach (MultipleServicePrice price in service.Prices)
                        {
                            String cubiTVPriceID = ConaxIntegrationHelper.GetCubiTVOfferID(price);
                            if (!price.IsRecurringPurchase.Value)
                            {
                                if (!String.IsNullOrEmpty(cubiTVPriceID))
                                {
                                    if (!wrapper.DeleteContentPrice(ulong.Parse(cubiTVPriceID)))
                                    {
                                        log.Warn("Error when deleting price in CubiTV, priceID = " + price.ID + ", CubiTVPriceID= " + cubiTVPriceID);
                                        //return false;
                                    }
                                }
                                else
                                {
                                    log.Warn("Error when deleting content, no CubiTV price ID found for content " + content.Name);
                                    //return false;
                                }
                            }
                            else
                            {
                                if (price.ContentsIncludedInPrice.Contains((ulong)content.ObjectID))
                                    price.ContentsIncludedInPrice.Remove((ulong)content.ObjectID);

                                wrapper.UpdateSubscriptionPrice(price);
                            }
                        }


                        log.Debug("Deleting content with CubiTVContentID= " + cubiTVContentID);
                        if (!wrapper.DeleteContent(ulong.Parse(cubiTVContentID)))
                        {
                            log.Warn("Error when deleting content from CubiTV, name= " + content.Name);
                            //return false;
                        }

                    }
                    else
                    {
                        log.Warn("Error when deleting content, no CubiTV Content ID found for content " + content.Name);
                        //return false;
                    }
                }
                catch (Exception e)
                {
                    log.Warn("Error when deleting content " + content.Name + " for servcie " + service.Name + " " + service.ID.ToString() + " " + service.ObjectID.ToString(), e);
                    //return false;
                }
            }

            return new RequestResult(RequestResultState.Successful);
        }
    }
}
