using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class MultipleContentService
    {
        public MultipleContentService() {
            Prices = new List<MultipleServicePrice>();
            ServiceViewMatchRules = new List<ServiceViewMatchRule>();
        }

        public UInt64? ID { get; set; }
        public UInt64? ObjectID { get; set; }
        public String Name { get; set; }

        public List<ServiceViewMatchRule> ServiceViewMatchRules { get; set; }
        public List<MultipleServicePrice> Prices { get; set; }
    }
}
