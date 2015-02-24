using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.MPP;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Policy;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;


namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    /// <summary>
    /// This wrapper helps with the communication towards MPP IntegrationServices.
    /// </summary>
    public class MPPIntegrationServicesWrapper
    {

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public IMPPService MPPService { get; set; }
    
        // locks
        private System.Object _GetAllServicesForAgreementWithName = new System.Object();
        private System.Object _GetServiceViewMatchRules = new System.Object();
        private System.Object _GetServiceForObjectId = new System.Object();        

        //private static Dictionary<UInt64, MultipleContentService> Services = new Dictionary<UInt64, MultipleContentService>();

        static MppXmlTranslator translator = new MppXmlTranslator();

        public MPPUser User { get; set; }

        private bool _zipReply;

        private MPPIntegrationServicesWrapper() { }

        /// <summary>
        /// Needed to use other account when doing delete for SFA
        /// </summary>
        public MPPIntegrationServicesWrapper(String accountID)
        {
            SSLValidator.OverrideValidation(); 
            var systemConfig =(MPPConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.MPP).SingleOrDefault();

            if (String.IsNullOrWhiteSpace(systemConfig.MPPServiceAssembly))
                MPPService = new MPPIntegrationService();
            else
            {
                MPPService = (IMPPService)Activator.CreateInstance(systemConfig.MPPServiceAssembly, systemConfig.MPPService).Unwrap();
            }

            MPPService.SetTimeout(systemConfig.TimeOut);
            _zipReply = systemConfig.ZipReply;
            this.User = this.GetMPPUser(accountID);
        }
        public ContentData GetContentDataByObjectID(UInt64 contentObjectID)
        {
            ContentData content = null;                        
            try
            {
                String reply = MPPService.GetContentForObjectId(this.User.AccountId, (Int64)contentObjectID);
                //log.Debug("XMLDATA: " + reply);
                XmlDocument contentMetadataXML = new XmlDocument();
                contentMetadataXML.LoadXml(reply);
                content = translator.TranslateXmlToContentData(contentMetadataXML)[0];
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("not found") > 0)
                    return null;

                log.Warn("Error fetching contentData from contentService or translating from xml to ContentData", e);
                throw;
            }

            return content;
        }

        public ContentData GetContentDataByID(UInt64 contentID)
        {
            ContentData content = null;
            try
            {
                String reply = MPPService.GetContentForId(this.User.AccountId, (Int64)contentID);
                //log.Debug("XMLDATA: " + reply);
                XmlDocument contentMetadataXML = new XmlDocument();
                contentMetadataXML.LoadXml(reply);
                content = translator.TranslateXmlToContentData(contentMetadataXML)[0];
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("not found") > 0)
                    return null;

                log.Warn("Error fetching contentData from contentService or translating from xml to ContentData", e);
                throw;
            }

            return content;
        }

        public List<EpgContentInfo> GetEpgContentInfoByChannel(UInt64 channelId, Int32 epgHistoryInHours, DateTime executeTime)
        {
            List<EpgContentInfo> epgs = new List<EpgContentInfo>();
            try
            {
               // List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();
                DateTime eventPeriodFrom = executeTime.AddHours(-epgHistoryInHours);
                EPGChannel epgChannel = CatchupHelper.GetEPGChannel(channelId); //channels.FirstOrDefault(c => c.MppContentId == channelId);

                if (epgChannel != null)
                {
                    ContentSearchParameters param = new ContentSearchParameters();
                    param.ContentRightsOwner = epgChannel.ContentRightOwner;
                    param.EventPeriodFrom = eventPeriodFrom;
                    //param.Properties.Add(CatchupContentProperties.ContentType, ContentType.CatchupTV.ToString("G"));
                    //param.Properties.Add(CatchupContentProperties.ChannelId, channelId.ToString());

                    //param.Properties.Add(SearchProperty.S_ChannelIdContentType, channelId.ToString() + ":" + ContentType.CatchupTV.ToString("G"));
                    param.ZipReply = _zipReply;
                    epgs = GetEpgContents(param);
                }
                return epgs;
            }
            catch (Exception ex) {
                log.Error("Failed to get EPG from MPP for channel " + channelId);
                return null;
            }
        }

        //public List<EpgContentInfo> GetAllEpgInfos(int epgHistoryInHours)
        //{
        //    //MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
        //    List<EpgContentInfo> ret = new List<EpgContentInfo>();
        //    try
        //    {
        //        List<String> CROs = CatchupHelper.GetAllContentRightsOwners();
        //        ContentSearchParameters param = new ContentSearchParameters();
        //        param.Properties.Add("ContentType", "CatchupTV");
        //        param.EventPeriodFrom = DateTime.UtcNow.AddHours(-epgHistoryInHours);
        //        foreach (String CRO in CROs)
        //        {
        //            param.ContentRightsOwner = CRO;
        //            ret.AddRange(GetEpgContents(param));
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        log.Error("Error fetching epgInfos", exc);
        //    }
        //    return ret;
        //}

        public List<ContentData> GetAllNonSynkedEpgContents()
        {
            //MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            List<ContentData> ret = new List<ContentData>();
            List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();
            try
            {
                foreach (EPGChannel channel in channels)
                {
                    ContentSearchParameters param = new ContentSearchParameters();
                    //param.Properties.Add(CatchupContentProperties.ContentType, ContentType.CatchupTV.ToString("G"));
                    //param.Properties.Add(CatchupContentProperties.EpgIsSynked, "False");                    
                    //param.Properties.Add(CatchupContentProperties.ChannelId, channel.MppContentId.ToString("G"));
                    //param.ContentRightsOwner = channel.ContentRightOwner;

                    //ret.AddRange(GetContent(param, true));
                    param.MaxReturn = 30;
                    //param.Properties.Add(SearchProperty.S_ChannelIdEpgIsSynked, channel.MppContentId.ToString("G") + ":" + "False");
                    param.ContentRightsOwner = channel.ContentRightOwner;
                    param.ZipReply = _zipReply;
                    ret.AddRange(GetContentFromProperties(param, true));
                }
                //List<String> CROs = CatchupHelper.GetAllContentRightsOwners();
                //ContentSearchParameters param = new ContentSearchParameters();
                //param.Properties.Add("ContentType", "CatchupTV");
                //param.Properties.Add(CatchupContentProperties.EpgIsSynked, "False");
                //foreach (String CRO in CROs)
                //{
                //    param.ContentRightsOwner = CRO;
                //    ret.AddRange(GetContent(param, true));
                //}
            }
            catch (Exception exc)
            {
                log.Error("Error fetching epgInfos", exc);
            }
            foreach (ContentData content in ret)
            {
                List<ContentAgreement> contentAgreements = GetAllServicesForContent(content);
                content.ContentAgreements.Clear();
                content.ContentAgreements.AddRange(contentAgreements);
            }
            return ret;
        }

        public List<EpgContentInfo> GetEpgContents(ContentSearchParameters param)
        {
            List<EpgContentInfo> ret = new List<EpgContentInfo>();
            try
            {
                //IDictionary<String, List<String>> agreements = new Dictionary<String, List<String>>();
               
                //List<ContentData> contents = GetContent(param, true);
                List<ContentData> contents = GetContentFromProperties(param, true);
                foreach (ContentData content in contents)
                {

                    EpgContentInfo epgInfo = new EpgContentInfo();
                    epgInfo.Content = RemoveAssets(content);

                    epgInfo.MetaDataHash = content.GetPropertyValue(CatchupContentProperties.EpgMetadataHash);
                    epgInfo.ChannelID = UInt64.Parse(content.GetPropertyValue(CatchupContentProperties.ChannelId));

                    List<ContentAgreement> contentAgreements = GetAllServicesForContent(content);
                    epgInfo.Content.ContentAgreements.Clear();
                    epgInfo.Content.ContentAgreements.AddRange(contentAgreements);
                    foreach (ContentAgreement agreement in contentAgreements)
                    {
                        foreach (MultipleContentService service in agreement.IncludedServices)
                        {
                            if (!epgInfo.Services.Contains(service.ObjectID.Value))
                                epgInfo.Services.Add(service.ObjectID.Value);
                        }
                    }
                    epgInfo.IsPublishedToAllServices = true;
                    foreach (ulong serviceObjectId in epgInfo.Services)
                    {
                        if (!epgInfo.Content.Properties.Exists(p => p.Type.Equals(CatchupContentProperties.CubiEpgId) && p.Value.StartsWith(serviceObjectId.ToString() + ":")))
                        {
                            epgInfo.IsPublishedToAllServices = false;
                            break;
                        }
                    }
                    ret.Add(epgInfo);
                }
            }
            catch (Exception ex)
            {
                log.Error("Error fetching contents", ex);
                return null;
            }
            return ret;
        }

        private ContentData RemoveAssets(ContentData content)
        {
            content.Assets.Clear();
            return content;
        }

        public void AddContents(List<ContentData> contentToAdd)
        {
            MppXmlTranslator translator = new MppXmlTranslator();
            String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><ContentMetadata>";

            foreach (ContentData content in contentToAdd)
            {
                xmlString += translator.TranslateContentDataToXml(content).InnerXml;
            }

            xmlString += "</ContentMetadata>";
            if (_zipReply)
                xmlString = CommonUtil.Zip(xmlString);
            String reply = MPPService.AddContent(this.User.AccountId, xmlString);

        }

        public MultipleServicePrice GetpriceDataByID(UInt64 priceID)
        {
            MultipleServicePrice price = null;

            try
            {
                String reply = MPPService.GetMultipleServicePriceByPriceID(this.User.AccountId, priceID);
                //log.Debug("XMLDATA: " + reply);
                XmlDocument priceXML = new XmlDocument();
                priceXML.LoadXml(reply);

                XmlDocument pericDoc = new XmlDocument();
                XmlNode node = priceXML.SelectSingleNode("MultipleServicePrices/MultipleServicePrice");
                pericDoc.LoadXml(node.OuterXml);
                price = translator.TranslateXmlToPriceData(pericDoc);
                //price.ContentsIncludedInPrice = GetAvailableContentsForPrice(price.ID.Value);
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("not found") > 0)
                    return null;

                log.Warn("Error fetching pricedata from serviceservice or translating from xml to priceData", e);
                throw;
            }

            return price;
        }

        public MultipleServicePrice GetpriceDataByObjectID(UInt64 priceObjectID)
        {
            MultipleServicePrice price = null;
            try
            {
                log.Debug("In GetpriceDataByObjectID for priceID " + priceObjectID.ToString());
                String reply = MPPService.GetMultipleServicePrice(this.User.AccountId, priceObjectID);
                log.Debug("reply = " + reply);
                //log.Debug("XMLDATA: " + reply);
                XmlDocument priceXML = new XmlDocument();
                priceXML.LoadXml(reply);
                
                XmlDocument pericDoc = new XmlDocument();
                XmlNode node = priceXML.SelectSingleNode("MultipleServicePrices/MultipleServicePrice");
                pericDoc.LoadXml(node.OuterXml);
                price = translator.TranslateXmlToPriceData(pericDoc);
                price.ObjectID = priceObjectID;
                price.ContentsIncludedInPrice = GetAvailableContentsForPrice(price.ID.Value);                
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("not found") > 0)
                    return null;

                log.Warn("Error fetching pricedata from serviceservice or translating from xml to priceData", e);
                throw;
            }

            return price;
        }

        public ContentData GetContentDataByExternalID(UInt64 serviceObjectID, String externalID)
        {            
            MultipleContentService service = GetServiceForObjectId(serviceObjectID);
            return GetContentDataByExternalID(service, externalID);
        }

        public ContentData GetContentDataByExternalID(MultipleContentService service, String externalID)
        {
            ContentData content = null;
            try
            {
                String reply = MPPService.GetContentForExternalId(this.User.AccountId, service.Name, externalID);
                //log.Debug("XMLDATA: " + reply);
                XmlDocument contentMetadataXML = new XmlDocument();
                contentMetadataXML.LoadXml(reply);
                content = translator.TranslateXmlToContentData(contentMetadataXML)[0];
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("not found") > 0)
                    return null;

                log.Warn("Error fetching contentData from contentService or translating from xml to ContentData", e);
                throw;
            }

            return content;
        }

        internal ulong GetLastEventID()
        {
            return 1; // EventHandling.GetLastID();
        }


        public List<MPPStationServerEvent> GetEventsFromSink(ulong lastEventID, ulong idOfUserToIgnore)
        {
            try
            {
                String reply = MPPService.GetEventsFromSink(this.User.AccountId, lastEventID, idOfUserToIgnore);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reply);
                List<MPPStationServerEvent> result  = translator.TranslateXmlToMPPStationServerEvent(doc);
                return result;
            }
            catch (Exception ex)
            {
                log.Warn("Error calling serviceService.GetEventsFromSink", ex);
                throw;
            }
        }

        /// <summary>
        /// This method adds a new content to the MPP
        /// </summary>
        /// <param name="contentData">An object containing data about the content to add.</param>
        /// <returns>true if the content was added successfully</returns>
        public void AddContent(ContentData contentData)
        {
            try
            {
                String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><ContentMetadata>";
                xmlString += translator.TranslateContentDataToXml(contentData).InnerXml;
                xmlString += "</ContentMetadata>";
                if (_zipReply)
                    xmlString = CommonUtil.Zip(xmlString);
                String reply = MPPService.AddContent(this.User.AccountId, xmlString);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reply);

                XmlElement modifiedContentNode = (XmlElement)doc.SelectSingleNode("ReturnInfo/ModifiedContent");
                contentData.ID =  UInt64.Parse(modifiedContentNode.GetAttribute("id"));
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.AddContent " + translator.TranslateContentDataToXml(contentData).InnerXml, e);
                throw;
            }
        }

        public void DeleteContent(ContentData contentData) {
            try {
                var propertiesToDelete = new List<Property>();
                ContentType contentType = ConaxIntegrationHelper.GetContentType(contentData);
                if (contentType == ContentType.CatchupTV)
                {
                    propertiesToDelete = GetEpgProperties(contentData);
                }
                else if (contentType == ContentType.VOD)
                {
                    propertiesToDelete = GetVodProperties(contentData);
                }
                else if (contentType == ContentType.Channel)
                {
                    propertiesToDelete = GetChannelProperties(contentData);
                }
                if (propertiesToDelete.Any())
                {
                    DeleteContentProperties(contentData.ID.Value, propertiesToDelete);
                }

                String reply = MPPService.DeleteContent(this.User.AccountId, (Int64)contentData.ID.Value);
                if (String.IsNullOrEmpty(reply))
                    log.Debug("Content " + contentData.ID.Value + " no longer exist in MPP.");
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.DeleteContent", e);
                throw;
            }
        }

        private List<Property> GetVodProperties(ContentData contentData)
        {
            var properties = new List<Property>();
            var property =
                contentData.Properties.SingleOrDefault<Property>(
                    p => p.Type.Equals("IngestIdentifier"));
            if (property != null && !String.IsNullOrEmpty(property.Value))
                properties.Add(property);

            property = contentData.Properties.SingleOrDefault<Property>(
                    p => p.Type.Equals(ColumbusContentProperties.Movie_Content_CheckSum));
            if (property != null && !String.IsNullOrEmpty(property.Value))
                properties.Add(property);
  
            property = contentData.Properties.SingleOrDefault<Property>(
                    p => p.Type.Equals("ConaxContegoContentID"));
            if (property != null && !String.IsNullOrEmpty(property.Value))
                properties.Add(property);
             
            var props = contentData.Properties.Where<Property>(
                   p => p.Type.Equals(VODnLiveContentProperties.ServiceExtContentID));
            if (props.Any())
            {
                foreach (Property p in props)
                {
                    if (!String.IsNullOrEmpty(p.Value))
                        properties.Add(p);
                }
            }
            return properties;
        }

        private List<Property> GetEpgProperties(ContentData contentData)
        {
            var properties = new List<Property>();
            var archiveveProperties =
                        contentData.Properties.Where<Property>(
                            p => p.Type.StartsWith(CatchupContentProperties.NPVRArchiveTimes));
            foreach (Property archiveProperty in archiveveProperties)
            {
                if (archiveProperty != null && !String.IsNullOrEmpty(archiveProperty.Value))
                    properties.Add(archiveProperty);
            }

            var props = contentData.Properties.Where<Property>(
                    p => p.Type.Equals(CatchupContentProperties.CubiEpgId));
            if (props.Any())
            {
                foreach (Property p in props)
                {
                    if (!String.IsNullOrEmpty(p.Value))
                        properties.Add(p);
                }
            }

            var property = contentData.Properties.SingleOrDefault<Property>(
                   p => p.Type.Equals(CatchupContentProperties.EpgMetadataHash));
            if (property != null && !String.IsNullOrEmpty(property.Value))
                properties.Add(property);

            props = contentData.Properties.Where<Property>(
                   p => p.Type.Equals(VODnLiveContentProperties.ServiceExtContentID));
            if (props.Any())
            {
                foreach (Property p in props)
                {
                    if (!String.IsNullOrEmpty(p.Value))
                        properties.Add(p);
                }
            }
            
            return properties;
        }

        private List<Property> GetChannelProperties(ContentData contentData)
        {
            var properties = new List<Property>();
            
            var props = contentData.Properties.Where<Property>(
                    p => p.Type.Equals("CubiNPVRId"));
            if (props.Any())
            {
                foreach (Property p in props)
                {
                    if (!String.IsNullOrEmpty(p.Value))
                        properties.Add(p);
                }
            }

            props = contentData.Properties.Where<Property>(
                    p => p.Type.Equals(VODnLiveContentProperties.CubiCatchUpId));
            if (props.Any())
            {
                foreach (Property p in props)
                {
                    if (!String.IsNullOrEmpty(p.Value))
                        properties.Add(p);
                }
            }
            
            props = contentData.Properties.Where<Property>(
                   p => p.Type.Equals(VODnLiveContentProperties.ServiceExtContentID));
            if (props.Any())
            {
                foreach (Property p in props)
                {
                    if (!String.IsNullOrEmpty(p.Value))
                        properties.Add(p);
                }
            }

            var property = contentData.Properties.SingleOrDefault<Property>(
                  p => p.Type.Equals("ConaxContegoContentID"));
            if (property != null && !String.IsNullOrEmpty(property.Value))
                properties.Add(property);

            property =
               contentData.Properties.SingleOrDefault<Property>(
                   p => p.Type.Equals("IngestIdentifier"));
            if (property != null && !String.IsNullOrEmpty(property.Value))
                properties.Add(property);
            return properties;
        }

        public void UpdateContentsInChunks(List<ContentData> newContent)
        {
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            int noToSendPerCall = 100;
            if (managerConfig.ConfigParams.ContainsKey("ContentsToSendPerCallToMpp"))
                noToSendPerCall = Int32.Parse(managerConfig.GetConfigParam("ContentsToSendPerCallToMpp"));
            List<ContentData> contents = new List<ContentData>();
            List<List<ContentData>> chunks = CommonUtil.SplitIntoChunks<ContentData>(newContent, noToSendPerCall);
            foreach (List<ContentData> newContentSublist in chunks)
            {
                UpdateContents(newContentSublist);
            }
        }

        public void UpdateContentsLimitedInChunks(List<ContentData> newContent)
        {
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            int noToSendPerCall = 100;
            if (managerConfig.ConfigParams.ContainsKey("ContentsToSendPerCallToMpp"))
                noToSendPerCall = Int32.Parse(managerConfig.GetConfigParam("ContentsToSendPerCallToMpp"));
            List<ContentData> contents = new List<ContentData>();
            List<List<ContentData>> chunks = CommonUtil.SplitIntoChunks<ContentData>(newContent, noToSendPerCall);
            foreach (List<ContentData> newContentSublist in chunks)
            {
                UpdateContentsLimited(newContentSublist);
            }
        }

        public void UpdateContentsPropertiesInChunks(List<UpdatePropertiesForContentParameter> contentList)
        {
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            int noToSendPerCall = 100;
            if (managerConfig.ConfigParams.ContainsKey("ContentsToSendPerCallToMpp"))
                noToSendPerCall = Int32.Parse(managerConfig.GetConfigParam("ContentsToSendPerCallToMpp"));
            List<UpdatePropertiesForContentParameter> contents = new List<UpdatePropertiesForContentParameter>();
            List<List<UpdatePropertiesForContentParameter>> chunks = CommonUtil.SplitIntoChunks<UpdatePropertiesForContentParameter>(contentList, noToSendPerCall);
            foreach (List<UpdatePropertiesForContentParameter> newContentSublist in chunks)
            {
                UpdateContentProperties(newContentSublist);
            }
        }

        /// <summary>
        /// This method updates an existing content in the MPP.
        /// </summary>
        /// <param name="contentData">An object containing data about the content to update</param>
        /// <returns>True if the update was successful</returns>
        public ContentData UpdateContent(ContentData contentData)
        {
            return UpdateContent(contentData, true);
        }
        /// <summary>
        /// This method updates an existing content in the MPP.
        /// </summary>
        /// <param name="contentData">An object containing data about the content to update</param>
        /// <param name="returnData">States if the updated content should be returned</param>
        /// <returns>True if the update was successful</returns>
        public ContentData UpdateContent(ContentData contentData, bool returnData)
        {
            try
            {
                String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><ContentMetadata>";
                xmlString += translator.TranslateContentDataToXml(contentData).InnerXml;
                xmlString += "</ContentMetadata>";
                if (_zipReply)
                    xmlString = CommonUtil.Zip(xmlString);
                String reply = MPPService.UpdateContent3(this.User.AccountId, xmlString);
                // refreach content.
                ContentData content = null;
                if (returnData)
                    content = GetContentDataByObjectID(contentData.ObjectID.Value);
                return content;
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateContent", e);
                throw;
            }            
        }


        public void AddContentProperty(UInt64 contentId, Property proeprty) {
            try
            {
                List<KeyValuePair<String, Property>> properties = new List<KeyValuePair<String, Property>>();
                properties.Add(new KeyValuePair<String, Property>("ADD", proeprty));
                UpdateContentProperties(contentId, properties);
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateContentProperties for content " + contentId, e);
                throw;
            }
        }

        public void UpdateContentProperties(UInt64 contentId, List<Property> proeprtiesToUpdate)
        {
            try
            {
                List<KeyValuePair<String, Property>> properties = new List<KeyValuePair<String, Property>>();
                foreach (Property property in proeprtiesToUpdate)
                    properties.Add(new KeyValuePair<String, Property>("UPDATE", property));
                
                UpdateContentProperties(contentId, properties);
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateContentProperties for content " + contentId, e);
                throw;
            }
        }

        public void UpdateContentProperty(UInt64 contentId, Property proeprty)
        {
            try
            {                
                List<KeyValuePair<String, Property>> properties = new List<KeyValuePair<String, Property>>();
                properties.Add(new KeyValuePair<String, Property>("UPDATE", proeprty));
                UpdateContentProperties(contentId, properties);
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateContentProperties for content " + contentId, e);
                throw;
            }
        }

        public void UpdateContentProperties(List<UpdatePropertiesForContentParameter> updateList )
        {
            string xml = "";
            try
            {
                // NOTE!!, I think this UpdateContentSet() in IS is not raising any content udpate event.
                // so you probably won't get any content update evnt even if you are using an actinve user accountid.
                // TODO: IF you need it to raise an content update event, then you will need to fix it in the IS to make it.
                xml = translator.BuildContentPropertiesUpdateXML(updateList).InnerXml;
                if (_zipReply)
                    xml = CommonUtil.Zip(xml);
                int retries = 0;
                try
                {
                    MPPService.UpdateContentProperties(this.User.AccountId, xml);
                }
                catch (Exception exc)
                {
                    Exception error = exc;
                    log.Debug("Error calling UpdateContentProperties, retry", exc);
                    retries++;
                    while (retries < 3)
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            MPPService.UpdateContentProperties(this.User.AccountId, xml);
                            break;
                        }
                        catch (Exception ex)
                        {
                            error = ex;
                            retries++;
                        }

                    }
                    if (retries >= 3)
                    {
                        log.Error("Error calling UpdateContentProperties, tried 3 times", error);
                        throw error;
                    }
                }
                
            }
            catch (Exception e)
            {
                log.Error("Failed to UpdateContentProperties with: " + xml);
               // log.Warn("Error calling contentService.UpdateContentProperties for content " + contentId, e);
                throw;
            }
        }

        public void UpdateContentProperties(UInt64 contentId, List<KeyValuePair<String, Property>> properties)
        {
            // NOTE!!, I think this UpdateContentSet() in IS is not raising any content udpate event.
            // so you probably won't get any content update evnt even if you are using an actinve user accountid.
            // TODO: IF you need it to raise an content update event, then you will need to fix it in the IS to make it.
            int retries = 0;
            string xml = translator.BuildContentSetMetaDataXML(contentId, properties).InnerXml;
            try
            {
                MPPService.UpdateContentSet(this.User.AccountId, xml);  
            }
            catch (Exception exc)
            {
                Exception error = exc;
                log.Debug("Error calling UpdateContentSet, retry", exc);
                retries++;
                while (retries < 3)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        MPPService.UpdateContentSet(this.User.AccountId, xml);
                        break;
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                        retries++;
                    }
                   
                }
                if (retries >= 3)
                {
                    log.Error("Error calling UpdateContentSet, tried 3 times", error);
                    throw error;
                }
            }
                   
        }

        public void DeleteContentProperties(UInt64 contentId, List<Property> propertiesToDelete)
        {
            try
            {
                List<KeyValuePair<String, Property>> properties = new List<KeyValuePair<String, Property>>();
                foreach (Property property in propertiesToDelete)
                    properties.Add(new KeyValuePair<String, Property>("DELETE", property));

                UpdateContentProperties(contentId, properties);
            }
            catch (Exception e)
            {
                if (e.Message.IndexOf("not found") > 0) {
                    log.Debug("Content " + contentId + " no longer exist in MPP.");
                    return;
                }

                log.Warn("Error calling contentService.UpdateContentProperties for content " + contentId, e);
                throw;
            }
        }

        public void UpdateContents(List<ContentData> contents)
        {
            String xmlString = "";
            try
            {
                MppXmlTranslator translator = new MppXmlTranslator();
                xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><ContentMetadata>";

                foreach (ContentData content in contents)
                {
                    xmlString += translator.TranslateContentDataToXml(content).InnerXml;
                }

                xmlString += "</ContentMetadata>";
                if (_zipReply)
                    xmlString = CommonUtil.Zip(xmlString);
                String reply = MPPService.UpdateContent3(this.User.AccountId, xmlString);
               
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateContent3 using xml= " + xmlString, e);
                throw;
            }
        }

        public void UpdateContentsLimited(List<ContentData> contents)
        {
            String xmlString = "";
            try
            {
                MppXmlTranslator translator = new MppXmlTranslator();
                xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><ContentMetadata>";

                foreach (ContentData content in contents)
                {
                    xmlString += translator.TranslateContentDataToXml(content).InnerXml;
                }

                xmlString += "</ContentMetadata>";
                if (_zipReply)
                    xmlString = CommonUtil.Zip(xmlString);
                String reply = MPPService.UpdateContentLimited(this.User.AccountId, xmlString);

            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateContent3 using xml= " + xmlString, e);
                throw;
            }
        }
        /*
        /// <summary>
        /// This method fetches the ids of contents deleted in the MPP.
        /// </summary>
        /// <returns>If successfull returns a list with contentObjectIDs.</returns>
        public List<ulong> GetDeletedContents()
        {
            List<ulong> contents = new List<ulong>();
            try
            {
                String reply = serviceService.GetEventsOfType(AccountID, (int)EventType.ContentDeleted, GetLastEventID(), false);
                XmlDocument events = new XmlDocument();
                events.LoadXml(reply);
                foreach (XmlElement contentIDNode in events.SelectNodes("ContentObjectIDs/ContentObjectID"))
                {
                    ulong contentID = 0;
                    ulong.TryParse(contentIDNode.InnerText, out contentID);
                    if (contentID != 0)
                    {
                        contents.Add(contentID);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Error when fetching deleted content", e);
                return contents;
            }
            return contents;
        }
        */
        public Boolean UpdateServicePrice(MultipleServicePrice servicePrice)
        {
            try
            {
                String translatedXML = translator.TranslatePriceDataToXml(servicePrice).InnerXml;
                log.Debug(translatedXML);
                String reply = MPPService.UpdateServicePrice(this.User.AccountId, servicePrice.ID.Value, translatedXML);
            } 
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateContent", e);
                throw;
            }
            return true;
        }

        public List<MultipleServicePrice> GetServicePricesForContent(ContentData contentData, UInt64 serviceObjectID)
        {
            try
            {
                List<MultipleServicePrice> result = new List<MultipleServicePrice>();

                String reply = MPPService.GetContentPrices(this.User.AccountId, contentData.ObjectID.Value, serviceObjectID);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reply);

                XmlNodeList multipleServicePriceNodes = doc.SelectNodes("MultipleServicePrices/MultipleServicePrice");
                foreach (XmlElement multipleServicePriceNode in multipleServicePriceNodes) {
                    XmlDocument pericDoc = new XmlDocument();
                    pericDoc.LoadXml(multipleServicePriceNode.OuterXml);
                    MultipleServicePrice servicePrice = translator.TranslateXmlToPriceData(pericDoc);
                    if (servicePrice.IsRecurringPurchase.Value)
                    {
                        servicePrice.ContentsIncludedInPrice = GetAvailableContentsForPrice(servicePrice.ID.Value);
                    }
                    result.Add(servicePrice);
                }

                return result;
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.GetServicePricesForContent", e);
                throw;
            }
        }

        /// <summary>
        /// This method updates the assets with new names.
        /// </summary>
        /// <param name="contentData">The content to update assets for.</param>
        /// <returns>true if update was successful.</returns>
        public Boolean UpdateAssets(ContentData contentData)
        {
            try
            {
                MppXmlTranslator translator = new MppXmlTranslator();
                String xml = translator.TranslateForAssetUpdate(contentData);
                //log.Debug("XMLDATA: " + xml);
                String reply = MPPService.UpdateAsset(this.User.AccountId, xml);
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateAssets", e);
                throw;
            }
            return true;
        }

        public Boolean UpdateAssets(List<Asset> assets)
        {
            try
            {
                MppXmlTranslator translator = new MppXmlTranslator();
                String xml = translator.TranslateForAssetUpdate(assets);
                //log.Debug("XMLDATA: " + xml);
                String reply = MPPService.UpdateAsset(this.User.AccountId, xml);
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateAssets", e);
                throw;
            }
            return true;
        }

        public List<ContentAgreement> GetAllServicesForContent(ContentData content)
        {
            List<ContentAgreement> agreements = new List<ContentAgreement>();
            foreach (ContentAgreement contentAgreement in content.ContentAgreements) {
                List<ContentAgreement> tempAgreements = GetAllServicesForAgreementWithName(contentAgreement.Name);
                var filteredAgreements = tempAgreements.Where(ta => ta.ContentRightsOwner.Name.Equals(content.ContentRightsOwner.Name, StringComparison.OrdinalIgnoreCase));
                if (filteredAgreements != null)
                    agreements.AddRange(filteredAgreements);
            }
            return agreements;
        }

        public List<ContentAgreement> GetAllServicesForAgreementWithName(String agreementName)
        {
            // check cache
            String key = "MPPIntegrationServicesWrapper.GetAllServicesForAgreementWithName|" + agreementName;
            List<ContentAgreement> agreements = WFMCache.Get<List<ContentAgreement>>(key);
            if (agreements != null)
                return agreements;

            lock (_GetAllServicesForAgreementWithName)
            {   // cehck again after locks
                agreements = WFMCache.Get<List<ContentAgreement>>(key);
                if (agreements == null) {

                    agreements = new List<ContentAgreement>();
                    try
                    {
                        String reply = MPPService.GetServicesIncludedInContentAgreement(this.User.AccountId, agreementName);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(reply);
                        foreach (XmlNode n in doc.SelectNodes("ContentAgreementInfos"))
                        {
                            ContentAgreement contentAgreement = new ContentAgreement();
                            XmlElement agreementInfo = n["ContentAgreementInfo"];
                            if (agreementInfo == null)
                            {
                                log.Warn("AgreementInfo is null, continuing to next");
                                continue;
                            }
                            XmlAttribute attribute = agreementInfo.Attributes["name"];
                            String name = "";
                            if (attribute != null)
                            {
                                name = agreementInfo.Attributes["name"].Value;
                            }
                            else
                            {
                                log.Warn("Agreement with objectId " + agreementInfo.Attributes["objectID"].Value + " is missing name");
                            }
                            contentAgreement.Name = name;
                            String objectID = agreementInfo.Attributes["objectID"].Value;
                            contentAgreement.ObjectID = ulong.Parse(objectID);
                            contentAgreement.ContentRightsOwner = new ContentRightsOwner();
                            contentAgreement.ContentRightsOwner.Name = agreementInfo.Attributes["contentRightsOwnerName"].Value;
                            contentAgreement.ContentRightsOwner.ObjectID = UInt64.Parse(agreementInfo.Attributes["contentRightsOwnerObjectID"].Value);

                            foreach (XmlElement element in agreementInfo.SelectNodes("ServiceInfo"))
                            {
                                MultipleContentService service = new MultipleContentService();
                                String serviceName = element.Attributes["Name"].Value;
                                service.Name = serviceName;
                                String serviceObjectID = element.Attributes["ObjectID"].Value;
                                service.ObjectID = ulong.Parse(serviceObjectID);
                                String id = element.Attributes["ID"].Value;
                                service.ID = ulong.Parse(id);
                                contentAgreement.IncludedServices.Add(service);
                            }
                            agreements.Add(contentAgreement);
                        }
                        WFMCache.Add<List<ContentAgreement>>(key, agreements);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error when fetching contentAgreement", ex);
                    }
                }
            }
            return agreements;
        }

        public List<ulong> GetAvailableContentsForPrice(ulong priceID)
        {
            try
            {
                //log.Debug("adding available prices -----------------------------------------------------------");
                List<MultipleServicePrice> result = new List<MultipleServicePrice>();
              //  log.Debug("accountID= " + AccountID + " ServiceObjectID= " + ServiceObjectID + " priceID= " + priceID);
                String reply = MPPService.GetContentsAvailableForPrice(this.User.AccountId, priceID);
              //  log.Debug("reply= " + reply);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reply);

                List<ulong> contentIDs = new List<ulong>();
                XmlNode contents = doc.SelectSingleNode("Contents");
                foreach (XmlNode content in contents.SelectNodes("ContentObjectID"))
                {
                    contentIDs.Add(ulong.Parse(content.InnerText));
                //    log.Debug("adding content with ID " + content.InnerText);
                }
               // log.Debug("found " + contentIDs.Count + " content available for price " + priceID.ToString());
                return contentIDs;
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.GetAvailableContentsForPrice", e);
                throw;
            }
        }
        /*
        public List<ContentData> GetAllContent()

        {
            try
            {
                List<ContentData> result = new List<ContentData>();

                String searchParam = "<ContentSearchParameters><ContentRightsOwner>" + DefaultContentRightsOwner + "</ContentRightsOwner></ContentSearchParameters>";
                String reply = contentService.GetContentInfo(AccountID, searchParam, "NoSort");

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reply);

                XmlNodeList contentInfoNodes = doc.SelectNodes("ContentInfoList/ContentInfo");
                foreach (XmlElement contentInfoNode in contentInfoNodes) {
                    String contentXML = contentService.GetContentForId(AccountID, Int64.Parse(contentInfoNode.GetAttribute("id")));
                    XmlDocument contentDoc = new XmlDocument();
                    contentDoc.LoadXml(contentXML);
                    ContentData content = translator.TranslateXmlToContentData(contentDoc)[0];
                    result.Add(content);
                }

                return result;
            }
            catch (Exception e)
            {
                log.Warn("Error calling contentService.UpdateContent", e);
                throw;
            }
        }
        */
        public List<ContentData> GetContent(ContentSearchParameters contentSearchParameters, Boolean includeMPPContext)
        {

            try
            {
                List<ContentData> result = new List<ContentData>();
                String reply = MPPService.GetContent(this.User.AccountId, contentSearchParameters.ToXML(), includeMPPContext);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reply);
                result = translator.TranslateXmlToContentData(doc);

                return result;
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("ApplicationException") > -1)
                {
                    if ((ex.Message.IndexOf("Could not find property kind") > 0) ||
                        (ex.Message.IndexOf("Could not find property with name") > 0)) {
                            log.Warn(ex.Message + " " + contentSearchParameters.ToXML());
                            return new List<ContentData>();
                    }
                }
                log.Warn("Error calling contentService.GetContent " + contentSearchParameters.ToXML(), ex);
                throw;
            }
        }

        public List<ContentData> GetContentFromProperties(ContentSearchParameters contentSearchParameters, Boolean includeMPPContext)
        {

            try
            {
                List<ContentData> result = new List<ContentData>();
                String reply = MPPService.GetContentFromProperties(this.User.AccountId, contentSearchParameters.ToXML(), includeMPPContext);
                if (contentSearchParameters.ZipReply)
                    reply = CommonUtil.UnZip(reply);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reply);
                result = translator.TranslateXmlToContentData(doc);
               
                return result;
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("ApplicationException") > -1)
                {
                    if ((ex.Message.IndexOf("Could not find property kind") > 0) ||
                        (ex.Message.IndexOf("Could not find property with name") > 0))
                    {
                        log.Warn(ex.Message + " " + contentSearchParameters.ToXML());
                        return new List<ContentData>();
                    }
                }
                log.Warn("Error calling contentService.GetContent " + contentSearchParameters.ToXML(), ex);
                throw;
            }
        }

        public List<ContentData> GetOngoingEpgs(XmlDocument xmlOfContentIdsToIgnore, ulong channelId, DateTime eventDateTo, int rerunInterval)
        {
            String reply = MPPService.GetOngoingEpgs(this.User.AccountId,
                xmlOfContentIdsToIgnore.InnerXml, channelId.ToString(), eventDateTo.ToString("yyyy-MM-dd HH:mm:ss"), rerunInterval, _zipReply);
            if (_zipReply)
                reply = CommonUtil.UnZip(reply);
            List<ContentData> result = new List<ContentData>();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(reply);
            result = translator.TranslateXmlToContentData(doc);
            return result;
        }

        public MultipleContentService GetServiceForObjectId(UInt64 serviceObjectID)
        {
            //try
            //{
            //    if (!Services.ContainsKey(serviceObjectID))
            //    {
            //        String reply = serviceService.GetServiceForObjectId(this.User.AccountId, serviceObjectID, false, false);
            //        XmlDocument doc = new XmlDocument();
            //        doc.LoadXml(reply);
            //        MultipleContentService result = translator.TranslateXmlToMultipleContentService(doc);
            //        if (!Services.ContainsKey(serviceObjectID)) {
            //            Services.Add(serviceObjectID, result);
            //        }
            //    }
            //    return Services.First(s => s.Key == serviceObjectID).Value;
            //}
            //catch (Exception ex)
            //{
            //    log.Warn("Error calling serviceService.GetServiceForObjectId", ex);
            //    throw;
            //}


            // check cache
            String key = "MPPIntegrationServicesWrapper.GetServiceForObjectId|" + serviceObjectID;
            MultipleContentService service = WFMCache.Get<MultipleContentService>(key);
            if (service != null)
                return service;

            lock (_GetServiceForObjectId)
            {   // cehck again after locks
                service = WFMCache.Get<MultipleContentService>(key);
                if (service == null)
                {
                    try
                    {
                        service = new MultipleContentService();
                        String reply = MPPService.GetServiceForObjectId(this.User.AccountId, serviceObjectID, false, false);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(reply);
                        service = translator.TranslateXmlToMultipleContentService(doc);

                        WFMCache.Add<MultipleContentService>(key, service);
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Error calling serviceService.GetServiceForObjectId", ex);
                        throw;
                    }
                }
            }
            return service;
        }

        public void DeleteServicePrice(MultipleServicePrice servicePrice) {
            try {
                MPPService.DeleteServicePrice(this.User.AccountId, servicePrice.ID.Value);
            }
            catch (Exception ex)
            {
                log.Warn("Error calling serviceService.DeleteServicePrice with servcie price id " + servicePrice.ID.Value, ex);
                throw;
            }
        }

        public void CreateServicePrice(UInt64 ServiceObjectID, MultipleServicePrice servicePrice) { 
            try
            {
                MultipleContentService service = GetServiceForObjectId(ServiceObjectID);
                String reply = MPPService.CreateServicePrice(this.User.AccountId, translator.TranslatePriceDataToXml(servicePrice).InnerXml, service.ID.Value);
                servicePrice.ID = UInt64.Parse(reply);
            } 
            catch (Exception ex)
            {
                log.Warn("Error calling serviceService.CreateServicePrice", ex);
                throw;
            }
        }
        
        public void SetContentServicePrice(MultipleServicePrice servicePrice, ContentData content)
        {
            try
            {
                String reply = MPPService.SetSingleContentServicePrice2(this.User.AccountId, servicePrice.ID.Value.ToString(), content.ID.Value.ToString(), 0);
            }
            catch (Exception ex)
            {
                log.Warn("Error calling serviceService.CreateServicePrice", ex);
                throw;
            }
        }

        public List<ServiceViewMatchRule> GetServiceViewMatchRules(MultipleContentService service)
        {
            // check cache
            String key = "MPPIntegrationServicesWrapper.GetServiceViewMatchRules|" + service.ID;
            List<ServiceViewMatchRule> matchrules = WFMCache.Get<List<ServiceViewMatchRule>>(key);
            if (matchrules != null)
                return matchrules;

            lock (_GetServiceViewMatchRules)
            {   // cehck again after locks
                matchrules = WFMCache.Get<List<ServiceViewMatchRule>>(key);
                if (matchrules == null)
                {
                    try
                    {
                        matchrules = new List<ServiceViewMatchRule>();
                        String reply = MPPService.GetServiceForId2(this.User.AccountId, (Int64)service.ID.Value, false, true, false);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(reply);

                        XmlNodeList matchruleNodes = doc.SelectNodes("ServiceMetadata/MultipleContentService/ServiceViewMatchRule");
                        foreach (XmlElement matchruleNode in matchruleNodes)
                        {
                            ServiceViewMatchRule matchrule = new ServiceViewMatchRule();
                            matchrule.serviceViewName = matchruleNode.GetAttribute("serviceViewName");
                            matchrule.serviceViewObjectId = UInt64.Parse(matchruleNode.GetAttribute("serviceViewObjectId"));
                            matchrule.Region = matchruleNode.GetAttribute("Region");

                            XmlNode serviceViewNode = matchruleNode.SelectSingleNode("ServiceView");
                            matchrule.ServiceViewLanugageISO = serviceViewNode.Attributes["lanugageISO"].Value;

                            matchrules.Add(matchrule);
                        }

                        WFMCache.Add<List<ServiceViewMatchRule>>(key, matchrules);
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Error calling serviceService.GetServiceForId2", ex);
                        throw;
                    }
                }
            }
            return matchrules;
        }

        public MPPUser GetMPPUser(String accountId)
        {
            try
            {
                String reply = MPPService.GetMPPUserAccountInfo(accountId);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reply);
                MPPUser user = translator.TranslateXmlToMPPUser(doc);
                user.AccountId = accountId;
                return user;
            }
            catch (Exception ex)
            {
                MPPUser mppUser = new MPPUser()
                {
                    AccountId = "123456",
                    Id=1613,
                    userName = "MPPtestUser"                   
                };
                return mppUser;
                //log.Warn("Error calling mppUserService.GetMPPUserAccountInfo with accountid " + accountId, ex);
                //throw;
            }
        }

        public List<ContentRightsOwner> GetContentRightsOwners()
        {
            try
            {
                String res = MPPService.GetContentRightsOwners(this.User.AccountId);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(res);

                List<ContentRightsOwner> cros = new List<ContentRightsOwner>();
                XmlNodeList nameNodes = doc.SelectNodes("//ContentRightsOwners/ContentRightsOwner/Name");
                foreach (XmlNode nameNode in nameNodes) {
                    ContentRightsOwner cro = new ContentRightsOwner();
                    cro.Name = nameNode.InnerText;
                    cros.Add(cro);
                }
                return cros;
            }
            catch (Exception ex)
            {
                log.Error("Failed to get CROs.", ex);
                throw;
            }
        } 
    }

    public class ContentSearchParameters
    {

        public ContentSearchParameters()
        {
            Properties = new Dictionary<String, String>();
        }

        public String ContentRightsOwner { get; set; }
        public Dictionary<String, String> Properties { get; set; }
        public DateTime? EventPeriodFrom { get; set; }
        public DateTime? EventPeriodTo { get; set; }
        public DateTime? EventPoint { get; set; }
        public String ContentAgreement { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public int MaxReturn { get; set; }
        public bool ZipReply { get; set; }

        public String ToXML()
        {
            String searchParam = "";

            searchParam += "<ContentSearchParameters>";

            if (!String.IsNullOrEmpty(ContentRightsOwner))
                searchParam += "<ContentRightsOwner>" + ContentRightsOwner + "</ContentRightsOwner>";

            if (!String.IsNullOrEmpty(ContentAgreement))
                searchParam += "<ContentAgreement>" + ContentAgreement + "</ContentAgreement>";
            if (MaxReturn > 0)
                searchParam += "<MaxReturn>" + MaxReturn + "</MaxReturn>";
            if (ZipReply)
                searchParam += "<ZipReply>" + ZipReply + "</ZipReply>";
            if (Properties.Count > 0)
            {
                searchParam += "<PropertyFilter>";
                foreach (KeyValuePair<String, String> kvp in Properties)
                    searchParam += "<Property type=\"" + kvp.Key + "\">" + kvp.Value + "</Property>";
                searchParam += "</PropertyFilter>";
            }

            if (EventPeriodFrom != null && EventPeriodFrom.HasValue)
                searchParam += "<EventPeriodFrom>" + EventPeriodFrom.Value.ToString("yyyy-MM-dd HH:mm:ss") + "</EventPeriodFrom>";

            if (EventPeriodTo != null && EventPeriodTo.HasValue)
                searchParam += "<EventPeriodTo>" + EventPeriodTo.Value.ToString("yyyy-MM-dd HH:mm:ss") + "</EventPeriodTo>";

            if (CreatedFrom != null && CreatedFrom.HasValue)
                searchParam += "<CreatedFrom>" + CreatedFrom.Value.ToString("yyyy-MM-dd HH:mm:ss") + "</CreatedFrom>";

            if (EventPoint != null && EventPoint.HasValue)
                searchParam += "<EventPoint>" + EventPoint.Value.ToString("yyyy-MM-dd HH:mm:ss") + "</EventPoint>";



            searchParam += "</ContentSearchParameters>";


            return searchParam;
        }
    }
}
