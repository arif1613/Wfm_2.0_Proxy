using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.Pricing
{
    public class ADIPricingRuleFactory
    {
        public static IADIPricingRule GetADIPricingRule(ADIPricingRuleType ruleType)
        {
            switch (ruleType) { 
                case ADIPricingRuleType.FS_ONLY:
                    return new FSOnlyADIPricingRule();
                case ADIPricingRuleType.ADI_ONLY:
                    return new ADIOnlyADIPricingRule();
                case ADIPricingRuleType.ADI_Default_FS:
                    return new ADIOverrideADIPricingRule();
                default:
                    throw new NotImplementedException("ADIPricingRuleType " + ruleType.ToString() + " is not implemented.");
            }
        }
    }
}
