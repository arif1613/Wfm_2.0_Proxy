using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.IO;
using log4net;
using System.Reflection;
using System.Xml;
 
namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util
{
    public class MetadataMappingHelper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal static bool DoesValueMatchFromValueInMetadataMappingFile(String metadataMappingXMLFileName, String value, String propertyType)
        {
            XElement doc = GetMetadataMappingXMLDoc(metadataMappingXMLFileName);

            IEnumerable<XElement> mappingNodes = doc.XPathSelectElements("//" + propertyType + "/Mapping");

            foreach (XElement mappingNode in mappingNodes)
            {
                Regex regex = new Regex(mappingNode.Attribute("from").Value);
                if (regex.IsMatch(value))
                    return true;
            }

            return false;
        }

        internal static String GetMappedValueForService(String metadataMappingXMLFileName, UInt64 serviceObjectId, String originalValue, String propertyType)
        {
            XElement doc = GetMetadataMappingXMLDoc(metadataMappingXMLFileName);
            return GetMetadataMappingForService(doc, serviceObjectId, originalValue, propertyType);
        }

        // TODO: Use GetMappingForPropertyTypeInService() instead
        internal static String GetRatingForService(String metadataMappingXMLFileName, UInt64 serviceObjectId, String originalValue, String ratingType)
        {
            XElement doc = GetMetadataMappingXMLDoc(metadataMappingXMLFileName);
            return GetMetadataMappingForService(doc, serviceObjectId, originalValue, ratingType);
        }

        internal static String GetGenreForService(String metadataMappingXMLFileName, UInt64 serviceObjectId, String originalValue)
        {
            XElement doc = GetMetadataMappingXMLDoc(metadataMappingXMLFileName);
            return GetMetadataMappingForService(doc, serviceObjectId, originalValue, "Genre");
        }

        internal static String GetCategoryForService(String metadataMappingXMLFileName, UInt64 serviceObjectId, String originalValue)
        {
            XElement doc = GetMetadataMappingXMLDoc(metadataMappingXMLFileName);
            return GetMetadataMappingForService(doc, serviceObjectId, originalValue, "Category");
        }

        private static XElement GetMetadataMappingXMLDoc(String metadataMappingXMLFileName)
        {
            try
            {
                var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
                String mappnigfilePath = Path.Combine(managerConfig.GetConfigParam("MetadataMappingDirectory"), metadataMappingXMLFileName);
                XElement doc = XElement.Load(mappnigfilePath);
                return doc;
            }
            catch (Exception ex) {
                log.Warn("Failed to load " + metadataMappingXMLFileName);
                return null;
            }
        }


        private static String GetMetadataMappingForService(XElement doc, UInt64 serviceObjectId, String fromValue, String elementName)
        {
            if (String.IsNullOrEmpty(fromValue))
                return null;
            if (doc == null)
                return null;
            try
            {
                XElement serviceNode = doc.XPathSelectElement("MetadataMapping[@serviceObjectId='" + serviceObjectId.ToString() + "']");
                if (serviceNode == null)
                    return null;

                IEnumerable<XElement> mappingNodes = serviceNode.XPathSelectElements(elementName + "/" + "Mapping");
                foreach (XElement mappingNode in mappingNodes)
                {
                    Regex regex = new Regex(mappingNode.Attribute("from").Value);
                    if (regex.IsMatch(fromValue))
                        return mappingNode.Attribute("to").Value;
                }
            }
            catch (Exception ex) {
                log.Warn("Failed to find Metadata mapping for type " + elementName + " for value " + fromValue, ex);
            }
            return null;
        }
    }
}
