using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Data;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.Pricing
{
    public class ADIOverrideADIPricingRule : BaseADIPricingRule, IADIPricingRule
    {

        public Dictionary<MultipleContentService, List<MultipleServicePrice>> GetPrice(IngestConfig ingestConfig, List<MultipleContentService> connectedServices, XmlDocument priceXml, String name)
        {
            var mppConfig = (MPPConfig)Config.GetConfig().SystemConfigs.First(c => c.SystemName == SystemConfigNames.MPP);
            Dictionary<MultipleContentService, List<MultipleServicePrice>> prices = new Dictionary<MultipleContentService, List<MultipleServicePrice>>();


            Decimal? price = GetPriceFromXML(priceXml);
            Int64? periodLenght = GetViewingLengthFromXML(priceXml);


            // creaet default content price object
            MultipleServicePrice contentPrice = null;
            if (price.HasValue && periodLenght.HasValue) {
                contentPrice = new MultipleServicePrice();
                contentPrice.Price = price.Value;
                // in hours
                contentPrice.ContentLicensePeriodLength = periodLenght.Value;
                contentPrice.ContentLicensePeriodLengthTime = LicensePeriodUnit.Hours;
                contentPrice.Currency = mppConfig.DefaultCurrency;
                contentPrice.Title = name;
            }

            // load the categories from the xml
            List<String> categories = new List<String>();
            XmlNodeList categoryNodes = priceXml.SelectNodes("ADI/Asset/Metadata/App_Data[@Name='Category']");
            foreach (XmlElement categoryNode in categoryNodes)
                categories.Add(categoryNode.GetAttribute("Value"));
            if (categories.Count == 0)
            {
                if (ingestConfig.MetaDataDefaultValues.ContainsKey(VODnLiveContentProperties.Category))
                    categories.Add(ingestConfig.MetaDataDefaultValues[VODnLiveContentProperties.Category]);
            }

            // load price setting from FS and override them ny ADI price
            foreach (MultipleContentService connectedService in connectedServices)
            {
                MultipleContentService service = new MultipleContentService();
                service.ObjectID = connectedService.ObjectID;

                List<MultipleServicePrice> servicePrices = new List<MultipleServicePrice>();
                List<MultipleServicePrice> matchedServicePrices = ingestConfig.FindPricesForService(service.ObjectID.Value, categories);
                // duplicate prices from FS settings and override by ADI prises if exists
                foreach (MultipleServicePrice servicePrice in matchedServicePrices)
                {
                    MultipleServicePrice newPrice = new MultipleServicePrice();
                    newPrice.ID = servicePrice.ID;
                    
                    newPrice.Price = servicePrice.Price;
                    newPrice.Currency = servicePrice.Currency;
                    newPrice.ContentLicensePeriodLength = servicePrice.ContentLicensePeriodLength;
                    newPrice.ContentLicensePeriodLengthTime = servicePrice.ContentLicensePeriodLengthTime;
                    newPrice.Title = name;

                    if (!servicePrice.ID.HasValue) {  // override rental price, they don't have pre-defined price id
                        if (price.HasValue)
                            newPrice.Price = price.Value;
                        if (periodLenght.HasValue)
                            newPrice.ContentLicensePeriodLength = periodLenght.Value;
                    }

                    servicePrices.Add(newPrice);
                }

                // if there wasn't any FS prices add  default prise
                Int32 rentalPricecount = (from e in servicePrices
                                          where e.ID.HasValue == false
                                          select e).Count();
                
                if (rentalPricecount == 0 && contentPrice != null)
                    servicePrices.Add(contentPrice);

                prices.Add(service, servicePrices);
            }

            return prices;
        }
    }
}
