using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.EPG
{
    public class EPGParserHelper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //public static String GetChannelId(String channelName, XElement EPGChannelConfigXML)
        //{
        //    String channelId = "";
        //    foreach (XElement channelNode in EPGChannelConfigXML.XPathSelectElements("Channels/Channel"))
        //    {
        //        if (channelNode.Attribute("epgId").Value.Equals(channelName, StringComparison.OrdinalIgnoreCase))
        //        {
        //            channelId = channelNode.Attribute("cubiChannelId").Value;
        //            break;
        //        }
        //    }
        //    return channelId;
        //}
        /*
        public static UInt64 GetConaxContegoContentID(String channelName, XElement EPGChannelConfigXML)
        {
            UInt64 conaxContegoContentID = 0;
            foreach (XElement channelNode in EPGChannelConfigXML.XPathSelectElements("Channels/Channel"))
            {
                if (channelNode.Attribute("epgId").Value.Equals(channelName, StringComparison.OrdinalIgnoreCase))
                {
                    conaxContegoContentID = UInt64.Parse(channelNode.Attribute("ConaxContegoContentID").Value);
                    break;
                }
            }
            return conaxContegoContentID;
        }
        */
        public static Boolean EnableCatchUp(EPGChannel epgChannel, String channelName, XElement EPGChannelConfigXML)
        {
            Boolean enableCatchUp = false;
            //foreach (XElement channelNode in EPGChannelConfigXML.XPathSelectElements("Channels/Channel"))
            //{
            //    if (channelNode.Attribute("epgChannelId").Value.Equals(channelName, StringComparison.OrdinalIgnoreCase))
            //    {
            //        enableCatchUp = Boolean.Parse(channelNode.Attribute("enableCatchUp").Value);
            //        break;
            //    }
            //}
            enableCatchUp = epgChannel.EnableCatchUpInAnyService;
            return enableCatchUp;
        }
        public static Boolean EnableNPVR(String channelName, XElement EPGChannelConfigXML)
        {
            Boolean enableNPVR = false;
            foreach (XElement channelNode in EPGChannelConfigXML.XPathSelectElements("Channels/Channel"))
            {
                if (channelNode.Attribute("epgChannelId").Value.Equals(channelName, StringComparison.OrdinalIgnoreCase))
                {
                    enableNPVR = false;// Boolean.Parse(channelNode.Attribute("enableNPVR").Value);
                    break;
                }
            }
            return enableNPVR;
        }
        /*
        public static String GetContentRightsOwner(String channelName, XElement EPGChannelConfigXML)
        {
            foreach (XElement channelNode in EPGChannelConfigXML.XPathSelectElements("Channels/Channel"))
            {
                if (channelNode.Attribute("epgId").Value.Equals(channelName, StringComparison.OrdinalIgnoreCase))
                {
                    return channelNode.XPathSelectElement("ContentRightsOwner").Value;
                }
            }
            return null;
        }
        */
        /*
        public static String GetContentAgreement(String channelName, XElement EPGChannelConfigXML)
        {
            foreach (XElement channelNode in EPGChannelConfigXML.XPathSelectElements("Channels/Channel"))
            {
                if (channelNode.Attribute("epgId").Value.Equals(channelName, StringComparison.OrdinalIgnoreCase))
                {
                    return channelNode.XPathSelectElement("ContentAgreement").Value;
                }
            }
            return null;
        }
        */
        public static String CreateExternalId(string channelId, string programId, DateTime eventPeriodFrom)
        {
            return "010" + channelId + "-" + programId + "-" + eventPeriodFrom.ToString("yyyyMMddHHmm");// +"-" + eventPeriodTo.ToString("yyyyMMddHHmm");
        }
        public static CatchupFilterObject GetFilterObject(XElement parentElement, XElement Filters)
        {
            XElement filterNameElement = parentElement.Element("catchupfilter");
            if (filterNameElement == null)
            {
                return null;
            }

            string filterName = filterNameElement.Value;

            XElement filterElement = Filters.Elements("Filter").FirstOrDefault(x => x.Attribute("name").Value == filterName);

            if (filterElement == null)
            {
                log.Warn("Found no catchup filter named " + filterName);
                return null;
            }

            CatchupFilterObject o = new CatchupFilterObject();
            o.catchupEnabled = bool.Parse(filterElement.Attribute("catchupenabled").Value);
            o.availableHours = double.Parse(filterElement.Attribute("availablehours").Value);

            foreach (var deviceElement in filterElement.Elements("Device"))
            {
                o.devices.Add(deviceElement.Attribute("type").Value);
            }

            return o;
        }
    }


    public class CatchupFilterObject
    {
        public bool catchupEnabled;
        public double availableHours;
        public List<string> devices = new List<string>();
    }
}
