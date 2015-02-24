using System;
using System.Collections.Generic;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class MultipleServicePrice
    {
        public UInt64? ID { get; set; }
        public UInt64? ObjectID { get; set; }
        public Decimal Price { get; set; }
        public String Currency { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public Boolean? IsRecurringPurchase { get; set; }
        public Int64 ContentLicensePeriodLength { get; set; }
        public LicensePeriodUnit ContentLicensePeriodLengthTime { get; set; }
        public DateTime ContentLicensePeriodBegin { get; set; }
        public DateTime ContentLicensePeriodEnd { get; set; }
        public Boolean IsContentLicensePeriodLength { get; set; }
        public Int32 MaxUsage { get; set; }
        public Int64 LicensePeriodLength { get; set; }
        public LicensePeriodUnit LicensePeriodLengthTime { get; set; }
        public DateTime LicensePeriodBegin { get; set; }
        public DateTime LicensePeriodEnd { get; set; }
        public String Title { get; set; }
        public String ShortDescription { get; set; }
        public String LongDescription { get; set; }
        public String SmallImage { get; set; }
        public String LargeImage { get; set; }
        public List<ulong> ContentsIncludedInPrice { get; set; }
        public Int64 ContentLicensePeriodLengthInUnit(LicensePeriodUnit Unit) {
            Int64 result = 0;
            // convert current to hours
            switch (ContentLicensePeriodLengthTime) { 
                case LicensePeriodUnit.Hours:
                    result = ContentLicensePeriodLength;
                    break;
                case LicensePeriodUnit.Days:
                    result = ContentLicensePeriodLength * 24;
                    break;
                case LicensePeriodUnit.Weeks:
                    result = ContentLicensePeriodLength * 24 * 7;
                    break;
                case LicensePeriodUnit.Months:
                    result = ContentLicensePeriodLength * 24 * 30;
                    break;
                case LicensePeriodUnit.Years:
                    result = ContentLicensePeriodLength * 24 * 365;
                    break;
                default:
                    throw new NotImplementedException();
                    break;
            }
            // convert to requested unit from hours
            switch (Unit)
            {
                case LicensePeriodUnit.Hours:
                    result = result;
                    break;
                case LicensePeriodUnit.Days:
                    result = result / 24;
                    break;
                case LicensePeriodUnit.Weeks:
                    result = result / (24 * 7);
                    break;
                case LicensePeriodUnit.Months:
                    result = result / (24 * 30);
                    break;
                case LicensePeriodUnit.Years:
                    result = result / (24 * 365);
                    break;
                default:
                    throw new NotImplementedException();
                    break;
            }
            return result;
        }
    }
}
