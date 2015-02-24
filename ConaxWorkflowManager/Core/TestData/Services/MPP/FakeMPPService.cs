using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.MPP;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.TestData.Services.MPP
{
    public class FakeMPPService : IMPPService
    {
        private static Dictionary<UInt64, String> contentByObjId = new Dictionary<UInt64, String>();
        private static Dictionary<UInt64, Asset> assetByObjId = new Dictionary<UInt64, Asset>();
        static MppXmlTranslator translator = new MppXmlTranslator();

        public void AddContentToMemory(UInt64 id, String fileName)
        {
            String xmlString = GetTestData(fileName);
            contentByObjId.Add(id, xmlString);
        }

        public void CleanMemory()
        {
            contentByObjId = new Dictionary<UInt64, String>();
            assetByObjId = new Dictionary<UInt64, Asset>();
        }

        public List<Asset> GetAssetsFromMemory()
        {
            List<Asset> res = new List<Asset>();
            foreach (KeyValuePair<UInt64, Asset> kvp in assetByObjId)                
                res.Add(kvp.Value);
            
            return res;
        }

        public string GetContentForObjectId(string accountId, long objectId)
        {
            if (contentByObjId.ContainsKey((UInt64)objectId))
                return contentByObjId[(UInt64) objectId];
            else if (objectId == 621)
                return GetTestData("channel_content_621.xml");
            else
                return "";
        }

        public string GetContentForId(string accountId, long contentId)
        {
            return GetTestData("channel_content_621.xml");
        }

        public string AddContent(string accountId, string contentMetadataXML)
        {
            throw new NotImplementedException();
        }

        public string GetContentForExternalId(string accountId, string serviceName, string externalId)
        {
            foreach (KeyValuePair<UInt64, String> kvp in contentByObjId)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(kvp.Value);
                ContentData content = translator.TranslateXmlToContentData(doc)[0];
                if (content.ExternalID == externalId)
                    return kvp.Value;
            }
            return "";
        }

        public string DeleteContent(string accountId, long contentId)
        {
            throw new NotImplementedException();
        }

        public string UpdateContent3(string accountId, string contentMetadataXML)
        {
            
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(contentMetadataXML);
            List<ContentData> contents = translator.TranslateXmlToContentData(doc);
            foreach (var contentData in contents)
            {
                if (contentData.ObjectID.HasValue)
                {

                    var newAssets = contentData.Assets.Where(a => a.ObjectID.HasValue == false);
                    UInt64 id = 1;
                    foreach (Asset newAsset in newAssets)
                    {
                        newAsset.ObjectID = id++;
                    }

                    String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><ContentMetadata>";
                    xmlString += translator.TranslateContentDataToXml(contentData).InnerXml;
                    xmlString += "</ContentMetadata>";

                    if (!contentByObjId.ContainsKey(contentData.ObjectID.Value))
                        contentByObjId.Add(contentData.ObjectID.Value, xmlString);
                    else
                        contentByObjId[contentData.ObjectID.Value] = xmlString;
                }
            }
            

            return "";
        }

        public string UpdateContentSet(string accountId, string updateContentSetMetadataXML)
        {
            XmlDocument updatedoc = new XmlDocument();
            updatedoc.LoadXml(updateContentSetMetadataXML);

            XmlNode idNode = updatedoc.SelectSingleNode("ContentSetMetadataUpdate/ContentIds");
            UInt64 contentId = UInt64.Parse(idNode.InnerText);
            if (contentByObjId.ContainsKey(contentId))
            {
                lock (contentByObjId[contentId])
                {
                    XmlDocument contentDoc = new XmlDocument();
                    contentDoc.LoadXml(contentByObjId[contentId]);

                    XmlNodeList propertyNodes = updatedoc.SelectNodes("ContentSetMetadataUpdate/PropertyReplace/Property");
                    foreach (XmlNode propertyNode in propertyNodes)
                    {

                        if (propertyNode.Attributes["method"].Value == "ADD")
                        {
                            XmlElement newPropertyNode = contentDoc.CreateElement("Property");
                            newPropertyNode.SetAttribute("type", propertyNode.Attributes["type"].Value);
                            newPropertyNode.InnerText = propertyNode.InnerText;

                            XmlNode metadataNode = contentDoc.SelectSingleNode("ContentMetadata/MediaContent/Metadata");
                            metadataNode.AppendChild(newPropertyNode);

                        }
                        else if (propertyNode.Attributes["method"].Value == "DELETE") {
                            String proerptyQuery = "ContentMetadata/MediaContent/Metadata/Property[@type='" +
                                                   propertyNode.Attributes["type"].Value + "']";

                            XmlNodeList propertyNodeList = contentDoc.SelectNodes(proerptyQuery);
                            foreach(XmlNode pnode in propertyNodeList) {
                                if (pnode.InnerText.Equals(propertyNode.InnerText))
                                {
                                    XmlNode metadataNode = contentDoc.SelectSingleNode("ContentMetadata/MediaContent/Metadata");
                                    metadataNode.RemoveChild(pnode);
                                    break;
                                }
                            }

                        } else {
                            String proerptyQuery = "ContentMetadata/MediaContent/Metadata/Property[@type='" +
                                                   propertyNode.Attributes["type"].Value + "']";
                            XmlNode contentPropertyNode = contentDoc.SelectSingleNode(proerptyQuery);
                            contentPropertyNode.InnerText = propertyNode.InnerText;
                        }
                    }

                    contentByObjId[contentId] = contentDoc.InnerXml;
                }
            }

            return "";
        }

        public string GetContentPrices(string accountId, ulong contentObjectId, ulong serviceObjectId)
        {
            throw new NotImplementedException();
        }

        public string UpdateAsset(string accountId, string updateAssetXML)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(updateAssetXML);

            foreach (XmlNode assetNode in doc.SelectNodes("Assets/VideoAsset"))
            {
                UInt64 id = UInt64.Parse(assetNode.Attributes["objectId"].Value);
                Asset asset = new Asset();
                asset.ObjectID = id;
                foreach (XmlNode propertyNode in assetNode.SelectNodes("Property"))                
                    asset.Properties.Add(new Property(propertyNode.Attributes["type"].Value, propertyNode.InnerText));

                if (assetByObjId.ContainsKey(id))
                    assetByObjId[id] = asset;
                else
                    assetByObjId.Add(id, asset);
                
            }
            
            return "";
        }

        public string GetContentsAvailableForPrice(string accountId, ulong priceId)
        {
            throw new NotImplementedException();
        }

        public string GetContent(string accountId, string contentSearchParamsXML, bool includeMPPContext)
        {
            //return GetTestData("content_622.xml");
            return contentByObjId.First().Value;
        }

        public String GetContentFromProperties(String accountId, String contentSearchParamsXML, Boolean includeMPPContext) { 
            return GetContent(accountId, contentSearchParamsXML, includeMPPContext);
        }

        public string GetContentRightsOwners(string accountId)
        {
            throw new NotImplementedException();
        }

        public string GetMultipleServicePriceByPriceID(string accountId, ulong priceId)
        {
            throw new NotImplementedException();
        }

        public string GetMultipleServicePrice(string accountId, ulong priceObjectId)
        {
            throw new NotImplementedException();
        }

        public string GetEventsFromSink(string accountId, ulong lastEventObjectId, ulong idOfUserToIgnore)
        {
            throw new NotImplementedException();
        }

        public string UpdateServicePrice(string accountId, ulong servicePriceId, string MultipleServicePriceXML)
        {
            throw new NotImplementedException();
        }

        public string GetServicesIncludedInContentAgreement(string accountId, string contentAgreementName)
        {            
            return GetTestData("content_agreement_Live_Multi2.xml");
        }

        public string GetServiceForObjectId(string accountId, ulong serviceObjectId, bool includeContentInfo, bool includeServiceviewData)
        {
            return GetTestData("service_2.xml");
        }

        public string DeleteServicePrice(string accountId, ulong servicePriceId)
        {
            throw new NotImplementedException();
        }

        public string CreateServicePrice(string accountId, string multipleServicePriceXML, ulong serviceId)
        {
            throw new NotImplementedException();
        }

        public string SetSingleContentServicePrice2(string accountId, string servicePriceId, string contentId, decimal price)
        {
            throw new NotImplementedException();
        }

        public string GetServiceForId2(string accountId, long serviceId, bool includeContentInfo, bool includeServiceViewData, bool includeContentAgreement)
        {
            switch (serviceId)
            {
                case 2:
                    return GetTestData("service_2.xml");
                case 41:
                    return GetTestData("service_41.xml");
                default:
                    throw new NotImplementedException();
            }
        }

        public string GetMPPUserAccountInfo(string accountId)
        {
            return GetTestData("mppuser.xml");
        }

        private String GetTestData(String fileName)
        {
            String appPath;
            appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            appPath = appPath.Replace(@"bin\Debug", @"Core\TestData\Services\MPP\Data").Replace(@"file:\", "");

            String dataPath = Path.Combine(appPath, fileName);
            XmlDocument confDoc = new XmlDocument();
            confDoc.Load(dataPath);


            return confDoc.OuterXml;
        }

        #region IMPPService Members


        public string UpdateContentProperties(string accountId, string contentPropertiesUpdateXML)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IMPPService Members


        public string UpdateContentLimited(string accountId, string contentPropertiesUpdateXML)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IMPPService Members


        public void SetTimeout(int timeoutInSeconds)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IMPPService Members


        public string GetOngoingEpgs(string accountId, string xmlWithIdsOfEpgsToIgnore, string channelId, string eventDateTo, int processIntervalInMinutes, bool zipReply)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
