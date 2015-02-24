using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ConaxContegoPPVProductService;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ConaxContegoOnDemandContentService;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.Collections;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class RegisterContentInConaxContegoHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Info("OnProcess");
            ConaxContegoServicesWrapper CCWrapper = new ConaxContegoServicesWrapper();
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

            TaskConfig contextConfig = parameters.Config;
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;

            List<MultipleServicePrice> allServicePrices = new List<MultipleServicePrice>();
            foreach (MultipleContentService service in services)
                allServicePrices.AddRange(service.Prices);

            RegisterContentInConaxContegoHandler.CheckForDoubleConfiguredContegoIDs(allServicePrices); // check if same contegoID is configured on more then one subscriptionPrice
           
            // register prices 
            foreach (MultipleServicePrice servicePrice in allServicePrices)
            {
                // (skip subscription prices, Conax contego doesn't have API for CRUD subscription prices/"products in conax term")
                if (servicePrice.IsRecurringPurchase.HasValue && servicePrice.IsRecurringPurchase.Value)
                    continue;

                String ConaxContegoProductID = ConaxIntegrationHelper.GetConaxContegoProductID(servicePrice);
                if (!String.IsNullOrEmpty(ConaxContegoProductID))
                    continue; // ConaxContego Product already exist.

                // regsiter individual price/"PPV rental product in conax term"
                PpvProductResponseType ppvResponseType = CCWrapper.AddServicePrice(servicePrice);
                log.Debug("CreatePpvProduct statusCode:" + ppvResponseType.TransactionStatus.StatusCode);
                if (ppvResponseType.TransactionStatus.StatusCode.Equals("OK"))
                {
                    // update MPP with the new created PPV rental product ID to Service prices longDesc property.
                    ConaxIntegrationHelper.SetConaxContegoProductID(servicePrice, ppvResponseType.PpvProduct.ProductId.ToString());
                    log.Debug("Update MPP service price " + servicePrice.ID + " with the new created Conax contego product Id: " + servicePrice.LongDescription);
                    mppWrapper.UpdateServicePrice(servicePrice);
                }
                else
                {
                    string message = "Failed to create PPV rental product in Conax contego. TransactionId: " + ppvResponseType.TransactionStatus.TransactionId + " Message: " + ppvResponseType.TransactionStatus.Message;
                    log.Error(message);
                    return new RequestResult(RequestResultState.Failed, message);
                }
            }


            // register products
            OnDemandContentResponseType result = CCWrapper.AddVODContent(content, allServicePrices, contextConfig);
            if (result.TransactionStatus.StatusCode == "OK")
            {
                // update content with conax product id
                ConaxIntegrationHelper.SetConaxContegoContentID(content, result.OnDemandContent.ContentId);
                log.Debug("Update MPP content " + content.ID.Value + " with new conax contego content id:" + result.OnDemandContent.ContentId);
                content = mppWrapper.UpdateContent(content);
                parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content = content;
            }
            else
            {
                string message = "Failed to Create content in Conax contego, statuscode:" + result.TransactionStatus.StatusCode + " Message:" + result.TransactionStatus.Message;
                log.Error(message);
                return new RequestResult(RequestResultState.Failed, message);
            }

            log.Info("Conax contego content successfully created.");
            return new RequestResult(RequestResultState.Successful);
        }

        public static void CheckForDoubleConfiguredContegoIDs(List<MultipleServicePrice> allServicePrices)
        {
            Hashtable svodPriceConaxIDList = new Hashtable();
            foreach (MultipleServicePrice servicePrice in allServicePrices)
            {
                if (servicePrice.IsRecurringPurchase.HasValue && servicePrice.IsRecurringPurchase.Value)
                {
                    String conaxID = ConaxIntegrationHelper.GetConaxContegoProductID(servicePrice);
                    if (!String.IsNullOrEmpty(conaxID))
                    {
                        if (svodPriceConaxIDList.ContainsKey(conaxID))
                        {
                            throw new Exception("Found same ConaxID on two prices with ID " + servicePrice.ID.ToString() + " and " + svodPriceConaxIDList[conaxID]);
                        }
                        else
                        {
                            svodPriceConaxIDList.Add(conaxID, servicePrice.ID.ToString());
                        }

                    }
                    else
                    {
                        throw new Exception("ServicePrice with id " + servicePrice.ID.ToString() + " is missing a contegoID");
                    }
                }
            }
        }

        public override void OnChainFailed(RequestParameters parameters)
        {
            log.Debug("OnChainFailed");
            ConaxContegoServicesWrapper CCWrapper = new ConaxContegoServicesWrapper();
            //TaskConfig contextConfig = parameters.Config;
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            // delete cotnent
            String conaxContegoContentID = ConaxIntegrationHelper.GetConaxContegoContentID(content);
            OnDemandContentResponseType responseType = CCWrapper.DeleteVODContent(conaxContegoContentID);
            if (responseType.TransactionStatus.StatusCode.Equals("OK"))
                log.Debug("delete conaxContego Content " + conaxContegoContentID + " successfull.");
            else
                log.Warn("Failed to delete conaxContego Content " + conaxContegoContentID);

            foreach (MultipleContentService service in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices)
            {
                // delete prices
                foreach (MultipleServicePrice servicePrice in service.Prices)
                {
                    if (!servicePrice.IsRecurringPurchase.Value)
                    {
                        // content price, delete
                        PpvProductResponseType ppvProductResponseType = CCWrapper.DeleteServicePrice(servicePrice);
                        if (!ppvProductResponseType.TransactionStatus.StatusCode.Equals("OK"))
                            log.Warn("Failed to delete conaxContego product for service ID:" + servicePrice.ID.ToString());
                        else
                            log.Debug("Successfully deleted conaxContego product for service ID:" + servicePrice.ID.ToString());
                    }
                    // no api for subscription product.
                }
            }
        }
    }
}
