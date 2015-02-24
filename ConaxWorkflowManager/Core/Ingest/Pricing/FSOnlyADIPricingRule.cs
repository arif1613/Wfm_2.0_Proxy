using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Data;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.Pricing
{
    public class FSOnlyADIPricingRule : IADIPricingRule
    {
        public Dictionary<MultipleContentService, List<MultipleServicePrice>> GetPrice(IngestConfig ingestConfig, List<MultipleContentService> connectedServices, XmlDocument priceXml, String name)
        {
           
            Dictionary<MultipleContentService, List<MultipleServicePrice>> prices = new Dictionary<MultipleContentService, List<MultipleServicePrice>>();
            
            List<String> categories = new List<String>();
            XmlNodeList categoryNodes = priceXml.SelectNodes("ADI/Asset/Metadata/App_Data[@Name='Category']");
            foreach (XmlElement categoryNode in categoryNodes)
                categories.Add(categoryNode.GetAttribute("Value"));
            if (categories.Count == 0)
            {
                if (ingestConfig.MetaDataDefaultValues.ContainsKey(VODnLiveContentProperties.Category))
                    categories.Add(ingestConfig.MetaDataDefaultValues[VODnLiveContentProperties.Category]);
            }

            foreach (MultipleContentService connectedService in connectedServices)
            {
                MultipleContentService service = new MultipleContentService();
                service.ObjectID = connectedService.ObjectID;

                List<MultipleServicePrice> servicePrices = new List<MultipleServicePrice>();
                List<MultipleServicePrice> matchedServicePrices = ingestConfig.FindPricesForService(service.ObjectID.Value, categories);
                // duplicate prices
                foreach (MultipleServicePrice servicePrice in matchedServicePrices)
                {
                    MultipleServicePrice newPrice = new MultipleServicePrice();
                    newPrice.ID = servicePrice.ID;
                    newPrice.Price = servicePrice.Price;
                    newPrice.Currency = servicePrice.Currency;
                    newPrice.ContentLicensePeriodLength = servicePrice.ContentLicensePeriodLength;
                    newPrice.ContentLicensePeriodLengthTime = servicePrice.ContentLicensePeriodLengthTime;
                    newPrice.Title = name;
                    servicePrices.Add(newPrice);
                }
                prices.Add(service, servicePrices);
            }           

            return prices;
        }
    }
}
