using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.MPP
{
    public class MPPIntegrationService : IMPPService
    {
        private static ServiceService.ServiceService serviceService = new ServiceService.ServiceService();
        private static ContentService.ContentService contentService = new ContentService.ContentService();
        private static MPPUserService.MPPUserService mppUserService = new MPPUserService.MPPUserService();

        public MPPIntegrationService()
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "MPP").SingleOrDefault();

            serviceService.Url = systemConfig.GetConfigParam("ServiceService");
            contentService.Url = systemConfig.GetConfigParam("ContentService");
            mppUserService.Url = systemConfig.GetConfigParam("MPPUserService");
        }

        public void SetTimeout(int timeoutInSeconds)
        {
            serviceService.Timeout = timeoutInSeconds;
            contentService.Timeout = timeoutInSeconds;
            mppUserService.Timeout = timeoutInSeconds;
        }

        #region contentService
        public String GetContentForObjectId(String accountId, Int64 objectId)
        {
            return contentService.GetContentForObjectId(accountId, objectId);
        }

        public String GetContentForId(String accountId, Int64 contentId)
        {
            return contentService.GetContentForId(accountId, contentId);
        }

        public String AddContent(String accountId, String contentMetadataXML)
        {
            return contentService.AddContent(accountId, contentMetadataXML);
        }

        public String GetContentForExternalId(String accountId, String serviceName, String externalId)
        {
            return contentService.GetContentForExternalId(accountId, serviceName, externalId);
        }

        public String DeleteContent(String accountId, Int64 contentId)
        {
            try
            {
                return contentService.DeleteContentAndSources(accountId, contentId);
            }
            catch (Exception ex) {
                if (ex.Message.IndexOf("not found") < 0)
                    throw;
                return String.Empty;
            }
        }

        public String UpdateContent3(String accountId, String contentMetadataXML)
        {
            return contentService.UpdateContent3(accountId, contentMetadataXML);
        }

        public String UpdateContentSet(String accountId, String updateContentSetMetadataXML)
        {
            return contentService.UpdateContentSet(accountId, updateContentSetMetadataXML);
        }

        public String UpdateContentLimited(String accountId, String updateContentSetMetadataXML)
        {
            return contentService.UpdateContentLimited(accountId, updateContentSetMetadataXML);
        }

        public String UpdateContentProperties(String accountId, String contentPropertiesUpdateXML)
        {
            return contentService.UpdateContentPropertiesInBulk(accountId, contentPropertiesUpdateXML);
        }

        public String GetContentPrices(String accountId, UInt64 contentObjectId, UInt64 serviceObjectId)
        {
            return contentService.GetContentPrices(accountId, contentObjectId, serviceObjectId);
        }

        public String UpdateAsset(String accountId, String updateAssetXML)
        {
            return contentService.UpdateAsset(accountId, updateAssetXML);
        }

        public String GetContentsAvailableForPrice(String accountId, UInt64 priceId)
        {
            return contentService.GetContentsAvailableForPrice(accountId, priceId);
        }

        public String GetContent(String accountId, String contentSearchParamsXML, Boolean includeMPPContext)
        {
            return contentService.GetContent(accountId, contentSearchParamsXML, includeMPPContext);
        }

        public String GetContentFromProperties(String accountId, String contentSearchParamsXML, Boolean includeMPPContext)
        {
            return contentService.GetContentFromProperties(accountId, contentSearchParamsXML, includeMPPContext);
        }

        public String GetContentRightsOwners(String accountId)
        {
            return contentService.GetContentRightsOwners(accountId);
        }

        public String GetOngoingEpgs(String accountId, String xmlWithIdsOfEpgsToIgnore, String channelId,
            String eventDateTo, int processIntervalInMinutes, bool zipReply)
        {
            return contentService.GetOngoingEpgs(accountId, xmlWithIdsOfEpgsToIgnore, channelId, eventDateTo,
                processIntervalInMinutes, zipReply);
        }
        #endregion

        #region serviceService
        public String GetMultipleServicePriceByPriceID(String accountId, UInt64 priceId)
        {
            return serviceService.GetMultipleServicePriceByPriceID(accountId, priceId);
        }

        public String GetMultipleServicePrice(String accountId, UInt64 priceObjectId)
        {
            return serviceService.GetMultipleServicePrice(accountId, priceObjectId);
        }

        public String GetEventsFromSink(String accountId, UInt64 lastEventObjectId, ulong idOfUserToIgnore)
        {
            return serviceService.GetEventsFromSink(accountId, lastEventObjectId, idOfUserToIgnore);
        }

        public String UpdateServicePrice(String accountId, UInt64 servicePriceId, String MultipleServicePriceXML)
        {
            return serviceService.UpdateServicePrice(accountId, servicePriceId, MultipleServicePriceXML);
        }

        public String GetServicesIncludedInContentAgreement(String accountId, String contentAgreementName)
        {
            return serviceService.GetServicesIncludedInContentAgreement(accountId, contentAgreementName);
        }

        public String GetServiceForObjectId(String accountId, UInt64 serviceObjectId, Boolean includeContentInfo, Boolean includeServiceviewData)
        {
            return serviceService.GetServiceForObjectId(accountId, serviceObjectId, includeContentInfo, includeServiceviewData);
        }

        public String DeleteServicePrice(String accountId, UInt64 servicePriceId)
        {
            return serviceService.DeleteServicePrice(accountId, servicePriceId);
        }

        public String CreateServicePrice(String accountId, String multipleServicePriceXML, UInt64 serviceId)
        {
            return serviceService.CreateServicePrice(accountId, multipleServicePriceXML, serviceId);
        }

        public String SetSingleContentServicePrice2(String accountId, String servicePriceId, String contentId, Decimal price)
        {
            return serviceService.SetSingleContentServicePrice2(accountId, servicePriceId, contentId, price);
        }

        public String GetServiceForId2(String accountId, Int64 serviceId, Boolean includeContentInfo, Boolean includeServiceViewData, Boolean includeContentAgreement)
        {
            return serviceService.GetServiceForId2(accountId, serviceId, includeContentInfo, includeServiceViewData, includeContentAgreement);
        }
        #endregion

        #region mppUserService
        public String GetMPPUserAccountInfo(String accountId)
        {
            return mppUserService.GetMPPUserAccountInfo(accountId);
        }
        #endregion
    }
}
