using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.Pricing;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.Globalization;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Data;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins
{
    public class CableLabsXmlTranslator
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<String> ratingsFromCableLabsXml = new List<string>();

        #region IXmlTranslate Members

        public ContentData TranslateXmlToContentData(IngestConfig ingestConfig, XmlDocument contentXml)
        {
            ContentData content = new ContentData();

            var MPPConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "MPP").SingleOrDefault();
            if (MPPConfig == null)
                throw new Exception("MPP system config is not configured in the ConaxWorkflowManagerConfig.xml.");

            // default values
            content.HostID = ingestConfig.HostID;


            XmlElement Mpp5_Id_Node = (XmlElement)contentXml.SelectSingleNode("ADI/MPP5_ID");
            if (!string.IsNullOrEmpty(Mpp5_Id_Node.InnerXml))
            {
                content.Mpp5_Id = Mpp5_Id_Node.InnerXml;
            }
            else
            {
                Console.WriteLine("no Mpp5 id is defined in ingest XML");
            }

            if (!String.IsNullOrEmpty(contentXml.BaseURI))
            {
                String tmpurl = contentXml.BaseURI.Replace("\\", "/");
                String[] tmpurls = tmpurl.Split('/');
                if (tmpurls.Length > 0)
                    content.Properties.Add(new Property("IngestXMLFileName", Path.Combine(tmpurls[tmpurls.Length - 2], tmpurls[tmpurls.Length - 1])));
            }

            ContentAgreement contentAgreement = new ContentAgreement();
            contentAgreement.Name = ingestConfig.ContentAgreement;
            content.ContentAgreements.Add(contentAgreement);
            //TODO: VOD is default type for xml ingest???
            //content.Properties.Add(new Property("ContentType", ContentType.VOD.ToString("G")));

            content.Properties.Add(new Property("EnableQA", ingestConfig.EnableQA.ToString()));
            // package metadata
            XmlElement PMAMSNode = (XmlElement)contentXml.SelectSingleNode("ADI/Metadata/AMS");

            content.ContentRightsOwner = new ContentRightsOwner();
            content.ContentRightsOwner.Name = ingestConfig.ContentRightsOwner;
            // content.Name = PMAMSNode.GetAttribute("Asset_Name");

            XmlNodeList deviceTypeNodes = contentXml.SelectNodes("ADI/Metadata/App_Data[@Name='MPS_DeviceType']");
            XmlElement IngestSourceNode = (XmlElement)contentXml.SelectSingleNode("ADI/Metadata/App_Data[@Name='MPS_IngestSource']");

            if (IngestSourceNode != null)
                content.Properties.Add(new Property("IngestSource", IngestSourceNode.GetAttribute("Value")));
            else
                content.Properties.Add(new Property("IngestSource", "CableLabs"));

            XmlElement uriProfileElement = (XmlElement)contentXml.SelectSingleNode("ADI/Metadata/App_Data[@Name='MPS_URIProfile']");
            if (uriProfileElement != null)
                content.Properties.Add(new Property("URIProfile", uriProfileElement.GetAttribute("Value")));
            else
                content.Properties.Add(new Property("URIProfile", ingestConfig.URIProfile));

            content.Properties.Add(new Property("MetadataMappingConfigurationFileName", ingestConfig.MetadataMappingConfigurationFileName));

            XmlElement channelIdElement = (XmlElement)contentXml.SelectSingleNode("ADI/Metadata/App_Data[@Name='MPS_ChannelId']");
            if (channelIdElement != null)
            {
                string channelId = channelIdElement.GetAttribute("Value");
                if (!channelId.StartsWith("Channel:", StringComparison.OrdinalIgnoreCase))
                {
                    channelId = "Channel:" + channelId;
                }
                content.Properties.Add(new Property(CatchupContentProperties.CubiChannelId, channelId));

                XmlElement LCNElement = (XmlElement)contentXml.SelectSingleNode("ADI/Metadata/App_Data[@Name='MPS_ChannelLCN']");
                if (LCNElement != null)
                {
                    content.Properties.Add(new Property("LCN", LCNElement.GetAttribute("Value")));
                }

                XmlNodeList CatchUpElements = contentXml.SelectNodes("ADI/Metadata/App_Data[@Name='MPS_CatchUpEnabledForService']");
                foreach (XmlElement CatchUpElement in CatchUpElements)
                {
                    content.Properties.Add(new Property("CubiCatchUpId", CatchUpElement.GetAttribute("Value") + ":"));
                }
                XmlNodeList NPVRElements = contentXml.SelectNodes("ADI/Metadata/App_Data[@Name='MPS_NPVREnabledForService']");
                foreach (XmlElement NPVRElement in NPVRElements)
                {
                    content.Properties.Add(new Property("CubiNPVRId", NPVRElement.GetAttribute("Value") + ":"));
                }
            }
            String contentName = "";

            foreach (XmlElement adNode in contentXml.SelectNodes("ADI/Asset/Metadata/App_Data"))
            {
                if (adNode.GetAttribute("Name").Equals("Title_Sort_Name"))
                {
                    SetLanguageInfo(content, "SortName", adNode.GetAttribute("Value"));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Title"))
                {
                    SetLanguageInfo(content, "Title", adNode.GetAttribute("Value"));
                    contentName = adNode.GetAttribute("Value");
                    content.Name = contentName;
                    continue;
                }

                if (adNode.GetAttribute("Name").Equals("Episode_Name"))
                {
                    String propertyType = VODnLiveContentProperties.EpisodeName;
                    if (!String.IsNullOrEmpty(propertyType))
                        content.Properties.Add(new Property(propertyType, adNode.GetAttribute("Value")));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Summary_Long"))
                {
                    SetLanguageInfo(content, "LongDescription", adNode.GetAttribute("Value"));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Summary_Short"))
                {
                    SetLanguageInfo(content, "ShortDescription", adNode.GetAttribute("Value"));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Rating"))
                {
                    ratingsFromCableLabsXml.Add(adNode.GetAttribute("Value"));
                    continue;
                }

                if (adNode.GetAttribute("Name").Equals("Run_Time"))
                {
                    TimeSpan runtime;
                    if (!TimeSpan.TryParse(adNode.GetAttribute("Value"), out runtime))
                        throw new Exception("Failed to parse Run_Time. Invalid time format.");
                    content.RunningTime = runtime;
                    //content.RunningTime = TimeSpan.Parse(adNode.GetAttribute("Value"));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Year"))
                {
                    UInt32 productionYear;
                    if (!UInt32.TryParse(adNode.GetAttribute("Value"), out productionYear))
                        throw new Exception("Failed to parse Year, Invalid Int value.");
                    content.ProductionYear = productionYear;
                    //content.ProductionYear = UInt32.Parse(adNode.GetAttribute("Value"));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Country_of_Origin"))
                {
                    String propertyType = VODnLiveContentProperties.Country;
                    if (!String.IsNullOrEmpty(propertyType))
                        content.Properties.Add(new Property(propertyType, adNode.GetAttribute("Value")));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Actors"))
                {
                    String propertyType = VODnLiveContentProperties.Cast;
                    if (!String.IsNullOrEmpty(propertyType))
                    {
                        var castProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(propertyType, StringComparison.OrdinalIgnoreCase));
                        if (castProperty == null)
                            content.Properties.Add(new Property(propertyType, adNode.GetAttribute("Value")));
                        else
                            castProperty.Value += ";" + adNode.GetAttribute("Value");
                    }
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Director"))
                {
                    String propertyType = VODnLiveContentProperties.Director;
                    if (!String.IsNullOrEmpty(propertyType))
                        content.Properties.Add(new Property(propertyType, adNode.GetAttribute("Value")));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Producer"))
                {
                    String propertyType = VODnLiveContentProperties.Producer;
                    if (!String.IsNullOrEmpty(propertyType))
                        content.Properties.Add(new Property(propertyType, adNode.GetAttribute("Value")));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Category"))
                {
                    String propertyType = VODnLiveContentProperties.Category;
                    if (!String.IsNullOrEmpty(propertyType))
                        content.Properties.Add(new Property(propertyType, adNode.GetAttribute("Value")));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Genre"))
                {
                    String propertyType = VODnLiveContentProperties.Genre;
                    if (!String.IsNullOrEmpty(propertyType))
                        content.Properties.Add(new Property(propertyType, adNode.GetAttribute("Value")));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Provider_Asset_ID"))
                {
                    content.ExternalID = adNode.GetAttribute("Value");
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Licensing_Window_Start"))
                {
                    DateTime eventPeriodFrom;
                    if (!DateTime.TryParse(adNode.GetAttribute("Value"), out eventPeriodFrom))
                        throw new Exception("Failed to parse Licensing_Window_Start. Invalid DateTime format.");
                    content.EventPeriodFrom = eventPeriodFrom;
                    //content.EventPeriodFrom = DateTime.Parse(adNode.GetAttribute("Value"));
                    continue;
                }
                if (adNode.GetAttribute("Name").Equals("Licensing_Window_End"))
                {
                    DateTime eventPeriodTo;
                    if (!DateTime.TryParse(adNode.GetAttribute("Value"), out eventPeriodTo))
                        throw new Exception("Failed to parse Licensing_Window_End. Invalid DateTime format.");
                    content.EventPeriodTo = eventPeriodTo;
                    //content.EventPeriodTo = DateTime.Parse(adNode.GetAttribute("Value"));
                    continue;
                }
            }

            // TODO: Move check of Licensing_Window_Start and Licensing_Window_End to publish workflow
            if (content.EventPeriodFrom == null)
            {
                log.Error("Licensing_Window_Start is missing.");
                throw new Exception("Licensing_Window_Start is missing.");
            }
            if (content.EventPeriodTo == null)
            {
                log.Error("Licensing_Window_End is missing.");
                throw new Exception("Licensing_Window_End is missing.");
            }
            log.Debug("Using contentname " + contentName);
            content.Name = contentName;
            
            

            //ASSETS


            foreach (XmlElement adNode in contentXml.SelectNodes("ADI/Asset/Asset"))
            {
               
                XmlElement typeNode = (XmlElement)adNode.SelectSingleNode("Metadata/App_Data[@Name='Type']");
                if (typeNode == null)
                    continue;
                if (typeNode.GetAttribute("Value").Equals("movie", StringComparison.OrdinalIgnoreCase) || AssetClassEquals(typeNode, "movie"))
                {
                    // map movie
                    ParseVideoAssetXML(ingestConfig, adNode, deviceTypeNodes, content, false);
                }
                if (typeNode.GetAttribute("Value").Equals("preview", StringComparison.OrdinalIgnoreCase) || AssetClassEquals(typeNode, "preview"))
                {
                    // map preview
                    ParseVideoAssetXML(ingestConfig, adNode, deviceTypeNodes, content, true);
                }
                if (typeNode.GetAttribute("Value").Equals("box cover", StringComparison.OrdinalIgnoreCase) || AssetClassEquals(typeNode, "box cover"))
                {
                    // map box cover
                    ParseImageXML(ingestConfig, adNode, content);
                    //imagecount++;
                }
                if (typeNode.GetAttribute("Value").Equals("poster", StringComparison.OrdinalIgnoreCase) || AssetClassEquals(typeNode, "poster"))
                {
                    // map box cover
                    ParseImageXML(ingestConfig, adNode, content);
                    //imagecount++;
                }
            }
            HandleRatingsFromCableLabs(content, ingestConfig);
            HandleDefaultRatings(content, ingestConfig);
            CommonUtil.AddDefaultMetaDataValue(content, VODnLiveContentProperties.Genre, ingestConfig);
            CommonUtil.AddDefaultMetaDataValue(content, VODnLiveContentProperties.Category, ingestConfig);

            return content;
        }

        private void HandleDefaultRatings(ContentData content, IngestConfig ingestConfig)
        {
            var movieRating = CommonUtil.GetRatingForContent(content, VODnLiveContentProperties.MovieRating);
            var tvRating = CommonUtil.GetRatingForContent(content, VODnLiveContentProperties.TVRating);

            if (tvRating == null && movieRating == null)
            {
                CommonUtil.AddDefaultMetaDataValue(content, VODnLiveContentProperties.MovieRating, ingestConfig);
                CommonUtil.AddDefaultMetaDataValue(content, VODnLiveContentProperties.TVRating, ingestConfig);
            }
        }
        public bool AssetClassEquals(XmlElement element, String name)
        {
            bool ret = false;
            XmlNode parentNode = element.ParentNode;
            if (parentNode != null)
            {
                XmlNode node = element.ParentNode.SelectSingleNode("AMS");
                if (node != null)
                {
                    XmlAttribute attribute = node.Attributes["Asset_Class"];
                    if (attribute != null)
                    {
                        String value = node.Attributes["Asset_Class"].Value;
                        if (value.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            log.Debug("Found " + name + " in Asset_Class");
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }
        private void ParseImageXML(IngestConfig ingestConfig, XmlElement MANode, ContentData content)
        {
          
            Image img = new Image();


            XmlElement AMSNode = (XmlElement)MANode.SelectSingleNode("Content");

            var property = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName"));
            
            if (property != null)
                img.URI = Path.Combine(Path.GetDirectoryName(property.Value), AMSNode.GetAttribute("Value"));
            else
                img.URI = AMSNode.GetAttribute("Value");

            XmlElement typeNode = (XmlElement)MANode.SelectSingleNode("Metadata/App_Data[@Name='Type']");
            img.ClientGUIName = typeNode.GetAttribute("Value");

            //XmlElement imageAspectRatioNode = (XmlElement)MANode.SelectSingleNode("Metadata/App_Data[@Name='Image_Aspect_Ratio']");
            //if (imageAspectRatioNode != null)
            //    img.Classification = imageAspectRatioNode.GetAttribute("Value");
            img.Classification = ingestConfig.DefaultImageClassification;

            foreach (LanguageInfo lang in content.LanguageInfos)
                lang.Images.Add(img);
        }
        private void ParseVideoAssetXML(IngestConfig ingestConfig, XmlElement MANode, XmlNodeList deviceTypeNodes, ContentData content, Boolean IsTraielr)
        {

            
            var deviceAndAssetMapping = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "DeviceAndAssetMapping").SingleOrDefault();
            
            if (deviceAndAssetMapping == null)
                throw new Exception("DeviceAndAssetMapping is not configured in the ConaxWorkflowManagerConfig.xml.");
            
            var deviceTypeGroups = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "DeviceTypeAssetGroups").SingleOrDefault();
            
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

            Asset defaultAsset = new Asset();
            defaultAsset.IsTrailer = IsTraielr;
            defaultAsset.DeliveryMethod = DeliveryMethod.Stream;
            //defaultAsset.contentAssetServerName = ingestConfig.CAS;


            //MPP5 ID................................................................................

            XmlElement AssetNode = (XmlElement)MANode.SelectSingleNode("Metadata/MPP5_Asset_ID");
            defaultAsset.Mpp5_Asset_Id = new Guid(AssetNode.InnerXml);
            
            XmlElement AMSNode = (XmlElement)MANode.SelectSingleNode("Content");

            bool isLiveChannel = CommonUtil.ContentIsChannel(content);

            if (!IsTraielr)
            {
                if (content.Properties.FirstOrDefault(p => p.Type == VODnLiveContentProperties.ContentType) == null)
                {
                    if (isLiveChannel)
                        content.Properties.Add(new Property(VODnLiveContentProperties.ContentType, ContentType.Live.ToString("G")));
                    else
                        content.Properties.Add(new Property(VODnLiveContentProperties.ContentType, ContentType.VOD.ToString("G")));
                }
            }

            
            var property = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName"));
            if (property != null && isLiveChannel == false)
                defaultAsset.Name = Path.Combine(Path.GetDirectoryName(property.Value), AMSNode.GetAttribute("Value"));
            else
                defaultAsset.Name = AMSNode.GetAttribute("Value");


            List<String> devicetypelist = new List<String>();
            if (deviceTypeNodes.Count > 0)
            {
                foreach (XmlElement deviceTypeNode in deviceTypeNodes)
                    devicetypelist.Add(deviceTypeNode.GetAttribute("Value"));
            }
            else
            {
                devicetypelist.AddRange(ingestConfig.Devices);
            }


            AssetFormatType liveFormatType = new AssetFormatType();
            if (isLiveChannel)
            {
                liveFormatType = CommonUtil.GetAssetFormatTypeFromFileName(defaultAsset.Name);
            }

            var deviceGroupMap = new Dictionary<string, string>();
            foreach (var deviceType in devicetypelist)
            {
                foreach (var key in deviceTypeGroups.ConfigParams.Keys)
                {
                    if (deviceTypeGroups.ConfigParams[key].ToLowerInvariant().Split(',').Contains(deviceType.ToLowerInvariant()))
                    {
                        deviceGroupMap[deviceType] = key;
                        break;
                    }
                }
            }

            Dictionary<string, Asset> assets = new Dictionary<string, Asset>();
            foreach (String deviceType in devicetypelist)
            {
                //String deviceType = deviceTypeNode.GetAttribute("Value");

                Asset asset;
                AssetFormatType formatType;
                if (!Enum.TryParse<AssetFormatType>(deviceAndAssetMapping.GetConfigParam(deviceType), out formatType))
                    throw new Exception("Failed to parse deviceAndAssetMapping for device type " + deviceType);

                if (!deviceGroupMap.ContainsKey(deviceType))
                    throw new Exception("Failed to find group for device type " + deviceType);

                bool addDeviceType = true;
                if (isLiveChannel && (liveFormatType != formatType))
                {
                    addDeviceType = false;
                    formatType = liveFormatType;
                }

                if (assets.Keys.Contains(deviceGroupMap[deviceType]))
                {
                    // add new device type to existing asset
                    asset = assets[deviceGroupMap[deviceType]];
                }
                else
                {
                    // create new asset.
                    asset = new Asset();
                    asset.IsTrailer = defaultAsset.IsTrailer;
                    asset.DeliveryMethod = defaultAsset.DeliveryMethod;
                    //asset.contentAssetServerName = defaultAsset.contentAssetServerName;
                    asset.Name = defaultAsset.Name;
                    asset.LanguageISO = defaultAsset.LanguageISO;
                    asset.FileSize = defaultAsset.FileSize;
                    asset.Properties.AddRange(defaultAsset.Properties.ToArray());
                    ConaxIntegrationHelper.AddAssetFormatTypeToAsset(asset, formatType);
                    assets.Add(deviceGroupMap[deviceType], asset);
                    asset.Mpp5_Asset_Id = defaultAsset.Mpp5_Asset_Id;

                }
                if (addDeviceType)
                {
                    asset.Properties.Add(new Property("DeviceType", deviceType));
                }
            }


            // add to content
            foreach (KeyValuePair<string, Asset> pair in assets)
                content.Assets.Add(pair.Value);

        }
        private void SetLanguageInfo(ContentData content, String type, String value)
        {

            LanguageInfo languageInfo = new LanguageInfo();
            languageInfo.ISO = "ENG";
            if (content.LanguageInfos.Count > 0)
                languageInfo = content.LanguageInfos[0];
            else
                content.LanguageInfos.Add(languageInfo);

            if (type.Equals("Title"))
                languageInfo.Title = value;

            if (type.Equals("SortName"))
                languageInfo.SortName = value;

            if (type.Equals("LongDescription"))
                languageInfo.LongDescription = value;

            if (type.Equals("ShortDescription"))
                languageInfo.ShortDescription = value;
        }
        public XmlDocument TranslatePriceDataToXml(MultipleServicePrice priceData)
        {
            throw new NotImplementedException();
        }
        #endregion
        public Dictionary<MultipleContentService, List<MultipleServicePrice>> TranslateXmlToPrices(IngestConfig ingestConfig, List<MultipleContentService> connectedServices, XmlDocument priceXml, String name)
        {
            IADIPricingRule pricingRule = ADIPricingRuleFactory.GetADIPricingRule(ingestConfig.ADIPricingRule);
            return pricingRule.GetPrice(ingestConfig, connectedServices, priceXml, name);

        }
        private void HandleRatingsFromCableLabs(ContentData content, IngestConfig ingestConfig)
        {
            foreach (var rating in ratingsFromCableLabsXml)
            {
                var contentRatingType = IsRatingTvRating(rating, ingestConfig)
                                            ? VODnLiveContentProperties.TVRating
                                            : VODnLiveContentProperties.MovieRating;

                CommonUtil.SetRatingForContent(content, rating, contentRatingType);
            }
        }
        private static bool IsRatingTvRating(String rating, IngestConfig ingestConfig)
        {
            if (ingestConfig.DefaultRatingType == VODnLiveContentProperties.MovieRating)
            {
                return (!RatingMatchesMovieRatingInMetadataMappingFile(rating, ingestConfig) &&
                        RatingMatchesTVRatingInMetadataMappingFile(rating, ingestConfig));
            }

            return (RatingMatchesTVRatingInMetadataMappingFile(rating, ingestConfig) ||
                        !RatingMatchesMovieRatingInMetadataMappingFile(rating, ingestConfig));

        }
        private static bool RatingMatchesMovieRatingInMetadataMappingFile(String rating, IngestConfig ingestConfig)
        {
            return (MetadataMappingHelper.DoesValueMatchFromValueInMetadataMappingFile(
               ingestConfig.MetadataMappingConfigurationFileName, rating, VODnLiveContentProperties.MovieRating));
        }
        private static bool RatingMatchesTVRatingInMetadataMappingFile(String rating, IngestConfig ingestConfig)
        {
            return (MetadataMappingHelper.DoesValueMatchFromValueInMetadataMappingFile(
               ingestConfig.MetadataMappingConfigurationFileName, rating, VODnLiveContentProperties.TVRating));
        }
    }
}
