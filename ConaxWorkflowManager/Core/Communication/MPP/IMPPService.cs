using System;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.MPP
{
    public interface IMPPService
    {
        #region contentService
        String GetContentForObjectId(String accountId, Int64 objectId);

        String GetContentForId(String accountId, Int64 contentId);

        String AddContent(String accountId, String contentMetadataXML);

        String GetContentForExternalId(String accountId, String serviceName, String externalId);

        String DeleteContent(String accountId, Int64 contentId);

        String UpdateContent3(String accountId, String contentMetadataXML);

        String UpdateContentSet(String accountId, String updateContentSetMetadataXML);

        String UpdateContentProperties(String accountId, String contentPropertiesUpdateXML);
       
        String UpdateContentLimited(String accountId, String contentPropertiesUpdateXML);

        String GetContentPrices(String accountId, UInt64 contentObjectId, UInt64 serviceObjectId);

        String UpdateAsset(String accountId, String updateAssetXML);

        String GetContentsAvailableForPrice(String accountId, UInt64 priceId);

        String GetContent(String accountId, String contentSearchParamsXML, Boolean includeMPPContext);

        String GetContentFromProperties(String accountId, String contentSearchParamsXML, Boolean includeMPPContext);

        String GetOngoingEpgs(String accountId, String xmlWithIdsOfEpgsToIgnore, String channelId, String eventDateTo,
            int processIntervalInMinutes, bool zipReply);

        String GetContentRightsOwners(String accountId);
        #endregion

        #region serviceService
        String GetMultipleServicePriceByPriceID(String accountId, UInt64 priceId);

        String GetMultipleServicePrice(String accountId, UInt64 priceObjectId);

        String GetEventsFromSink(String accountId, UInt64 lastEventObjectId, ulong idOfUserToIgnore);

        String UpdateServicePrice(String accountId, UInt64 servicePriceId, String MultipleServicePriceXML);

        String GetServicesIncludedInContentAgreement(String accountId, String contentAgreementName);

        String GetServiceForObjectId(String accountId, UInt64 serviceObjectId, Boolean includeContentInfo,
                                            Boolean includeServiceviewData);

        String DeleteServicePrice(String accountId, UInt64 servicePriceId);

        String CreateServicePrice(String accountId, String multipleServicePriceXML, UInt64 serviceId);

        String SetSingleContentServicePrice2(String accountId, String servicePriceId, String contentId,
                                                    Decimal price);

        String GetServiceForId2(String accountId, Int64 serviceId, Boolean includeContentInfo,
                                       Boolean includeServiceViewData, Boolean includeContentAgreement);

        #endregion

        #region mppUserService

        String GetMPPUserAccountInfo(String accountId);

        #endregion

        void SetTimeout(int timeoutInSeconds);
    }
}
