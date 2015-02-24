using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ConaxContegoOnDemandContentService;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ConaxContegoPPVProductService;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Policy;
using System.Net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    public class ConaxContegoServicesWrapper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static OnDemandContentWebService onDemandContentWebService = new OnDemandContentWebService();
        private static PpvProductWebService ppvProductWebService = new PpvProductWebService();

        public ConaxContegoServicesWrapper() {
            //System.Net.ServicePointManager.CertificatePolicy = new TrustAllCertificatePolicy();
            SSLValidator.OverrideValidation(); 
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxContego").SingleOrDefault();

            onDemandContentWebService.Url = systemConfig.GetConfigParam("OnDemandContentService");
            ppvProductWebService.Url = systemConfig.GetConfigParam("PPVProductService");

            String userName = systemConfig.GetConfigParam("UserName");
            String password = systemConfig.GetConfigParam("Password");

            onDemandContentWebService.Credentials = new NetworkCredential(userName, password);
            onDemandContentWebService.PreAuthenticate = true;

            ppvProductWebService.Credentials = new NetworkCredential(userName, password);
            ppvProductWebService.PreAuthenticate = true;
        }


        /// <summary>
        /// This method registers a VOD Content in Conax Contego system.
        /// </summary>
        /// <param name="contentData">The object containing the information about the content to add.</param>
        /// <returns>Returns true if the content was added successfully.</returns>
        public OnDemandContentResponseType AddVODContent(ContentData contentData, IList<MultipleServicePrice> servicePrices, TaskConfig contextConfig)
        {
            Int32[] productIDs = new Int32[servicePrices.Count];
            Int32 counter = 0;
            foreach(MultipleServicePrice servicePrice in servicePrices) {
                String pidStr = ConaxIntegrationHelper.GetConaxContegoProductID(servicePrice);
                productIDs[counter++] = Int32.Parse(pidStr);
            }

            OnDemandContentUpdateRequestType ODMRequestType = new OnDemandContentUpdateRequestType();
            OnDemandContentType ODMContentType = new OnDemandContentType();
            OttParameterType OTTParam = new  OttParameterType();

            var workFlowConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            XmlDocument deviceMapXML = new XmlDocument();
            deviceMapXML.Load(workFlowConfig.GetConfigParam("ConaxContegoDeviceMapXML"));

            String deviceProfileName = ConaxIntegrationHelper.GetConaxContegoDeviceProfile(contentData, deviceMapXML);
            if (String.IsNullOrEmpty(deviceProfileName)) {
                throw new Exception("Conax contego device profile name not found.");
            }

            String maturityRating = ConaxIntegrationHelper.GetConaxContegoMaturityRating(contentData);
            String uriProfile = ConaxIntegrationHelper.GetURIProfile(contentData);

            OTTParam.OttDeviceProfile = deviceProfileName;
            ODMContentType.Name = contentData.Name;
            ODMContentType.Description = contentData.Name;
            ODMContentType.OfferedBy = productIDs;
            ODMContentType.MaturityRating = maturityRating;
            ODMContentType.URIProfile = uriProfile;
            ODMContentType.OttParams = OTTParam;
            ODMRequestType.OnDemandContent = ODMContentType;
            
            OnDemandContentResponseType result = onDemandContentWebService.CreateOnDemandContent(ODMRequestType);
            
            return result;
        }

        /// <summary>
        /// This method updates a VOD Content in Conax Contego system.
        /// </summary>
        /// <param name="contentData">The object containing the information about the content to update.</param>
        /// <returns>Returns true if the content was updated successfully.</returns>
        public OnDemandContentResponseType UpdateVODContent(ContentData contentData, IList<MultipleServicePrice> servicePrices, TaskConfig contextConfig)
        {
            
            Int32[] productIDs = new Int32[servicePrices.Count];
            Int32 counter = 0;
            foreach (MultipleServicePrice servicePrice in servicePrices)
            {
                String pidStr = ConaxIntegrationHelper.GetConaxContegoProductID(servicePrice);
                if (String.IsNullOrEmpty(pidStr))
                    throw new Exception("ConaxContego ProductID is missing for MPP service price: " + servicePrice.Title + " ID:" + servicePrice.ID.Value.ToString());
                productIDs[counter++] = Int32.Parse(pidStr);
            }

            OnDemandContentUpdateRequestType ODMRequestType = new OnDemandContentUpdateRequestType();
            OnDemandContentType ODMContentType = new OnDemandContentType();
            OttParameterType OTTParam = new OttParameterType();

            var workFlowConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            XmlDocument deviceMapXML = new XmlDocument();
            deviceMapXML.Load(workFlowConfig.GetConfigParam("ConaxContegoDeviceMapXML"));

            String deviceProfileName = ConaxIntegrationHelper.GetConaxContegoDeviceProfile(contentData, deviceMapXML);
            if (String.IsNullOrEmpty(deviceProfileName))
            {
                throw new Exception("Conax contego device profile name not found.");
            }

            String conaxContegoContentID = ConaxIntegrationHelper.GetConaxContegoContentID(contentData);
            if (String.IsNullOrEmpty(conaxContegoContentID)) {
                throw new Exception("conaxContego ContentID is missing for MPP content:" + contentData.Name + " ID:" + contentData.ID.Value.ToString());
            }

            String maturityRating = ConaxIntegrationHelper.GetConaxContegoMaturityRating(contentData);
            String uriProfile = ConaxIntegrationHelper.GetURIProfile(contentData);

            OTTParam.OttDeviceProfile = deviceProfileName;
            ODMContentType.ContentId = conaxContegoContentID;
            ODMContentType.Name = contentData.Name;
            ODMContentType.Description = contentData.Name;
            ODMContentType.OfferedBy = productIDs;
            ODMContentType.MaturityRating = maturityRating;
            ODMContentType.URIProfile = uriProfile;
            ODMContentType.OttParams = OTTParam;
            ODMRequestType.OnDemandContent = ODMContentType;

            OnDemandContentResponseType result = onDemandContentWebService.UpdateOnDemandContent(ODMRequestType);

            return result;
        }

        /// <summary>
        /// This method updates a content prices in Conax Contego system.
        /// </summary>
        /// <param name="servicePrice">The object containing the information about the price to update.</param>
        /// <returns>Returns true if the content was updated successfully.</returns>
        public PpvProductResponseType UpdateContentPrice(MultipleServicePrice servicePrice, TaskConfig contextConfig)
        {
            String pidStr = ConaxIntegrationHelper.GetConaxContegoProductID(servicePrice);
            if (String.IsNullOrEmpty(pidStr))
                throw new Exception("ConaxContego ProductID is missing for MPP service price: " + servicePrice.Title + " ID:" + servicePrice.ID.Value.ToString());


            PpvProductUpdateRequestType ppvRequestType = new PpvProductUpdateRequestType();
            RentalPpvProductType rentalProduct = new RentalPpvProductType();

            rentalProduct.Name = servicePrice.Title;
            rentalProduct.RentalTimeInMinutes = (Int32)servicePrice.ContentLicensePeriodLengthInUnit(LicensePeriodUnit.Hours) * 60;
            rentalProduct.ProductId = int.Parse(pidStr);
            log.Debug("Setting ProductId to " + pidStr + " in call");
            rentalProduct.ProductIdSpecified = true;
            PriceType[] pts = new PriceType[1];
            pts[0] = new PriceType();
            pts[0].Amount = (double)servicePrice.Price;
            pts[0].CurrencyCode = servicePrice.Currency;
            rentalProduct.LocalizedPrices = pts;
            ppvRequestType.PpvProduct = rentalProduct;
            

            PpvProductResponseType result = ppvProductWebService.UpdatePpvProduct(ppvRequestType);

            return result;
        }      

        /// <summary>
        /// This method deletes a VOD Content in Conax Contego system.
        /// </summary>
        /// <param name="contentID">The ID of the content to delete.</param>
        /// <returns>Returns true if the content was deleted successfully.</returns>
        public OnDemandContentResponseType DeleteVODContent(String contentID)
        {
            RemoveOnDemandContentRequestType removeOnDemandContentRequestType = new RemoveOnDemandContentRequestType();
            removeOnDemandContentRequestType.ContentId = contentID;

            OnDemandContentResponseType result = onDemandContentWebService.RemoveOnDemandContent(removeOnDemandContentRequestType);
            return result;
        }

        public PpvProductResponseType AddServicePrice(MultipleServicePrice servicePrice)
        {

            PpvProductUpdateRequestType ppvRequestType = new PpvProductUpdateRequestType();
            RentalPpvProductType rentalProduct = new RentalPpvProductType();

            rentalProduct.Name = servicePrice.Title;
            rentalProduct.RentalTimeInMinutes = (Int32)servicePrice.ContentLicensePeriodLengthInUnit(LicensePeriodUnit.Hours) * 60;
            PriceType[] pts = new PriceType[1];
            pts[0] = new PriceType();
            pts[0].Amount = (double)servicePrice.Price;
            pts[0].CurrencyCode = servicePrice.Currency;
            rentalProduct.LocalizedPrices = pts;
            ppvRequestType.PpvProduct = rentalProduct;
            
            PpvProductResponseType ppvResponseType = ppvProductWebService.CreatePpvProduct(ppvRequestType);
            return ppvResponseType;
        }

        public PpvProductResponseType DeleteServicePrice(MultipleServicePrice servicePrice)
        {

            String conaxContegoProductID = ConaxIntegrationHelper.GetConaxContegoProductID(servicePrice);
            RemovePpvProductRequestType removePpvProductRequestType = new RemovePpvProductRequestType();
            removePpvProductRequestType.ProductId = Int32.Parse(conaxContegoProductID);

            PpvProductResponseType result = ppvProductWebService.RemovePpvProduct(removePpvProductRequestType);
            return result;
        }
       
    }
}
