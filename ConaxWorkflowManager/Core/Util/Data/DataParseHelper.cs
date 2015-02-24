using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Data
{
    /// <summary>
    /// Class that helps with parsing of data, for example prices
    /// </summary>
    public class DataParseHelper
    {
        /// <summary>
        /// Returns the priceString as a decimal
        /// </summary>
        /// <param name="priceString">The price as a string</param>
        /// <returns>The parsed price</returns>
        public static decimal ParsePrice(String priceString)
        {
            priceString = priceString.Replace(',', '.');
            return Decimal.Parse(priceString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }
    }
}
