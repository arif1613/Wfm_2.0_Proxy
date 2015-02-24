using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Data;
using log4net;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.Pricing
{
    public abstract class BaseADIPricingRule
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Decimal? GetPriceFromXML(XmlDocument priceXml)
        {

            Decimal? price = null;            
            foreach (XmlElement adNode in priceXml.SelectNodes("ADI/Asset/Metadata/App_Data"))
            {
                if (adNode.GetAttribute("Name").Equals("Suggested_Price"))
                {
                    try
                    {
                        price = DataParseHelper.ParsePrice(adNode.GetAttribute("Value"));
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failed to parse Suggested_Price. Invalid decimal value " + adNode.GetAttribute("Value"), ex);
                        throw;
                    }
                    if (price < 0)
                        throw new Exception("Failed to set Suggested_Price. Can't set a negative price " + price);

                    return price;
                }
            }

            return price;
        }

        protected Int64? GetViewingLengthFromXML(XmlDocument priceXml)
        {
            Int64? periodLenght = null;
            foreach (XmlElement adNode in priceXml.SelectNodes("ADI/Asset/Metadata/App_Data"))
            {
                if (adNode.GetAttribute("Name").Equals("Maximum_Viewing_Length"))
                {
                    try
                    {
                        String[] viewLenght = adNode.GetAttribute("Value").Split(':');
                        Int32 DD = Int32.Parse(viewLenght[0]);
                        Int32 HH = Int32.Parse(viewLenght[1]);
                        Int32 MM = Int32.Parse(viewLenght[2]);

                        if (DD < 0 || DD > 99 ||
                            HH < 0 || HH > 99 ||
                            MM < 0 || MM > 99)
                            throw new Exception("Value " + adNode.GetAttribute("Value") + " is out of range.");

                        if (MM > 0 && MM <= 60) // MPP min time unit is in hrs, so round up if tehre is any specified minutes.
                            HH++;
                        else if (MM > 60)
                            HH += 2; 

                        TimeSpan time = new TimeSpan(DD, HH, 0, 0);
                        periodLenght = (Int64)time.TotalHours;
                        return periodLenght;
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failed to parse Maximum_Viewing_Length make sure the format is 'DD:HH:MM' " + adNode.GetAttribute("Value"), ex);
                        throw;
                    }
                }
            }

            return periodLenght;
        }
    }
}
