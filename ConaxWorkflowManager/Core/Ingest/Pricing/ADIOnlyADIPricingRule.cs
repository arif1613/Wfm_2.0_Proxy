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
    public class ADIOnlyADIPricingRule : BaseADIPricingRule, IADIPricingRule
    {
        public Dictionary<MultipleContentService, List<MultipleServicePrice>> GetPrice(IngestConfig ingestConfig, List<MultipleContentService> connectedServices, XmlDocument priceXml, String name)
        {            
            var mppConfig = (MPPConfig)Config.GetConfig().SystemConfigs.First(c => c.SystemName == SystemConfigNames.MPP);

            Dictionary<MultipleContentService, List<MultipleServicePrice>> prices = new Dictionary<MultipleContentService, List<MultipleServicePrice>>();


            Decimal? price = GetPriceFromXML(priceXml);
            Int64? periodLenght = GetViewingLengthFromXML(priceXml);

            // creaet content price
            MultipleServicePrice contentPrice = null;
            if (price.HasValue && periodLenght.HasValue)
            {
                contentPrice = new MultipleServicePrice();
                contentPrice.Price = price.Value;
                // in hours
                contentPrice.ContentLicensePeriodLength = periodLenght.Value;
                contentPrice.ContentLicensePeriodLengthTime = LicensePeriodUnit.Hours;
                contentPrice.Currency = mppConfig.DefaultCurrency;
                contentPrice.Title = name;
            }

            foreach (MultipleContentService connectedService in connectedServices)
            {
                MultipleContentService service = new MultipleContentService();
                service.ObjectID = connectedService.ObjectID;
                List<MultipleServicePrice> servicePrices = new List<MultipleServicePrice>();

                if (contentPrice != null)
                    servicePrices.Add(contentPrice);
                prices.Add(service, servicePrices);
            }

            return prices;
        }
    }
}
