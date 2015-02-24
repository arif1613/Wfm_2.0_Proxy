using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax
{
    public class ConaxBusinessDomain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public static IngestModelValidationResult IsValidCableLabsIngestModel(IngestConfig ingestConfig, IngestItem ingestItem)
        {

            //XmlDocument orgXml = new XmlDocument();
            //orgXml = CommonUtil.LoadXML(fileInformation.Path);

            //// Parese cablelabs xml to content object 
            //CableLabsXmlTranslator translator = new CableLabsXmlTranslator();
            //ContentData content = translator.TranslateXmlToContentData(ingestConfig, orgXml);
            //CommonUtil.AddPublishInfoToContent(content, PublishState.Created);
            //List<ContentAgreement> agreemetns = mppWrapper.GetAllServicesForContent(content);
            //// a content should only be included in one agreement in the conax solution.
            //Dictionary<MultipleContentService, List<MultipleServicePrice>> servicePrices = translator.TranslateXmlToPrices(ingestConfig, agreemetns[0].IncludedServices, orgXml, content.Name);

            Dictionary<MultipleContentService, List<MultipleServicePrice>> servicePrices = ingestItem.MultipleServicePrices;
            ContentData content = ingestItem.contentData;
            ServiceConfig serviceConfig = null;
           
            foreach (KeyValuePair<MultipleContentService, List<MultipleServicePrice>> kvp in servicePrices)
            {
                bool serviceNeedPrice = true;
                serviceConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == kvp.Key.ObjectID.Value);
                if (serviceConfig != null && serviceConfig.ConfigParams.ContainsKey("ServiceNeedsPrice"))
                    bool.TryParse(serviceConfig.GetConfigParam("ServiceNeedsPrice"), out serviceNeedPrice);
                List<MultipleServicePrice> prices = kvp.Value;
                // check prices
                if (serviceNeedPrice && prices.Count == 0)
                {
                    if (ingestConfig.ADIPricingRule == ADIPricingRuleType.ADI_ONLY ||
                        ingestConfig.ADIPricingRule == ADIPricingRuleType.ADI_Default_FS)
                        return new IngestModelValidationResult(false, "Price and/or viewing length is missing for " + ingestItem.OriginalIngestXMLPath + " ingest XML.");

                    return new IngestModelValidationResult(false, "Price settings is missing in folder settings for " + ingestItem.OriginalIngestXMLPath + " ingest XML.");
                }

                // check service price.
                foreach (MultipleServicePrice price in prices)
                {
                    if (price.ID.HasValue)
                    {
                        try
                        {
                            MultipleServicePrice servicePrice = mppWrapper.GetpriceDataByID(price.ID.Value);

                            if (servicePrice == null)
                            {
                                return new IngestModelValidationResult(false, "Could not find a price with id " + price.ID.Value);
                            }

                            if (!servicePrice.IsRecurringPurchase.HasValue || !servicePrice.IsRecurringPurchase.Value)
                            {
                                return new IngestModelValidationResult(false, "Price id " + price.ID + " is not a subscription price.");
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Warn("Failed to verify price id " + price.ID.Value, ex);
                            return new IngestModelValidationResult(false, "Failed to verify price id " + price.ID.Value);
                        }
                    }
                    else
                    {
                        // check content price.
                        if (price.Price == -1 ||
                            price.ContentLicensePeriodLength == -1 ||
                            String.IsNullOrEmpty(price.Currency))
                        {
                            return new IngestModelValidationResult(false, "Content price is not complete.");
                        }
                    }
                }
            }

            foreach (Asset asset in content.Assets)
            {   // check filenames
                if (String.IsNullOrEmpty(asset.Name))
                {
                    return new IngestModelValidationResult(false, "Media file name is missing.");
                }
            }

            int noOfAssets = content.Assets.Count(a => a.IsTrailer == false);
            if (noOfAssets == 0)
            {
                return new IngestModelValidationResult(false, "Content must have at least one movie asset.");
            }


            if (content.ContentRightsOwner == null || String.IsNullOrEmpty(content.ContentRightsOwner.Name))
            {
                return new IngestModelValidationResult(false, "Content must have ContentRightsOwner specified.");
            }

            // check iamges
            Int32 imgCount = 0;
            foreach (LanguageInfo iang in content.LanguageInfos)
            {
                foreach (Image img in iang.Images)
                {
                    if (String.IsNullOrEmpty(img.URI))
                    {
                        return new IngestModelValidationResult(false, "Image file name is missing.");
                    }
                    else
                    {
                        imgCount++;
                    }
                }
            }

            if (imgCount == 0) { 
                if (String.IsNullOrEmpty(ingestConfig.DefaultImageFileName)) {
                    return new IngestModelValidationResult(false, "No image file was defined in ingest XML and no default iamge was found in the folder settings.");
                }
            } 

            return new IngestModelValidationResult(true, "");
        }
    }
}
