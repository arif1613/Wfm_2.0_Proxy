using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.RestApi
{
    public class ReplyParser
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private XmlDocument reply = null;

        private XmlNode topNode = null;

        public ReplyParser(XmlDocument replyDocument, String topNodeName)
        {
            try
            {
                reply = replyDocument;
                if (!topNodeName.StartsWith("/"))
                    topNodeName = "/" + topNodeName;
                topNode = replyDocument.SelectSingleNode(topNodeName);
            }
            catch (Exception e)
            {
                log.Error("Error loading ReplyParser", e);
            }
        }

        /// <summary>
        /// Fetches the value found in the node with the specified node
        /// </summary>
        /// <param name="nodeName">The name of the node to fetch value from.</param>
        /// <returns>The value of the node, "" if no value is found.</returns>
        public String GetValue(String nodeName)
        {
            if (!nodeName.StartsWith("/"))
                nodeName = "/" + nodeName;
            String value = "";
            try
            {
                value = topNode.SelectSingleNode(nodeName).InnerText;
            }
            catch (Exception ex)
            {
                log.Debug("No node with name " + nodeName + " was found", ex);
            }
            return value;
        }

        /// <summary>
        /// Fetches a value from a field with the specified name. If a nodeName is sent the value from of the field in the specified node is reteurned,
        /// othervise the value from the topNode is returned.
        /// </summary>
        /// <param name="fieldName">The name of the atribute to fetch the value from.</param>
        /// <param name="nodeName">If a specified node is sent the value of the attribute in that node is returned.</param>
        /// <returns></returns>
        public String GetAttribute(String attributeName, String nodeName)
        {
            String value = "";
            XmlNode valueNode = topNode;
            if (!String.IsNullOrEmpty(nodeName))
            {
                try
                {
                    if (!nodeName.StartsWith("/"))
                        nodeName = "/" + nodeName;
                    valueNode = topNode.SelectSingleNode(nodeName);
                }
                catch (Exception ex)
                {
                    log.Error("Error loading node with name " + nodeName, ex);
                }
                XmlAttribute attribute = valueNode.Attributes[attributeName];
                if (attribute != null)
                    value = attribute.Value;
              
            }
            return value;
        }
    }
}
