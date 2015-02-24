using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class VerifyDataHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Info("OnProcess");
            //return new RequestResult(RequestResultState.Revoke); ;
            ValidatorResult res;
            switch (parameters.Action)
            {
                case WorkFlowType.AddVODContent:
                case WorkFlowType.AddChannelContent:
                    res = IsContentComplete(parameters);
                    if (!res.IsOK)
                    {
                        log.Warn(res.Message);
                        return new RequestResult(RequestResultState.Revoke); // Conetnt is not ready to be processed.
                    }
                    res = IsContentValid(parameters);
                    if (!res.IsOK)
                    {
                        log.Warn(res.Message);
                        return new RequestResult(RequestResultState.Failed, res.Message); // Conetnt is not valid to be processed.
                    }
                    break;
                case WorkFlowType.UpdateVODContent:
                case WorkFlowType.UpdateChannelContent:
                    res = IsContentComplete(parameters);
                    if (!res.IsOK)
                    {
                        log.Warn(res.Message);
                        return new RequestResult(RequestResultState.Failed, res.Message); // Conetnt is not valid to be processed.
                    }
                    res = IsContentValid(parameters);
                    if (!res.IsOK)
                    {
                        log.Warn(res.Message);
                        return new RequestResult(RequestResultState.Failed, res.Message); // Conetnt is not valid to be processed.
                    }
                    break;
                case WorkFlowType.DeleteVODContent:
                    break;
                case WorkFlowType.UpdateServicePrice:
                    res = IsServicePriceValid(parameters);
                    if (!res.IsOK)
                    {
                        log.Warn(res.Message);
                        return new RequestResult(RequestResultState.Failed, res.Message); // ServicePrice is not valid to be processed.
                    }
                    break;
                default:
                    log.Warn("No data validation for workflow type: " + parameters.Action.ToString("G") + " is implemented.");
                    break;
            }

            return new RequestResult(RequestResultState.Successful);
        }

        private ValidatorResult IsServicePriceValid(RequestParameters parameters)
        {
            foreach (MultipleContentService service in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices)
            {
                foreach (MultipleServicePrice price in service.Prices)
                {

                    if (String.IsNullOrEmpty(ConaxIntegrationHelper.GetConaxContegoProductID(price)))
                    {
                        // This content doesn't have related contego/cubi price id, no update can be done, invalid action.
                        return new ValidatorResult(false, "price " + price.Title + " " + price.ID + " is missing in Contego, set this to Invalid state.");
                    }
                }
            }

            return new ValidatorResult(true);
        }

        public ValidatorResult IsContentValid(RequestParameters parameters)
        {

            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            //List<MultipleServicePrice> servicePrices = mppWrapper.GetServicePricesForContent(content);
            var contentTypeProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(VODnLiveContentProperties.ContentType, StringComparison.OrdinalIgnoreCase));
            ContentType ct = (ContentType)Enum.Parse(typeof(ContentType), contentTypeProperty.Value, true);

            if (parameters.Action == WorkFlowType.UpdateVODContent)
            {
                if (String.IsNullOrEmpty(ConaxIntegrationHelper.GetConaxContegoContentID(content)))
                {
                    // This content doesn't have related contego content id, no update can be done, invalid action.
                    return new ValidatorResult(false, "content " + content.Name + " " + content.ObjectID + " is missing Contego content ID, set this to Invalid state.");
                }
            }

            // Check Channel content values
            if (ct == ContentType.Channel)
                return IsChannelContentComplete(parameters);

            return new ValidatorResult(true);
        }

        

        public ValidatorResult IsContentComplete(RequestParameters parameters)
        {
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;

            //ContentType
            var contentTypeProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(VODnLiveContentProperties.ContentType, StringComparison.OrdinalIgnoreCase));
            ContentType ct = (ContentType)Enum.Parse(typeof(ContentType), contentTypeProperty.Value, true);

            if (ct == ContentType.VOD || ct == ContentType.Channel)
            {
                foreach(MultipleContentService service in services) {

                    bool serviceNeedPrice = true;
                    var serviceConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == service.ObjectID.Value);
                    
                    if(serviceConfig == null)
                        return new ValidatorResult(false, "No service configuration found for service with objectId:" + service.ObjectID.Value);
                    
                    if (serviceConfig.ConfigParams.ContainsKey("ServiceNeedsPrice"))
                        bool.TryParse(serviceConfig.GetConfigParam("ServiceNeedsPrice"), out serviceNeedPrice);

                    if (serviceNeedPrice && service.Prices.Count == 0)
                    {
                        // no service price, leave it for now.
                        return new ValidatorResult(false, "content " + content.Name + " " + content.ObjectID + " doesn't have any service prices. No action will be taken for now.");
                    }
                }
            }
            ////foreach (Asset asset in content.Assets)
            ////{
            ////    if (String.IsNullOrEmpty(asset.contentAssetServerName))
            ////    {
            ////        return new ValidatorResult(false, "content " + content.Name + " " + content.ObjectID + " Asset is missing CAS.");
            ////    }
            ////}

            // check PublishInfos
            if (content.PublishInfos.Count != services.Count)
            {
                // a content must have same number of PublishInfo as number of connected servcies, this PublishInfo is used for determine contents delete sate.
                return new ValidatorResult(false, "content " + content.Name + " " + content.ObjectID + " must have same number of publishinfo as number of connected servers. PublishInfos.Count " + content.PublishInfos.Count);
            }   
            foreach(PublishInfo publishInfo in content.PublishInfos) {
                if (String.IsNullOrEmpty(publishInfo.Region))
                    return new ValidatorResult(false, "content " + content.Name + " " + content.ObjectID + " has a PublishInfo is missing a Region.");

                if (publishInfo.PublishState != PublishState.Created &&
                    parameters.Action == WorkFlowType.AddVODContent) {
                        return new ValidatorResult(false, "Create Content event has to have Created state, content:" + content.Name + " id:" + content.ID.Value + " has PublishInfo state " + publishInfo.PublishState.ToString("G") + " no workflow action will be taken. skip to next MPP event.");
                }
            }            

            return new ValidatorResult(true);
        }

        public ValidatorResult IsChannelContentComplete(RequestParameters parameters) {

            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;

            String errorPreText = "Channel content " + content.Name + " " + content.ID + " ";

            Property channelIdProperty =
                content.Properties.FirstOrDefault(p => p.Type.Equals(VODnLiveContentProperties.CubiChannelId));
            if (channelIdProperty == null || String.IsNullOrEmpty(channelIdProperty.Value) || channelIdProperty.Value.Equals("Channel:", StringComparison.OrdinalIgnoreCase))
                return new ValidatorResult(false, errorPreText + " is missing ChannelId data");

            if (content.Properties.Count(p => p.Type.Equals(VODnLiveContentProperties.RadioChannel)) == 0)
                return new ValidatorResult(false, errorPreText + " is missing RadioChannel data");

            foreach(MultipleContentService service in services)
            {
                Property lcnProperty =
                    content.Properties.SingleOrDefault(
                        p => p.Type.Equals(VODnLiveContentProperties.LCN + ":" + service.ObjectID));
                if (parameters == null || String.IsNullOrEmpty(lcnProperty.Value))
                    return new ValidatorResult(false, errorPreText + " is missing LCN for Serivce " + service.Name + " " + service.ObjectID);

                if (content.Properties.Count(p => p.Type.Equals(VODnLiveContentProperties.EnableCatchUp + ":" + service.ObjectID)) == 0)
                    return new ValidatorResult(false, errorPreText + " is missing Catchup data for Serivce " + service.Name + " " + service.ObjectID);

                if (content.Properties.Count(p => p.Type.Equals(VODnLiveContentProperties.EnableNPVR + ":" + service.ObjectID)) == 0)
                    return new ValidatorResult(false, errorPreText + " is missing NPVR data for Serivce " + service.Name + " " + service.ObjectID);
                String languageIso = service.ServiceViewMatchRules[0].ServiceViewLanugageISO;
                Int32 assetCount = content.Assets.Count(a => a.LanguageISO.Equals(languageIso, StringComparison.OrdinalIgnoreCase));
                if (assetCount == 0)
                    return new ValidatorResult(false, errorPreText + " is missing assets for Serivce " + service.Name + " " + service.ObjectID);

                var langinfo = content.LanguageInfos.FirstOrDefault(l => l.ISO.Equals(service.ServiceViewMatchRules[0].ServiceViewLanugageISO, StringComparison.OrdinalIgnoreCase));
                if (langinfo == null) {
                    return new ValidatorResult(false, errorPreText + " is missing title and image for Serivce " + service.Name + " " + service.ObjectID);
                }
                else { 
                    if (String.IsNullOrWhiteSpace(langinfo.Title))
                        return new ValidatorResult(false, errorPreText + " is missing title for Serivce " + service.Name + " " + service.ObjectID);

                    if (langinfo.Images.Count == 0)
                        return new ValidatorResult(false, errorPreText + " is missing image for Serivce " + service.Name + " " + service.ObjectID);
                }   
               
                if (ConaxIntegrationHelper.IsNPVREnabledChannel(service.ObjectID.Value, content))
                {
                    var dvbAssets = content.Assets.Where(a => CommonUtil.GetStreamType(a.Name) == StreamType.DVB && a.LanguageISO.Equals(languageIso));
                    foreach (Asset asset in dvbAssets)
                    {
                        DeviceType deviceType = ConaxIntegrationHelper.GetDeviceType(asset);
                        if (
                            content.Assets.Count(
                                a =>
                                    ConaxIntegrationHelper.GetDeviceType(a) == deviceType &&
                                    CommonUtil.GetStreamType(a.Name) == StreamType.IP && a.LanguageISO.Equals(languageIso)) == 0)
                        {
                            return new ValidatorResult(false, errorPreText + " is missing an ip source for deviceType " + deviceType + " in service " + service.Name + " " + service.ObjectID);
                        }
                    }

                    if (dvbAssets.Any())
                    {
                        Property uuidProperty = ConaxIntegrationHelper.GetUuidProperty(content, service.ObjectID.Value);
                        if (uuidProperty == null)
                        {
                            return new ValidatorResult(false,
                                errorPreText + " is missing an UUID in service " + service.Name + " " + service.ObjectID);
                        }
                        if (String.IsNullOrEmpty(uuidProperty.Value))
                        {
                            return new ValidatorResult(false,
                                errorPreText + " have a empty UUID value in service " + service.Name + " " + service.ObjectID);
                        }
                        
                    }
                }
            }

           

            return new ValidatorResult(true);
        }
    }

    public class ValidatorResult
    {
        public bool IsOK { get; set; }
        public string Message { get; set; }

        public ValidatorResult() { }

        public ValidatorResult(bool isOk, string message)
        {
            IsOK = isOk;
            Message = message;
        }

        public ValidatorResult(bool isOk)
        {
            IsOK = isOk;
        }
    }
}
