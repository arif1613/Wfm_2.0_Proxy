using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest
{
    public class IngestConfig
    {
        public IngestConfig() {
            Devices = new List<string>();
            ServicePrices = new Dictionary<PriceMatchParameter, List<MultipleServicePrice>>();
            DefaultServicePrices = new Dictionary<String, MultipleServicePrice>();
            IngestXMLTypes = new List<String>();
            MetaDataDefaultValues = new Dictionary<String, String>();
            ADIPricingRule = ADIPricingRuleType.FS_ONLY;
        }
        public Boolean EnableQA { get; set; }
        public String URIProfile { get; set; }
        public ADIPricingRuleType ADIPricingRule { get; set; }
        public List<String> IngestXMLTypes { get; set; }
        public String HostID { get; set; }
        ////public String CAS { get; set; }
        //public String Region { get; set;}
        public String ContentRightsOwner { get; set; }
        public String ContentAgreement { get; set; }
        public String DefaultImageFileName { get; set; }
        public String DefaultImageClientGUIName { get; set; }
        public String DefaultImageClassification { get; set; }
        public String DefaultRatingType { get; set; }

        public List<String> Devices { get; set; }
        public String MetadataMappingConfigurationFileName { get; set; }

        public Dictionary<String, String> MetaDataDefaultValues { get; set; }
        public Dictionary<String, MultipleServicePrice> DefaultServicePrices { get; set; }
        public Dictionary<PriceMatchParameter, List<MultipleServicePrice>> ServicePrices { get; set; }

        public List<MultipleServicePrice> FindPricesForService(UInt64 serviceObjId, List<String> categories) {
            List<MultipleServicePrice> prices = new List<MultipleServicePrice>();

            //regexp match
            foreach (String category in categories) {                
                foreach (KeyValuePair<PriceMatchParameter, List<MultipleServicePrice>> kvp in this.ServicePrices)
                {
                    if (kvp.Key.Category.Equals("*"))
                        continue; // skip *, it's a fallback one. can use for regexp object.
                    Regex regex = new Regex(kvp.Key.Category);
                    if (regex.IsMatch(category) &&
                        kvp.Key.Service.ObjectID == serviceObjId) {
                        return kvp.Value;
                    }
                }
            }

            var matchPrices2 = this.ServicePrices.FirstOrDefault(s => s.Key.Service.ObjectID == serviceObjId &&
                                                                 s.Key.Category.Equals("*", StringComparison.OrdinalIgnoreCase));
            if (matchPrices2.Key != null)
                return matchPrices2.Value;

            //  regexp matach
            foreach (String category in categories) {
                foreach(KeyValuePair<String, MultipleServicePrice> kvp in this.DefaultServicePrices) {

                    if (kvp.Key.Equals("*"))
                        continue; // skip *, it's a fallback one. can use for regexp object.
                    Regex regex = new Regex(kvp.Key);
                    if (regex.IsMatch(category)) {
                        prices.Add(kvp.Value);
                        return prices;
                    }
                }
            }

            var dmatchPrices2 = this.DefaultServicePrices.FirstOrDefault(s => s.Key.Equals("*", StringComparison.OrdinalIgnoreCase));
            if (dmatchPrices2.Key != null) {
                prices.Add(dmatchPrices2.Value);
                return prices;
            }

            return prices;
        }
    } 
}
