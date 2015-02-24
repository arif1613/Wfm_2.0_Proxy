using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Translation
{
    public class XmlUtil
    {
        public static string UnescapeXML(string s)
        {
            if (String.IsNullOrEmpty(s))
                return "";
            string returnString = s;
            
            returnString = returnString.Replace("&apos;", "'");

            returnString = returnString.Replace("&quot;", "\"");

            returnString = returnString.Replace("&gt;", ">");

         //   returnString = returnString.Replace("&lt;", "<");

            returnString = returnString.Replace("&amp;", "&");
        
            return returnString;

        }

        public static string RemoveSpecialChars(string s)
        {
            if (String.IsNullOrEmpty(s))
                return "";
            string returnString = s;
            returnString = returnString.Replace("<", "&lt;");

            returnString = returnString.Replace("&", "&amp;");

            return returnString;

        }
    }
}
