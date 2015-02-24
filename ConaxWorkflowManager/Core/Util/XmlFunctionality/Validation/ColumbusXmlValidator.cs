using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Validation
{
    public class ColumbusXmlValidator : IExternalXmlValidator
    {
        #region IExternalXmlValidator Members

        public List<XmlError> ValidateXml(XmlDocument documentToValidate)
        {
            List<XmlError> errors = new List<XmlError>();

            XmlNode packageNode = documentToValidate.SelectSingleNode("ADI/Metadata");
            XmlNode amsNode = packageNode.SelectSingleNode("AMS");
            XmlElement dataNode = null;
            XmlNodeList dataNodes = null;
            if (amsNode == null)
            {
                XmlError error = new XmlError(ColumbusContentProperties.Package_Metadata_AMS, XmlValidationErrors.MissingField, "Field is missing");
                errors.Add(error);
            }

            dataNodes = packageNode.SelectNodes("App_Data[@Name='Provider_Content_Tier']");
            foreach (XmlNode node in dataNode)
            {
               // not sure on how to check
            }


            // Fetch title info
            XmlNode titleNode = AddColumbusPropertiesHandler.GetTitleNode(documentToValidate);
            amsNode = (XmlElement)titleNode.SelectSingleNode("AMS");
            if (amsNode == null)
            {
                XmlError error = new XmlError(ColumbusContentProperties.Title_AMS, XmlValidationErrors.MissingField, "Field is missing");
                errors.Add(error);
            }
            

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Type']");
            if (dataNode == null)
            {
                XmlError error = new XmlError(ColumbusContentProperties.Title_Type, XmlValidationErrors.MissingField, "Field is missing");
                errors.Add(error);
            }
           
            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Title_Sort_Name']");
            if (dataNode == null || String.IsNullOrEmpty(dataNode.GetAttribute("Value")))
            {
                XmlError error = new XmlError(ColumbusContentProperties.Title_Sort_Name, XmlValidationErrors.MissingField, "Field is missing or empty");
                errors.Add(error);
            }
            else
            {
                String value = dataNode.GetAttribute("Value");
                if (value.Length > 22)
                {
                    XmlError error = new XmlError(ColumbusContentProperties.Title_Sort_Name, XmlValidationErrors.DataError, "Length of field to long, maximum is 22 chars");
                    errors.Add(error);
                }
            }

            dataNodes = titleNode.SelectNodes("App_Data[@Name='Subscriber_View_Limit']");
            foreach (XmlNode node in dataNodes)
            {
                if (!CheckSubscriberViewLimit(node))
                {
                    XmlError error = new XmlError(ColumbusContentProperties.Title_Subscriber_View_Limit, XmlValidationErrors.DataError, "Wrong format on field");
                    errors.Add(error);
                }
            }

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Title_Brief']");
            if (dataNode == null || String.IsNullOrEmpty(dataNode.GetAttribute("Value")))
            {
                XmlError error = new XmlError(ColumbusContentProperties.Title_Brief, XmlValidationErrors.MissingField, "Field is missing or empty");
                errors.Add(error);
            }
            else
            {
                String value = dataNode.GetAttribute("Value");
                if (value.Length > 19)
                {
                    XmlError error = new XmlError(ColumbusContentProperties.Title_Brief, XmlValidationErrors.DataError, "Length of field to long, maximum is 19 chars");
                    errors.Add(error);
                }
            }

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Title']");
            if (dataNode == null || String.IsNullOrEmpty(dataNode.GetAttribute("Value")))
            {
                XmlError error = new XmlError(ColumbusContentProperties.Title, XmlValidationErrors.MissingField, "Field is missing or empty");
                errors.Add(error);
            }
            else
            {
                String value = dataNode.GetAttribute("Value");
                if (value.Length > 128)
                {
                    XmlError error = new XmlError(ColumbusContentProperties.Title_Brief, XmlValidationErrors.DataError, "Length of field to long, maximum is 128 chars");
                    errors.Add(error);
                }
            }

            dataNodes = titleNode.SelectNodes("App_Data[@Name='Category']");
            if (dataNodes.Count == 0)
            {
                XmlError error = new XmlError(ColumbusContentProperties.Category, XmlValidationErrors.MissingField, "Categories are missing, atleast one is required");
                errors.Add(error);
            }
            foreach (XmlElement node in dataNodes)
            {
                XmlError xmlError = CheckCategoryNode(node);
                if (xmlError != null)
                    errors.Add(xmlError);
            }
              

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Summary_Long']");
            if (dataNode != null)
            {
                String value = dataNode.GetAttribute("Value");
                if (value.Length > 4096)
                {
                    XmlError error = new XmlError(ColumbusContentProperties.Title_Summary_Long, XmlValidationErrors.DataError, "Length of field to long, maximum is 4096 chars");
                    errors.Add(error);
                }
            }




            return errors;
        }

        private XmlError CheckCategoryNode(XmlElement node)
        {
            // TODO implement
            return null;
        }

        private bool CheckSubscriberViewLimit(XmlNode node)
        {
            // TODO implement
            return true;
        }

        #endregion
    }
}
