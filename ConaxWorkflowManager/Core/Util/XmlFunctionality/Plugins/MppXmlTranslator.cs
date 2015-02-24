using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Globalization;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using log4net;
using System.Reflection;
using System.Security;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Translation;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Data;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.Collections;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins
{
    public class MppXmlTranslator
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region IXmlTranslate Members
        static string _ingestIdentifier;
        public MppXmlTranslator()
        {
        }
        public MppXmlTranslator(string ingestIdentifier)
        {
            _ingestIdentifier = ingestIdentifier;
        }

        public XmlDocument TranslateContentDataToXml(ContentData contentData)
        {
            String xmlStr = "";
            xmlStr += "<MediaContent>";
            xmlStr += "<Identification name=\"" + SecurityElement.Escape(contentData.Name) + "\" externalId=\"" + SecurityElement.Escape(_ingestIdentifier) + "\"/>";
            xmlStr += "<Metadata>";
            xmlStr += "<EventPeriod from=\"" + ((contentData.EventPeriodFrom.HasValue) ? contentData.EventPeriodFrom.Value.ToString("yyyy-MM-dd HH:mm:ss") : "") + "\" " +
                                   "to=\"" + ((contentData.EventPeriodTo.HasValue) ? contentData.EventPeriodTo.Value.ToString("yyyy-MM-dd HH:mm:ss") : "") + "\" />";

            foreach (PublishInfo publishInfos in contentData.PublishInfos)
            {
                xmlStr += "<PublishInfo from=\"" + publishInfos.from.ToString("yyyy-MM-dd HH:mm:ss") + "\" " +
                                       "to=\"" + publishInfos.to.ToString("yyyy-MM-dd HH:mm:ss") + "\" " +
                                       "deliveryMethod=\"" + publishInfos.DeliveryMethod.ToString("G") + "\" " +
                                       "region=\"" + SecurityElement.Escape(publishInfos.Region) + "\" " +
                                       "publishState=\"" + publishInfos.PublishState.ToString("G") + "\" />";
            }

            if (contentData.ProductionYear.HasValue)
                xmlStr += "<ProductionYear>" + contentData.ProductionYear.Value + "</ProductionYear>";

            if (contentData.RunningTime.HasValue)
            {
                DateTime runtime_tmp = new DateTime(contentData.RunningTime.Value.Ticks);
                xmlStr += "<RunningTime>" + runtime_tmp.ToString("HH:mm:ss") + "</RunningTime>";
            }

            if (contentData.TemporaryUnavailable.HasValue)
            {
                xmlStr += "<TemporaryUnavailable>" + contentData.TemporaryUnavailable.ToString().ToLower() + "</TemporaryUnavailable>";
            }

            var systemConfig =
               (ConaxWorkflowManagerConfig)
                   Config.GetConfig()
                       .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            string workFolder = systemConfig.FileIngestWorkDirectory;
            foreach (Property property in contentData.Properties)
            {
                if (property.Type == "IngestXMLFileName")
                {
                    property.Value = Path.Combine(workFolder, property.Value);
                }
                xmlStr += "<Property type=\"" + property.Type + "\">" + SecurityElement.Escape(property.Value) + "</Property>";
            }

            string contenttype = contentData.Properties.FirstOrDefault(r => r.Type == "ContentType").Value.ToString();
            if (contenttype == "VOD")
            {
                string IsQA = contentData.Properties.FirstOrDefault(r => r.Type == "EnableQA").Value.ToString();
                if (IsQA == "True" || IsQA == "true")
                {
                    xmlStr += "<PublishState>" + "needs QA" + "</PublishState>";
                }
                else
                {
                    xmlStr += "<PublishState>" + "Publish" + "</PublishState>";
                }
            }
            else
            {
                string IsQA = contentData.Properties.FirstOrDefault(r => r.Type == "EnableQA").Value.ToString();
                if (IsQA == "True" || IsQA == "true")
                {
                    xmlStr += "<PublishState>" + "needs QA" + "</PublishState>";
                }
                else
                {
                    xmlStr += "<PublishState>" + "Created" + "</PublishState>";
                }
            }

            //ingest identifier
            xmlStr += "<IngestIdentifier>" + _ingestIdentifier + "</IngestIdentifier>";

            foreach (LanguageInfo languageInfo in contentData.LanguageInfos)
            {
                xmlStr += "<LanguageInfo ISO=\"" + languageInfo.ISO + "\">";
                xmlStr += "<Title>" + SecurityElement.Escape(languageInfo.Title) + "</Title>";
                if (!String.IsNullOrEmpty(languageInfo.SortName))
                    xmlStr += "<SortName>" + SecurityElement.Escape(languageInfo.SortName) + "</SortName>";
                xmlStr += "<ShortDescription><![CDATA[" + languageInfo.ShortDescription + "]]></ShortDescription>";
                xmlStr += "<LongDescription><![CDATA[" + languageInfo.LongDescription + "]]></LongDescription>";
                foreach (Image image in languageInfo.Images)
                {
                    xmlStr += "<Image ClientGUIName=\"" + image.ClientGUIName + "\" " +
                                     "Classification=\"" + image.Classification + "\">" +
                                     image.URI + "</Image>";
                }
                if (!String.IsNullOrEmpty(languageInfo.SubtitleURL))
                    xmlStr += "<SubtitleURL>" + SecurityElement.Escape(languageInfo.SubtitleURL) + "</SubtitleURL>";
                xmlStr += "</LanguageInfo>";
            }
            xmlStr += "</Metadata>";

            if (contentData.Assets.Count > 0)
            {
                xmlStr += "<Assets>";
                foreach (Asset asset in contentData.Assets)
                {
                    var encoderconfig =
                        (ElementalEncoderConfig)
                            Config.GetConfig()
                                .SystemConfigs.FirstOrDefault(r => r.SystemName == SystemConfigNames.ElementalEncoder);
                    string encoderOutFolder = encoderconfig.ElementalEncoderOutFolder;
                    xmlStr += "<VideoAsset name=\"" + SecurityElement.Escape(Path.Combine(encoderOutFolder,asset.Name)) + "\" " +
                                          "bitrate=\"" + asset.Bitrate + "\" " +
                                          "deliveryMethod=\"" + asset.DeliveryMethod.ToString("G") + "\" " +
                                          "streamPublishingPoint=\"" + asset.StreamPublishingPoint + "\" " +
                                          "codec=\"" + asset.Codec + "\" " +
                                          "filesize=\"" + asset.FileSize + "\" " +
                                          "trailer=\"" + asset.IsTrailer.ToString().ToLower() + "\" ";
                    if (asset.ObjectID.HasValue)
                    {
                        //log.Debug("Asset have ObjectId = " + asset.ObjectID.Value.ToString() + ", content= " + contentData.Name);
                        xmlStr += "objectId=\"" + asset.ObjectID.Value.ToString() + "\" ";
                    }
                    else
                    {
                        //log.Debug("Asset doesn't have ObjectId, content= " + contentData.Name);
                    }
                    if (!String.IsNullOrEmpty(asset.LanguageISO))
                        xmlStr += "languageISO=\"" + asset.LanguageISO + "\"";
                    xmlStr += ">";

                    ////if (asset.ObjectID.HasValue || !String.IsNullOrEmpty(asset.contentAssetServerName))
                    ////{
                    ////    xmlStr += "<MPPAssetContext ";
                    ////    if (asset.ObjectID.HasValue)
                    ////        xmlStr += "objectId=\"" + asset.ObjectID.Value.ToString() + "\" ";
                    ////    if (!String.IsNullOrEmpty(asset.contentAssetServerName))
                    ////        xmlStr += "contentAssetServerName=\"" + SecurityElement.Escape(asset.contentAssetServerName) + "\" ";
                    ////    xmlStr += "/>";
                    ////}

                    foreach (Property property in asset.Properties)
                    {
                        xmlStr += "<Property type=\"" + property.Type + "\">" + SecurityElement.Escape(property.Value) + "</Property>";
                    }
                    xmlStr += "</VideoAsset>";
                }
                xmlStr += "</Assets>";
            }


            if (contentData.ObjectID.HasValue || !String.IsNullOrEmpty(contentData.HostID) ||
                contentData.ContentRightsOwner != null)
            {
                xmlStr += "<MPPContentContext ";
                if (contentData.ObjectID.HasValue)
                    xmlStr += "objectId=\"" + contentData.ObjectID.Value + "\" ";
                if (!String.IsNullOrEmpty(contentData.HostID))
                    xmlStr += "hostId=\"" + contentData.HostID + "\" ";
                if (contentData.ID.HasValue)
                    xmlStr += "contentId=\"" + contentData.ID.Value + " \" ";
                xmlStr += ">";

                if (contentData.ContentRightsOwner != null)
                {
                    xmlStr += "<ContentRightsOwner ";
                    if (contentData.ContentRightsOwner.ObjectID.HasValue)
                        xmlStr += "objectID=\"" + contentData.ContentRightsOwner.ObjectID.Value.ToString() + "\" ";
                    xmlStr += ">";
                    xmlStr += SecurityElement.Escape(contentData.ContentRightsOwner.Name) + "</ContentRightsOwner>";
                }
                foreach (ContentAgreement contentAgreement in contentData.ContentAgreements)
                {
                    xmlStr += "<ContentAgreement ";
                    if (contentAgreement.ObjectID.HasValue)
                        xmlStr += "objectID=\"" + contentAgreement.ObjectID.Value.ToString() + "\" ";
                    xmlStr += ">";
                    xmlStr += SecurityElement.Escape(contentAgreement.Name) + "</ContentAgreement>";
                }
                xmlStr += "</MPPContentContext>";
            }


            xmlStr += "</MediaContent>";

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xmlStr);
            }
            catch (Exception ex)
            {
                log.Error("Failed to load XML: " + xmlStr, ex);
                throw;
            }
            return doc;
        }

        public MPPUser TranslateXmlToMPPUser(XmlDocument mppUserXML)
        {
            MPPUser mppUser = new MPPUser();

            XmlElement MPPUserInfoNode = (XmlElement)mppUserXML.SelectSingleNode("MPPUserInfo");

            mppUser.userName = MPPUserInfoNode.GetAttribute("userName");
            mppUser.Id = UInt64.Parse(MPPUserInfoNode.GetAttribute("id"));

            return mppUser;
        }

        public XmlDocument BuildContentSetMetaDataXML(UInt64 contentId, List<KeyValuePair<String, Property>> properties)
        {

            String XMLStr = "<ContentSetMetadataUpdate>";
            XMLStr += "<ContentIds>" + contentId + "</ContentIds>";
            XMLStr += "<PropertyReplace>";
            foreach (KeyValuePair<String, Property> kvp in properties)
                XMLStr += "<Property type=\"" + kvp.Value.Type + "\" method=\"" + kvp.Key + "\">" + kvp.Value.Value + "</Property>";
            XMLStr += "</PropertyReplace>";
            XMLStr += "</ContentSetMetadataUpdate>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(XMLStr);

            return doc;
        }

        public String BuildContentPropertyUpdateXMLString(UInt64 contentId, List<KeyValuePair<String, Property>> properties)
        {

            String XMLStr = "<ContentPropertiesUpdate>";
            XMLStr += "<ContentId>" + contentId + "</ContentId>";
            XMLStr += "<PropertyReplace>";
            foreach (KeyValuePair<String, Property> kvp in properties)
                XMLStr += "<Property type=\"" + kvp.Value.Type + "\" method=\"" + kvp.Key + "\">" + kvp.Value.Value + "</Property>";
            XMLStr += "</PropertyReplace>";
            XMLStr += "</ContentPropertiesUpdate>";

            return XMLStr;
        }

        public XmlDocument BuildContentPropertiesUpdateXML(
           List<UpdatePropertiesForContentParameter> contentsToUpdate)
        {
            String XMLStr = "<ContentPropertiesUpdates>";
            foreach (
                UpdatePropertiesForContentParameter contentWithProperties in
                    contentsToUpdate)
            {
                XMLStr += BuildContentPropertyUpdateXMLString(contentWithProperties.Content.ID.Value, contentWithProperties.Properties);
            }
            XMLStr += "</ContentPropertiesUpdates>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(XMLStr);
            return doc;
        }

        public List<ContentData> TranslateXmlToContentData(XmlDocument contentXml)
        {
            IFormatProvider theCultureInfo = new System.Globalization.CultureInfo("en-GB", true);
            List<ContentData> contents = new List<ContentData>();
            foreach (XmlElement mediaContentNode in contentXml.SelectNodes("ContentMetadata/MediaContent"))
            {
                ContentData contentData = new ContentData();
                //XmlNode mediaContentNode = contentXml.SelectSingleNode("ContentMetadata/MediaContent");

                XmlElement identificationNode = (XmlElement)mediaContentNode.SelectSingleNode("Identification");

                contentData.Name = XmlUtil.UnescapeXML(identificationNode.GetAttribute("name"));
                if (identificationNode.HasAttribute("externalId"))
                    contentData.ExternalID = identificationNode.GetAttribute("externalId");

                if (identificationNode.HasAttribute("created"))
                {
                    try
                    {
                        contentData.Created = DateTime.ParseExact(identificationNode.GetAttribute("created"), "yyyy-MM-dd HH:mm:ss", theCultureInfo);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                XmlElement eventPeriodNode = (XmlElement)mediaContentNode.SelectSingleNode("Metadata/EventPeriod");
                if (eventPeriodNode != null)
                {
                    contentData.EventPeriodFrom = DateTime.ParseExact(eventPeriodNode.GetAttribute("from"), "yyyy-MM-dd HH:mm:ss", theCultureInfo);
                    contentData.EventPeriodTo = DateTime.ParseExact(eventPeriodNode.GetAttribute("to"), "yyyy-MM-dd HH:mm:ss", theCultureInfo);
                }

                XmlNodeList publishInfoNodes = mediaContentNode.SelectNodes("Metadata/PublishInfo");

                if (publishInfoNodes != null)
                {
                    foreach (XmlElement publishInfoNode in publishInfoNodes)
                    {
                        PublishInfo publishInfo = new PublishInfo();
                        publishInfo.from = DateTime.ParseExact(publishInfoNode.GetAttribute("from"), "yyyy-MM-dd HH:mm:ss", theCultureInfo);
                        publishInfo.to = DateTime.ParseExact(publishInfoNode.GetAttribute("to"), "yyyy-MM-dd HH:mm:ss", theCultureInfo);
                        publishInfo.DeliveryMethod = (DeliveryMethod)Enum.Parse(typeof(DeliveryMethod), publishInfoNode.GetAttribute("deliveryMethod"), true);
                        publishInfo.Region = XmlUtil.UnescapeXML(publishInfoNode.GetAttribute("region"));
                        publishInfo.PublishState = (PublishState)Enum.Parse(typeof(PublishState), publishInfoNode.GetAttribute("publishState"), true);
                        contentData.PublishInfos.Add(publishInfo);
                    }
                }
                XmlElement productionYearNode = (XmlElement)mediaContentNode.SelectSingleNode("Metadata/ProductionYear");
                if (productionYearNode != null && !String.IsNullOrEmpty(productionYearNode.InnerText))
                    contentData.ProductionYear = UInt32.Parse(productionYearNode.InnerText);

                XmlElement runningTimeNode = (XmlElement)mediaContentNode.SelectSingleNode("Metadata/RunningTime");
                if (runningTimeNode != null && !String.IsNullOrEmpty(runningTimeNode.InnerText))
                    contentData.RunningTime = TimeSpan.Parse(runningTimeNode.InnerText);

                XmlNodeList propertyNodes = mediaContentNode.SelectNodes("Metadata/Property");
                if (propertyNodes != null)
                {
                    foreach (XmlElement propertyNode in propertyNodes)
                    {
                        Property property = new Property();
                        property.Type = propertyNode.GetAttribute("type");
                        property.Value = XmlUtil.UnescapeXML(propertyNode.InnerText);
                        contentData.Properties.Add(property);
                    }
                }
                /*
                XmlElement sortNameNode = (XmlElement)mediaContentNode.SelectSingleNode("Metadata/SortName");
                if (sortNameNode != null)
                    contentData.SortName = sortNameNode.InnerText;
                */
                XmlNodeList languageInfoNodes = mediaContentNode.SelectNodes("Metadata/LanguageInfo");
                if (languageInfoNodes != null)
                {
                    foreach (XmlElement languageInfoNode in languageInfoNodes)
                    {
                        LanguageInfo languageInfo = new LanguageInfo();
                        languageInfo.ISO = languageInfoNode.GetAttribute("ISO");

                        XmlElement titleNode = (XmlElement)languageInfoNode.SelectSingleNode("Title");
                        if (titleNode != null && !String.IsNullOrEmpty(titleNode.InnerText))
                            languageInfo.Title = XmlUtil.UnescapeXML(titleNode.InnerText);

                        XmlElement sortNameNode = (XmlElement)languageInfoNode.SelectSingleNode("SortName");
                        if (sortNameNode != null && !String.IsNullOrEmpty(sortNameNode.InnerText))
                            languageInfo.SortName = XmlUtil.UnescapeXML(sortNameNode.InnerText);

                        XmlElement shortDescriptionNode = (XmlElement)languageInfoNode.SelectSingleNode("ShortDescription");
                        if (shortDescriptionNode != null && !String.IsNullOrEmpty(shortDescriptionNode.InnerText))
                            languageInfo.ShortDescription = XmlUtil.UnescapeXML(shortDescriptionNode.InnerText);

                        XmlElement longDescriptionNode = (XmlElement)languageInfoNode.SelectSingleNode("LongDescription");
                        if (longDescriptionNode != null && !String.IsNullOrEmpty(longDescriptionNode.InnerText))
                            languageInfo.LongDescription = XmlUtil.UnescapeXML(longDescriptionNode.InnerText);

                        XmlNodeList imageNodes = languageInfoNode.SelectNodes("Image");
                        if (imageNodes != null)
                        {
                            foreach (XmlElement imageNode in imageNodes)
                            {
                                Image image = new Image();
                                image.ClientGUIName = imageNode.GetAttribute("ClientGUIName");
                                image.Classification = imageNode.GetAttribute("Classification");
                                image.URI = imageNode.InnerText;
                                languageInfo.Images.Add(image);
                            }
                        }

                        XmlElement subtitleURLNode = (XmlElement)languageInfoNode.SelectSingleNode("SubtitleURL");
                        if (subtitleURLNode != null && !String.IsNullOrEmpty(subtitleURLNode.InnerText))
                            languageInfo.SubtitleURL = XmlUtil.UnescapeXML(subtitleURLNode.InnerText);

                        contentData.LanguageInfos.Add(languageInfo);
                    }
                }

                XmlNodeList videoAssetNodes = mediaContentNode.SelectNodes("Assets/VideoAsset");
                if (videoAssetNodes != null)
                {
                    foreach (XmlElement videoAssetNode in videoAssetNodes)
                    {
                        Asset asset = new Asset();


                        asset.Name = XmlUtil.UnescapeXML(videoAssetNode.GetAttribute("name"));
                        asset.Bitrate = UInt32.Parse(videoAssetNode.GetAttribute("bitrate"));
                        asset.DeliveryMethod = (DeliveryMethod)Enum.Parse(typeof(DeliveryMethod), videoAssetNode.GetAttribute("deliveryMethod"), true);
                        asset.StreamPublishingPoint = videoAssetNode.GetAttribute("streamPublishingPoint");
                        asset.Codec = videoAssetNode.GetAttribute("codec");
                        if (!String.IsNullOrEmpty(videoAssetNode.GetAttribute("filesize")))
                            asset.FileSize = UInt64.Parse(videoAssetNode.GetAttribute("filesize"));
                        asset.IsTrailer = false;
                        if (videoAssetNode.HasAttribute("trailer"))
                            asset.IsTrailer = Boolean.Parse(videoAssetNode.GetAttribute("trailer"));
                        asset.LanguageISO = videoAssetNode.GetAttribute("languageISO");

                        if (videoAssetNode.HasAttribute("objectId"))
                        {
                            asset.ObjectID = UInt64.Parse(videoAssetNode.GetAttribute("objectId"));
                        }
                  


                        propertyNodes = videoAssetNode.SelectNodes("Property");
                        if (propertyNodes != null)
                        {
                            foreach (XmlElement propertyNode in propertyNodes)
                            {
                                Property property = new Property();
                                property.Type = propertyNode.GetAttribute("type");
                                property.Value = XmlUtil.UnescapeXML(propertyNode.InnerText);
                                asset.Properties.Add(property);
                            }
                        }
                        contentData.Assets.Add(asset);
                    }
                }

                if (contentData.Properties.Exists(p => p.Type.Equals(SystemContentProperties.NPVRAssetData, StringComparison.OrdinalIgnoreCase)))
                    contentData.AddAssetsFromAssetDataProperty(CatchupContentProperties.NPVRAssetData);
                if (contentData.Properties.Exists(p => p.Type.Equals(SystemContentProperties.CatchupAssetData, StringComparison.OrdinalIgnoreCase)))
                    contentData.AddAssetsFromAssetDataProperty(CatchupContentProperties.CatchupAssetData);

                XmlElement MPPContentContextNode = (XmlElement)mediaContentNode.SelectSingleNode("MPPContentContext");

                if (MPPContentContextNode != null)
                {
                    contentData.ObjectID = UInt64.Parse(MPPContentContextNode.GetAttribute("objectId"));
                    contentData.HostID = MPPContentContextNode.GetAttribute("hostId");
                    contentData.ID = UInt64.Parse(MPPContentContextNode.GetAttribute("contentId"));

                    XmlElement contentRightsOwnerNode = (XmlElement)MPPContentContextNode.SelectSingleNode("ContentRightsOwner");
                    ContentRightsOwner ContentRightsOwner = new ContentRightsOwner();
                    ContentRightsOwner.ObjectID = UInt64.Parse(contentRightsOwnerNode.GetAttribute("objectID"));
                    ContentRightsOwner.Name = XmlUtil.UnescapeXML(contentRightsOwnerNode.InnerText);
                    contentData.ContentRightsOwner = ContentRightsOwner;

                    XmlNodeList contentAgreementNodes = MPPContentContextNode.SelectNodes("ContentAgreement");
                    foreach (XmlElement contentAgreementNode in contentAgreementNodes)
                    {
                        ContentAgreement contentAgreement = new ContentAgreement();
                        contentAgreement.ObjectID = UInt64.Parse(contentAgreementNode.GetAttribute("objectID"));
                        contentAgreement.Name = XmlUtil.UnescapeXML(contentAgreementNode.InnerText);
                        contentData.ContentAgreements.Add(contentAgreement);
                    }
                }
                contents.Add(contentData);
            }
            return contents;
        }

        public XmlDocument TranslatePriceDataToXml(MultipleServicePrice priceData)
        {
            String xmlStr = "";
            xmlStr += "<MultipleServicePrice>";

            xmlStr += "<ServicePrice ";
            if (priceData.ID.HasValue)
                xmlStr += "Id=\"" + priceData.ID.Value + "\" ";
            xmlStr += "Price=\"" + priceData.Price.ToString("G", CultureInfo.InvariantCulture) + "\" ";
            xmlStr += "Currency=\"" + priceData.Currency + "\" ";
            xmlStr += "DeliveryMethod=\"" + priceData.DeliveryMethod.ToString("G") + "\" ";
            xmlStr += "/>";

            xmlStr += "<LicensePeriod ";
            xmlStr += "LicensePeriodLength=\"" + priceData.ContentLicensePeriodLength.ToString() + "\" ";
            xmlStr += "Entity=\"" + priceData.ContentLicensePeriodLengthTime.ToString("G") + "\" ";
            xmlStr += "Unlimited=\"true\" ";
            xmlStr += "/>";

            xmlStr += "<MetaData ";
            xmlStr += "Title=\"" + SecurityElement.Escape(priceData.Title) + "\" ";
            xmlStr += "ShortDescription=\"" + SecurityElement.Escape(priceData.ShortDescription) + "\" ";
            xmlStr += "LongDescription=\"" + SecurityElement.Escape(priceData.LongDescription) + "\" ";
            xmlStr += "SmallImage=\"" + priceData.SmallImage + "\" ";
            xmlStr += "LargeImage=\"" + priceData.LargeImage + "\" ";
            xmlStr += "/>";

            xmlStr += "</MultipleServicePrice>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlStr);
            return doc;
        }

        public MultipleServicePrice TranslateXmlToPriceData(XmlDocument priceXml)
        {
            MultipleServicePrice servicePrice = new MultipleServicePrice();

            XmlElement servicePriceNode = (XmlElement)priceXml.SelectSingleNode("MultipleServicePrice/ServicePrice");
            servicePrice.ID = UInt64.Parse(servicePriceNode.GetAttribute("Id"));
            servicePrice.Price = DataParseHelper.ParsePrice(servicePriceNode.GetAttribute("Price"));
            servicePrice.Currency = servicePriceNode.GetAttribute("Currency");
            servicePrice.DeliveryMethod = (DeliveryMethod)Enum.Parse(typeof(DeliveryMethod), servicePriceNode.GetAttribute("DeliveryMethod"), true);
            servicePrice.IsRecurringPurchase = Boolean.Parse(servicePriceNode.GetAttribute("IsRecurringPurchase"));

            // Validity is not in use here, so skiped the mapping for now.
            //XmlElement validityNode = priceXml.SelectSingleNode("MultipleServicePrice/Validity");

            XmlElement licensePeriodNode = (XmlElement)priceXml.SelectSingleNode("MultipleServicePrice/LicensePeriod");
            servicePrice.ContentLicensePeriodLength = Int64.Parse(licensePeriodNode.GetAttribute("LicensePeriodLength"));
            servicePrice.ContentLicensePeriodLengthTime = (LicensePeriodUnit)Enum.Parse(typeof(LicensePeriodUnit), licensePeriodNode.GetAttribute("Entity"), true);
            servicePrice.ContentLicensePeriodBegin = DateTime.ParseExact(licensePeriodNode.GetAttribute("LicensePeriodBegin"), "yyyy-MM-dd HH:mm:ss", null);
            servicePrice.ContentLicensePeriodEnd = DateTime.ParseExact(licensePeriodNode.GetAttribute("LicensePeriodEnd"), "yyyy-MM-dd HH:mm:ss", null);
            servicePrice.IsContentLicensePeriodLength = Boolean.Parse(licensePeriodNode.GetAttribute("Unlimited"));
            servicePrice.MaxUsage = Int32.Parse(licensePeriodNode.GetAttribute("UsageCount"));

            XmlElement licensePeriodForServiceNode = (XmlElement)priceXml.SelectSingleNode("MultipleServicePrice/LicensePeriod/LicensePeriodForService");
            servicePrice.LicensePeriodLength = Int64.Parse(licensePeriodForServiceNode.GetAttribute("LicensePeriodLength"));
            servicePrice.LicensePeriodLengthTime = (LicensePeriodUnit)Enum.Parse(typeof(LicensePeriodUnit), licensePeriodForServiceNode.GetAttribute("Entity"), true);
            servicePrice.LicensePeriodBegin = DateTime.ParseExact(licensePeriodForServiceNode.GetAttribute("LicensePeriodBegin"), "yyyy-MM-dd HH:mm:ss", null);
            servicePrice.LicensePeriodEnd = DateTime.ParseExact(licensePeriodForServiceNode.GetAttribute("LicensePeriodEnd"), "yyyy-MM-dd HH:mm:ss", null);

            XmlElement metaDataNode = (XmlElement)priceXml.SelectSingleNode("MultipleServicePrice/MetaData");
            servicePrice.Title = XmlUtil.UnescapeXML(metaDataNode.GetAttribute("Title"));
            servicePrice.ShortDescription = XmlUtil.UnescapeXML(metaDataNode.GetAttribute("ShortDescription"));
            servicePrice.LongDescription = XmlUtil.UnescapeXML(metaDataNode.GetAttribute("LongDescription"));
            servicePrice.SmallImage = metaDataNode.GetAttribute("SmallImage");
            servicePrice.LargeImage = metaDataNode.GetAttribute("LargeImage");

            return servicePrice;
        }

        #endregion

        public MultipleContentService TranslateXmlToMultipleContentService(XmlDocument serviceXML)
        {
            MultipleContentService service = new MultipleContentService();

            XmlElement multipleContentServiceNode = (XmlElement)serviceXML.SelectSingleNode("ServiceMetadata/MultipleContentService");
            service.ID = UInt64.Parse(multipleContentServiceNode.GetAttribute("id"));
            service.ObjectID = UInt64.Parse(multipleContentServiceNode.GetAttribute("objectId"));
            service.Name = XmlUtil.UnescapeXML(multipleContentServiceNode.GetAttribute("name"));

            return service;
        }

        public List<MPPStationServerEvent> TranslateXmlToMPPStationServerEvent(XmlDocument eventXML)
        {

            List<MPPStationServerEvent> result = new List<MPPStationServerEvent>();

            XmlNodeList eventNodes = eventXML.SelectNodes("StationServerEvents/Event");
            foreach (XmlElement eventNode in eventNodes)
            {
                try
                {
                    MPPStationServerEvent mppEvent = new MPPStationServerEvent();
                    mppEvent.ObjectId = UInt64.Parse(eventNode.GetAttribute("objectId"));
                    mppEvent.Occurred = DateTime.ParseExact(eventNode.GetAttribute("occurred"), "yyyy-MM-dd HH:mm:ss", null);
                    mppEvent.Type = (EventType)Enum.Parse(typeof(EventType), eventNode.GetAttribute("type"), true);
                    mppEvent.RelatedPersistentObjectId = UInt64.Parse(eventNode.GetAttribute("relatedPersistentObjectId"));
                    mppEvent.RelatedPersistentClassName = eventNode.GetAttribute("relatedPersistentClassName");
                    UInt64 userid = 0;
                    if (UInt64.TryParse(eventNode.GetAttribute("userId"), out userid))
                        mppEvent.UserId = userid;
                    mppEvent.State = WorkFlowJobState.UnProcessed;

                    result.Add(mppEvent);
                }
                catch (Exception e)
                {
                    log.Debug("Skipping this event. " + e.Message);
                }
            }

            return result;
        }

        public String TranslateForAssetUpdate(ContentData content)
        {
            String ret = "<Assets>";
            foreach (Asset asset in content.Assets)
            {
                log.Debug("update assets: " + asset.Name + " " + asset.ObjectID.ToString());
                ret += "<VideoAsset objectId=\"" + asset.ObjectID.ToString() + "\" name=\"" + SecurityElement.Escape(asset.Name) + "\"></VideoAsset>";
            }
            ret += "</Assets>";
            return ret;
        }

        public String TranslateForAssetUpdate(List<Asset> assets)
        {
            String ret = "<Assets>";
            foreach (Asset asset in assets)
            {
                ret += "<VideoAsset objectId=\"" + asset.ObjectID.ToString() + "\" >";
                foreach (Property property in asset.Properties)
                {
                    ret += "<Property type=\"" + property.Type + "\">" + property.Value + "</Property>";
                }
                ret += "</VideoAsset>";
            }
            ret += "</Assets>";
            return ret;
        }
    }
}

