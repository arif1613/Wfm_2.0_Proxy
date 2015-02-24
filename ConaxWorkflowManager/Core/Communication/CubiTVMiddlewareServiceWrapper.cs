using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;
using System.Net;
using System.IO;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using System.Collections;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Translation;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{

    /// <summary>
    /// This wrapper helps with the communication towards CubiTV Middleware.
    /// </summary>
    public class CubiTVMiddlewareServiceWrapper : ICubiTVMWServiceWrapper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IMiddleWareRestApiCaller restAPI = null;
        MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
        CubiTVTranslator translator = null;
        ServiceConfig serviceConfig = null;

        public CubiTVMiddlewareServiceWrapper(ServiceConfig serviceConfig)
            : this(serviceConfig, new MiddleWareRestApiCaller(serviceConfig.GetConfigParam("RestAPIBaseURL"), serviceConfig.GetConfigParam("UserHash")))
        {
        }

        public CubiTVMiddlewareServiceWrapper(ServiceConfig serviceConfig, IMiddleWareRestApiCaller MiddleWareRestApiCaller)
        {
            //String key = serviceConfig.GetConfigParam("UserHash");
            //String baseURL = serviceConfig.GetConfigParam("RestAPIBaseURL");
            restAPI = MiddleWareRestApiCaller;
            translator = new CubiTVTranslator(this);
            this.serviceConfig = serviceConfig;
        }

        

        public MultipleContentService Service
        {
            get
            {
                MultipleContentService service = mppWrapper.GetServiceForObjectId(this.serviceConfig.ServiceObjectId);
                if (service.ServiceViewMatchRules.Count == 0)
                {
                    List<ServiceViewMatchRule> matchRules = mppWrapper.GetServiceViewMatchRules(service);
                    service.ServiceViewMatchRules = matchRules;
                }
                return service;
            }
        }

        public void CreateEpgImports(List<ContentData> contents, Double keepCatchupAliveInHour)
        {
            //CubiTVTranslator translater = new CubiTVTranslator();
            XmlDocument contentXML = translator.TranslateContentDataToXMLTVXml(contents, this.Service.ObjectID.Value, keepCatchupAliveInHour);
            if (contentXML.ChildNodes.Count == 0)
            {
                log.Debug("No EPG items found. No XMLTV will sent.");
                return;
            }
            // save to file first
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            if (systemConfig.ConfigParams.ContainsKey("XMLTVArchive"))
            {
                String xmltvPath = Path.Combine(systemConfig.GetConfigParam("XMLTVArchive"), DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_ffff") + ".xml");
                log.Debug("Save XMLTV to " + xmltvPath);
                contentXML.Save(xmltvPath);
            }

            String base64XMLTV = CommonUtil.EncodeTo64(contentXML.OuterXml);
            log.Debug("Creating xml to send Cubiware");
            XmlDocument base64XMLTVXML = new XmlDocument();
            base64XMLTVXML.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?><datafile>" + base64XMLTV + "</datafile>");
            log.Debug("Calling Cubiware to add epgs");
            CallStatus status = restAPI.MakeAddCall("epgs/import", base64XMLTVXML);
            if (!status.Success)
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        public void CreateCatchUpContent(ContentData content, Double keepCatchupAliveInHour)
        {
            List<ContentData> contents = new List<ContentData>();
            contents.Add(content);
            CreateEpgImports(contents, keepCatchupAliveInHour);
        }

        public void CreateNPVRRecording(String cubiepgid)
        {
            String xmlstr = "<npvr-recording><end-at>2013-03-21T15:20:00+01:00</end-at><epg-id>" + cubiepgid + "</epg-id><customer-id>2</customer-id><service-id>6</service-id><npvr-folder></npvr-folder><npvr-state>scheduled</npvr-state><start-at>2013-03-21T15:00:00+01:00</start-at></npvr-recording>";

            XmlDocument recordingdoc = new XmlDocument();
            recordingdoc.LoadXml(xmlstr);

            CallStatus status = restAPI.MakeAddCall("npvr_recordings", recordingdoc);
            if (!status.Success)
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        /// <summary>
        /// This method registers a VOD Content in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="contentData">The object containing the information about the content to add.</param>
        /// <returns>Returns true if the content was added successfully.</returns>
        public ContentData CreateContent(ContentData contentData, ulong serviceObjectID, ServiceConfig seviceConfig, bool createCategoryIfNotExists)
        {
            //CubiTVTranslator translater = new CubiTVTranslator();
            XmlDocument contentXML = translator.TranslateContentDataToXml(contentData, serviceObjectID, serviceConfig, createCategoryIfNotExists, false);

            CallStatus status = restAPI.MakeAddCall("vods", contentXML);
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                // ContentData ret = translator.TranslateXmlToContentData(doc);
                String externalID = doc.SelectSingleNode("vod/id").InnerText;
                ConaxIntegrationHelper.SetCubiTVContentID(this.serviceConfig.ServiceObjectId, contentData, externalID);
                return contentData;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }

        }

        public String GetCubiepgidByExtneralId(String externalId)
        {
            CallStatus status = restAPI.MakeGetCall("epgs?search[external_event_id_equals]=" + externalId, "");
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                if (String.IsNullOrWhiteSpace(status.Data))
                    return null;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);

                XmlNodeList epgNodes = doc.SelectNodes("//epgs/epg");
                foreach (XmlNode epgNode in epgNodes)
                {
                    XmlNode node = epgNode.SelectSingleNode("external-event-id");
                    if (node != null && node.InnerText.Equals(externalId))
                        return epgNode.SelectSingleNode("id").InnerText;
                }
                return null;
            }
            else
            {
                throw new Exception("Call failed on Serivce " + this.serviceConfig.ServiceObjectId + ", error= " + status.Error, status.Exception);
            }
        }

        public List<CubiEPG> GetEPGsReadyForPurge(DateTime searchFrom, DateTime searchTo)
        {

            List<CubiEPG> allCubiEPGs = new List<CubiEPG>();
            UInt32 page = 1;

            while (true)
            {
                List<CubiEPG> CubiEPGs = GetEPGsReadyForPurge(searchFrom, searchTo, page++);
                if (CubiEPGs.Count == 0)
                    break;
                allCubiEPGs.AddRange(CubiEPGs);
            }

            return allCubiEPGs;
        }

        public List<NPVRRecording> GetRecordingsDeletedSince(DateTime dateFrom)
        {
            List<NPVRRecording> NPVRRecordings = new List<NPVRRecording>();
            UInt32 page = 1;
            while (true)
            {
                List<NPVRRecording> recordings = GetRecordingsDeletedSince(dateFrom, page++);
                if (recordings.Count == 0)
                    break;
                foreach (NPVRRecording recording in recordings)
                {
                    if (!NPVRRecordings.Contains(recording))
                        NPVRRecordings.Add(recording);
                }
            }

            return NPVRRecordings;
        }

        private List<NPVRRecording> GetRecordingsDeletedSince(DateTime dateFrom, UInt32 page)
        {
            List<NPVRRecording> NPVRRecordings = new List<NPVRRecording>();

            CallStatus status = restAPI.MakeGetCall(@"npvr_recordings?search[npvr_state_equals]=deleted&search[updated_at_greater_than_or_equal_to]=" + dateFrom.ToString("yyyyMMddTHH:mm:ssZ") + "&page=" + page, "");

            if (status.Success)
            {
                if (String.IsNullOrWhiteSpace(status.Data))
                    return NPVRRecordings;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                NPVRRecordings = translator.TranslateXmlToNPVRRecordings(doc);

            }
            else
            {
                log.Error("Error fetching npvr_recordings that have been deleted");
                throw new Exception("Error fetching npvr_recordings that have been deleted", status.Exception);
            }
            return NPVRRecordings;
        }

        private List<CubiEPG> GetEPGsReadyForPurge(DateTime searchFrom, DateTime searchTo, UInt32 page)
        {

            CallStatus status = restAPI.MakeGetCall("epgs?search[when_all_npvrs_deleted_greater_than_or_equal_to]=" + searchFrom.ToString("yyyyMMddTHH:mm:ssZ") +
                                                        "&search[when_all_npvrs_deleted_less_than_or_equal_to]=" + searchTo.ToString("yyyyMMddTHH:mm:ssZ") + "&page=" + page, "");

            List<CubiEPG> CubiEPGs = new List<CubiEPG>();
            if (status.Success)
            {
                if (String.IsNullOrWhiteSpace(status.Data))
                    return CubiEPGs;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);

                XmlNodeList epgNodes = doc.SelectNodes("//epgs/epg");
                foreach (XmlNode epgNode in epgNodes)
                {
                    CubiEPG epg = new CubiEPG();
                    if (epgNode.SelectSingleNode("external-event-id") != null)
                        epg.ExternalID = epgNode.SelectSingleNode("external-event-id").InnerText;
                    epg.ID = UInt64.Parse(epgNode.SelectSingleNode("id").InnerText);

                    if (!CubiEPGs.Contains(epg))
                        CubiEPGs.Add(epg);

                }
                return CubiEPGs;
            }
            else
            {
                throw new Exception("Call failed on Serivce " + this.serviceConfig.ServiceObjectId + ", error= " + status.Error, status.Exception);
            }
        }

        public XmlDocument GetCatchUpContent(String externalId)
        {
            CallStatus status = restAPI.MakeGetCall("catchup_events", "ext:" + externalId);
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                //ContentData ret = translator.TranslateXmlToContentData(doc);
                return doc;
            }
            else
            {
                if (status.HttpStatusCode == HttpStatusCode.NotFound)
                    return null;
                throw new Exception("Call failed on Serivce " + this.serviceConfig.ServiceObjectId + ", error= " + status.Error, status.Exception);
            }
        }

        private void DeleteCatchupEventsFromCubiWare(ContentData content)
        {
            log.Debug("DeleteContentsFromCubiWare");
            List<Property> publishedToProperties = content.Properties.FindAll(p => p.Type.Equals("ServiceExtContentID", StringComparison.OrdinalIgnoreCase));
            log.Debug("Found " + publishedToProperties.Count.ToString() + " publishedTo properties");
            ICubiTVMWServiceWrapper wrapper = null;
            foreach (Property publishedTo in publishedToProperties)
            {
                try
                {
                    log.Debug("PublishedTo value = " + publishedTo.Value);
                    String[] publishedInfo = publishedTo.Value.Split(':');
                    wrapper = CubiTVMiddlewareManager.Instance(ulong.Parse(publishedInfo[0]));
                    String id = publishedInfo[1];
                    log.Debug("catchupEventID= " + id);
                    if (!wrapper.DeleteContent(ulong.Parse(id)))
                        log.Warn("could not delete catchup event with id " + id);
                    else
                        log.Debug("Catchup event with id " + id + " was deleted");
                }
                catch (Exception ex)
                {
                    log.Warn("Error deleting catchup from cubiware", ex);
                }
            }
        }

        public ContentData CreateLiveChannel(ContentData content)
        {
            //CubiTVTranslator translater = new CubiTVTranslator();
            XmlDocument contentXML = translator.TranslateToChannelXML(content, this.Service);

            CallStatus status = restAPI.MakeAddCall("channels", contentXML);
            if (!status.Success)
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
            //CubiTVTranslator translator = new CubiTVTranslator();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(status.Data);
            String externalID = doc.SelectSingleNode("channel/id").InnerText;
            ConaxIntegrationHelper.SetCubiTVContentID(this.serviceConfig.ServiceObjectId, content, externalID);

            if (ConaxIntegrationHelper.IsCatchUpEnabledChannel(this.serviceConfig.ServiceObjectId, content))
            {
                XmlDocument cuXML = translator.TranslateToCatchUpChannelXML(this.Service, content);
                CallStatus cuStatus = restAPI.MakeAddCall("catchup_channels", cuXML);
                if (!cuStatus.Success)
                {
                    throw new Exception("Call failed, error= " + cuStatus.Error, cuStatus.Exception);
                }
                XmlDocument cuDoc = new XmlDocument();
                cuDoc.LoadXml(cuStatus.Data);
                String cuID = cuDoc.SelectSingleNode("catchup-channel/id").InnerText;
                ConaxIntegrationHelper.SetCubiTVCatchUpId(this.serviceConfig.ServiceObjectId, content, cuID);
            }

            if (ConaxIntegrationHelper.IsNPVREnabledChannel(this.serviceConfig.ServiceObjectId, content))
            {
                log.Debug("NPVR is enabled, creating npvr channel");
                String id = CreateNPVRChannel(content);
                log.Debug("Channel created, id= " + id);
                ConaxIntegrationHelper.SetCubiTVNPVRId(this.serviceConfig.ServiceObjectId, content, id);
            }
            else
            {
                log.Debug("NPVR is not enabled");
            }

            return content;
        }

        public String CreateNPVRChannel(ContentData content)
        {
            //CubiTVTranslator translater = new CubiTVTranslator();
            XmlDocument contentXML = translator.TranslateToNPVRChannelXML(this.Service, content);
            log.Debug("creating npvr channel with xml = " + contentXML.InnerXml);
            CallStatus status = restAPI.MakeAddCall("npvr_channels", contentXML);
            if (!status.Success)
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
            else
            {
                XmlDocument cuDoc = new XmlDocument();
                cuDoc.LoadXml(status.Data);
                String cuID = cuDoc.SelectSingleNode("npvr-channel/id").InnerText;

                return cuID;
            }

        }

        /// <summary>
        /// This method registers a cover in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="cover">The image to create an cover from</param>
        /// <param name="file">The fileInfo of the image</param>
        /// <returns>Returns the ID in CubiTV if the cover was created successfully.</returns>
        public int CreateCover(Image cover, FileInfo file)
        {
            byte[] bytes = File.ReadAllBytes(file.FullName);
            String data = Convert.ToBase64String(bytes);
            //CubiTVTranslator translator = new CubiTVTranslator();
            XmlDocument coverXML = translator.CreateCoverXml(cover, data);
            CallStatus status = restAPI.MakeAddCall("covers", coverXML);
            int ret = 0;
            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                XmlNode idNode = doc.SelectSingleNode("cover/id");
                if (idNode != null && !String.IsNullOrEmpty(idNode.InnerText))
                {
                    int.TryParse(idNode.InnerText, out ret);
                }
                return ret;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }

        }

        /// <summary>
        /// This method fetches a VOD Content in CubiTV Middleware Server.
        /// </summary>
        /// <param name="contentID">The ID of the content to fetch.</param>
        /// <returns>Returns information about the content.</returns>
        public ContentData GetContent(ulong contentID)
        {
            CallStatus status = restAPI.MakeGetCall("vods", contentID.ToString());
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                ContentData ret = translator.TranslateXmlToContentData(doc);
                return ret;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        public XmlDocument GetCatchupChannelByCatchupEvent(XmlDocument sourceDoc)
        {
            XmlNode CatchupChannelNode = sourceDoc.SelectSingleNode("catchup-event/related-channel-id");
            String CatchupChannelId = CatchupChannelNode.InnerText;

            CallStatus status = restAPI.MakeGetCall("catchup_channels", CatchupChannelId);

            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                return doc;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        public ContentData UpdateCatchUpContent(ContentData contentData, XmlDocument sourceDoc, Double keepCatchupAliveInHour, Int32 coverID)
        {
            //CubiTVTranslator translater = new CubiTVTranslator();
            //XmlDocument contentXML = translater.CompleteCatchUpEventXml(contentData, sourceDoc, keepCatchupAliveInHour);
            XmlDocument contentXML = translator.TranslateContentDataToCatchUpEventXml(contentData, sourceDoc, keepCatchupAliveInHour, coverID);
            CallStatus status = restAPI.MakeUpdateCall("catchup_events", "ext:" + contentData.ExternalID, contentXML);

            if (status.Success)
            {
                return contentData;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        /// <summary>
        /// This method updates a VOD Content in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="contentID">The id of the content to update.</param>
        /// <param name="contentData">The object containing the information about the content to update.</param>
        /// <returns>Returns true if the content was updated successfully.</returns>
        public ContentData UpdateContent(ulong contentID, ContentData contentData, ulong serviceObjectID, ServiceConfig seviceConfig, bool createCategoryIfNotExists)
        {

            //CubiTVTranslator translater = new CubiTVTranslator();
            XmlDocument contentXML = translator.TranslateContentDataToXml(contentData, serviceObjectID, serviceConfig, createCategoryIfNotExists, true);
            log.Debug("Update content, xml = " + contentXML.InnerXml);
            CallStatus status = restAPI.MakeUpdateCall("vods", contentID.ToString(), contentXML);
            if (status.Success)
            {
                //  CubiTVTranslator translator = new CubiTVTranslator();
                //  XmlDocument doc = new XmlDocument();
                //   doc.LoadXml(status.Data);
                //ContentData ret = translator.TranslateXmlToContentData(doc);
                return contentData;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        /// <summary>
        /// This method deletes a VOD Content in CubiTV Middleware Server.
        /// </summary>
        /// <param name="contentID">The ID of the content to delete.</param>
        /// <returns>Returns true if the content was deleted successfully.</returns>
        public bool DeleteContent(ulong contentID)
        {
            CallStatus status = restAPI.MakeDeleteCall("products", contentID.ToString());
            if (status.Success)
            {
                return status.Success;
            }
            else
            {
                throw new Exception("Call failed " + ((Int32)status.HttpStatusCode) + " " + status.HttpStatusCode.ToString() + " " + status.Error, status.Exception);
            }
        }

        public bool DeleteEPG(ulong epgID)
        {
            CallStatus status = restAPI.MakeDeleteCall("epgs", epgID.ToString());
            if (status.Success)
            {
                return status.Success;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        /// <summary>
        /// This method deletes a channel in CubiTV Middleware Server.
        /// </summary>
        /// <param name="channelID">The ID of the channel to delete.</param>
        /// <returns>Returns true if the channel was deleted successfully.</returns>
        public bool DeleteChannel(ulong channelID)
        {
            CallStatus status = restAPI.MakeDeleteCall("products", channelID.ToString());
            if (status.Success)
            {
                return status.Success;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }


        /// <summary>
        /// This method fetches a content Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The ID of the content price to fetch data about.</param>
        /// <returns>Information of the price if the call was successful.</returns>
        public MultipleServicePrice GetContentPrice(ulong priceID)
        {
            CallStatus status = restAPI.MakeGetCall("rental_offers", priceID.ToString());
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                MultipleServicePrice ret = translator.TranslateXmlToPriceData(doc);
                return ret;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }

        }


        /// <summary>
        /// This method registers a content Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceData">The object containing the information about the price to add.</param>
        /// <param name="externalContentIDs">A list of one or more external ID to be added to the price</param>
        /// <returns>Returns true if the content was added successfully.</returns>
        public MultipleServicePrice CreateContentPrice(MultipleServicePrice priceData, ContentData content)
        {
            String externalContentID = ConaxIntegrationHelper.GetCubiTVContentID(this.serviceConfig.ServiceObjectId, content);
            String contegoContentID = ConaxIntegrationHelper.GetConaxContegoProductID(priceData);
            //CubiTVTranslator translater = new CubiTVTranslator();
            XmlDocument priceXML = translator.TranslatePriceDataToXml(priceData, content);

            // Use content cover for now
            XmlNode coverNode = priceXML.SelectSingleNode("rental-offer/cover-id");
            coverNode.InnerText = ConaxIntegrationHelper.GetCubiTVContentCoverID(content, this.serviceConfig.ServiceObjectId);

            XmlNode contentIDNode;
            XmlElement newContentIDNode;
            XmlNode removeNode;
            XmlNode rentalOfferNode = priceXML.SelectSingleNode("rental-offer");

            if (ConaxIntegrationHelper.GetContentType(content) == ContentType.VOD)
            {
                contentIDNode = priceXML.SelectSingleNode("rental-offer/vod-ids");
                newContentIDNode = priceXML.CreateElement("vod-id");
                removeNode = rentalOfferNode.SelectSingleNode("channel-ids");
                newContentIDNode.InnerText = externalContentID;
                contentIDNode.AppendChild(newContentIDNode);
                rentalOfferNode.RemoveChild(removeNode);
            }
            else // Live (channel)
            {
                contentIDNode = priceXML.SelectSingleNode("rental-offer/channel-ids");
                newContentIDNode = priceXML.CreateElement("channel-id");
                removeNode = rentalOfferNode.SelectSingleNode("vod-ids");

                newContentIDNode.InnerText = externalContentID;
                contentIDNode.AppendChild(newContentIDNode);

                if (ConaxIntegrationHelper.IsCatchUpEnabledChannel(this.serviceConfig.ServiceObjectId, content))
                {
                    XmlElement cuChannelE = priceXML.CreateElement("channel-id");
                    cuChannelE.InnerText = ConaxIntegrationHelper.GetCubiTVCatchUpId(this.serviceConfig.ServiceObjectId, content);
                    contentIDNode.AppendChild(cuChannelE);
                }

                rentalOfferNode.RemoveChild(removeNode);
            }

            XmlNode conaxOfferNode = priceXML.SelectSingleNode("rental-offer/conax-contego-offer-data-attributes/conax-product-id");
            conaxOfferNode.InnerText = contegoContentID;
            CallStatus status = restAPI.MakeAddCall("rental_offers", priceXML);
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                //MultipleServicePrice ret = translator.TranslateXmlToPriceData(doc);
                SetExternalContentIDOnPrice(priceData, doc);
                return priceData;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        /// <summary>
        /// This method updates a Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The CubiTV priceID of the offer to update</param>
        /// <param name="priceData">The object containing the information to update the price with.</param>
        /// <returns>MultipleServicePrice containing data about the updated content price.</returns>
        public MultipleServicePrice UpdateContentPrice(ulong priceID, MultipleServicePrice priceData, ContentData content)
        {
            log.Debug("In updateContentPrice, fetching content");
            //ContentData content = mppWrapper.GetContentDataByObjectID(priceData.ContentsIncludedInPrice[0]);
            log.Debug("Fetched content");
            //String externalContentID = ConaxIntegrationHelper.GetCubiTVContentID(content);
            String contegoContentID = ConaxIntegrationHelper.GetConaxContegoProductID(priceData);

            //CubiTVTranslator translater = new CubiTVTranslator();
            XmlDocument priceXML = translator.TranslatePriceDataToXml(priceData, content);

            String coverID = ConaxIntegrationHelper.GetCubiTVPriceCoverID(priceData);

            XmlNode rentalOfferNode = priceXML.SelectSingleNode("rental-offer");

            XmlNode conaxOfferNode = priceXML.SelectSingleNode("rental-offer/conax-contego-offer-data-attributes/conax-product-id");
            conaxOfferNode.InnerText = contegoContentID;

            XmlNode vodNode = rentalOfferNode.SelectSingleNode("vod-ids");
            rentalOfferNode.RemoveChild(vodNode); // remove vod node so that is not set

            XmlNode channelNode = rentalOfferNode.SelectSingleNode("channel-ids");
            rentalOfferNode.RemoveChild(channelNode);

            XmlNode coverIDNode = rentalOfferNode.SelectSingleNode("cover-id");
            if (!String.IsNullOrEmpty(coverID))
            {
                log.Debug("adding coverID");
                coverIDNode.InnerText = coverID;
            }
            else
            {
                log.Debug("removing coverID node");
                rentalOfferNode.RemoveChild(coverIDNode);
            }

            // Add content ID to price xml
            //XmlNode contentIDNode = priceXML.SelectSingleNode("rental-offer/vod-ids");
            //XmlElement newContentIDNode = priceXML.CreateElement("vod-id");
            //newContentIDNode.InnerText = externalContentID;
            //contentIDNode.AppendChild(newContentIDNode);

            //XmlNode conaxOfferNode = priceXML.SelectSingleNode("rental-offer/conax-contego-offer-data-attributes/conax-product-id");


            CallStatus status = restAPI.MakeUpdateCall("rental_offers", priceID.ToString(), priceXML);
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(status.Data);
                MultipleServicePrice ret = new MultipleServicePrice();
                //SetExternalContentIDOnPrice(ret, doc);
                return ret;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }


        /// <summary>
        /// This method deletes a Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The ID of the content price to delete.</param>
        /// <returns>True if the content price was deleted successful.</returns>
        public Boolean DeleteContentPrice(ulong priceID)
        {
            CallStatus status = restAPI.MakeDeleteCall("rental_offers", priceID.ToString());
            if (status.Success)
            {
                return status.Success;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        /// <summary>
        /// This method fetches a subscription price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The ID of the subscription price to fetch data about.</param>
        /// <returns>Information of the price if the call was successful.</returns>
        public XmlDocument GetSubscriptionPrice(ulong priceID)
        {
            CallStatus status = restAPI.MakeGetCall("package_offers", priceID.ToString());
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                return doc;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }

        }

        public String GetProfileID(String deviceType)
        {
            CallStatus status = restAPI.MakeGetCall("profiles", "");
            String ret = "";
            bool found = false;
            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                foreach (XmlNode node in doc.SelectNodes("profiles/profile"))
                {
                    if (node.SelectSingleNode("name").InnerText.ToLower().Equals(deviceType.ToLower()))
                    {
                        ret = node.SelectSingleNode("id").InnerText;
                        found = true;
                        break;
                    }
                }
            }
            else
            {
                log.Error("Error when fetching profileID, error= " + status.Error + " errorCode = " + status.ErrorCode);
                throw new Exception("Error when fetching profileID, error= " + status.Error + " errorCode = " + status.ErrorCode);
            }
            if (!found)
            {
                throw new Exception("No profile match for deviceType " + deviceType + "! need to be added to CubiTV");
            }
            return ret;
        }

        /// <summary>
        /// This method checks that all categories are present in cubiTV
        /// </summary>
        /// <param name="properties">All properties containing the categories to check</param>
        /// <returns>True if all catergories are found</returns>
        public bool CheckAllCategories(IEnumerable<Property> properties, ServiceConfig seviceConfig)
        {
            Hashtable categories = GetAllCategories();

            List<String> missingProperties = new List<string>();
            foreach (Property p in properties)
            {
                int id = GetCategoryID(serviceConfig, p.Value, categories, false);
                if (id == 0)
                {
                    if (!missingProperties.Contains(p.Value))
                        missingProperties.Add(p.Value);
                }

            }
            if (missingProperties.Count() > 0)
            {
                log.Error("Missing categories in CubiTV: " + String.Join(", ", missingProperties.ToArray()));
                return false;
            }
            return true;


        }

        /// <summary>
        /// This method checks that all genres are present in cubiTV
        /// </summary>
        /// <param name="properties">All properties containing the genres to check</param>
        /// <returns>True if all genres are found</returns>
        public bool CheckAllGenres(IEnumerable<Property> properties)
        {

            Hashtable allGenres = GetAllGenres();
            List<String> missingProperties = new List<string>();
            foreach (Property p in properties)
            {
                if (!allGenres.ContainsKey(p.Value.ToLower()))
                {
                    if (!missingProperties.Contains(p.Value))
                        missingProperties.Add(p.Value);
                }
            }
            if (missingProperties.Count() > 0)
            {
                log.Error("Missing genres in CubiTV: " + String.Join(", ", missingProperties.ToArray()));
                return false;
            }
            return true;
        }


        public Hashtable GetCategoryIDs(ServiceConfig seviceConfig, List<String> listOfCategories, bool createCategoryIfNotExists)
        {
            log.Debug("Fetching categoryID for " + listOfCategories.Count + " in list");
            Hashtable categories = GetAllCategories();
            Hashtable ret = new Hashtable();
            //  log.Debug("fetched " + categories.Count + " categories");
            foreach (String cat in listOfCategories)
            {
                log.Debug("Fetching id for category " + cat);
                int id = GetCategoryID(serviceConfig, cat, categories, createCategoryIfNotExists);
                if (id != 0)
                {
                    log.Debug("Adding " + cat + " to hashTable with id " + id.ToString());
                    if (!ret.ContainsKey(cat))
                        ret.Add(cat, id);
                }
                else
                {
                    throw new Exception("No category match for category " + cat + "! need to be added to CubiTV");
                }
            }
            return ret;
        }

        //public String GetCategoryID(String category)
        //{
        //    log.Debug("Fetching categoryID for " + category);
        //    Hashtable categories = GetAllCategories();
        //    category = category.ToLower();
        //    log.Debug("fetched " + categories.Count + " categories");
        //    if (categories.ContainsKey(category))
        //    {
        //        String id = categories[category] as String;
        //        log.Debug("returning id = " + id);
        //        return id;
        //    }
        //    else
        //    {
        //        throw new Exception("No category match for category " + category + "! need to be added to CubiTV");
        //    }
        //}

        public string GetServiceId(string serviceType)
        {
            CallStatus status = restAPI.MakeGetCall("services", "");
            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                foreach (XmlNode node in doc.SelectNodes("services/service"))
                {
                    String serviceTypeInNode = node.SelectSingleNode("service-type").InnerText;
                    if (serviceTypeInNode == serviceType)
                    {
                        return node.SelectSingleNode("id").InnerText;
                    }
                }
                // Found no service, must create one
                log.Error("No service exists of type " + serviceType + ", must create one in Cubi admin.");
                throw new Exception("No service exists of type " + serviceType + ", must create one in Cubi admin.");
            }
            else
            {
                log.Error("Error when fetching all services, error= " + status.Error + " errorCode = " + status.ErrorCode);
                throw new Exception("Error when fetching all services, error= " + status.Error + " errorCode = " + status.ErrorCode);
            }
        }

        public Dictionary<String, String> GetAllProfiles()
        {
            CubiTVTranslator translator = new CubiTVTranslator(this);

            Dictionary<String, String> ret = new Dictionary<string, string>();
            CallStatus status = restAPI.MakeGetCall("profiles", "");
            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                foreach (XmlNode node in doc.SelectNodes("profiles/profile"))
                {
                    String profileName = node.SelectSingleNode("name").InnerText;
                    String id = node.SelectSingleNode("id").InnerText;
                    ret.Add(profileName, id);
                }
            }
            return ret;
        }

        private Hashtable GetAllCategories()
        {
            CubiTVTranslator translator = new CubiTVTranslator(this);
            Hashtable ret = new Hashtable();
            CallStatus status = restAPI.MakeGetCall("categories", "");
            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                if (!String.IsNullOrEmpty(status.Data))
                {
                    try
                    {
                        doc.LoadXml(status.Data);
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Error loading categories, continuing", ex);
                    }
                    foreach (XmlNode node in doc.SelectNodes("categories/category"))
                    {
                        String category = node.SelectSingleNode("name").InnerText.ToLower();
                        String id = node.SelectSingleNode("id").InnerText;
                        String parentNodeId = node.SelectSingleNode("parent-id").InnerText;
                        String key = category;
                        if (!String.IsNullOrEmpty(parentNodeId))
                            key += ":" + parentNodeId;
                        key = key.ToLower();
                        if (!String.IsNullOrEmpty(key) && !ret.ContainsKey(key))
                        {
                            Category cat = translator.BuildCategory(node);

                            ret.Add(key, cat);
                        }
                    }
                    int page = 2;
                    while (true)
                    {

                        status = restAPI.MakeGetCall("categories?page=" + page, "");
                        if (status.Success && !String.IsNullOrEmpty(status.Data))
                        {
                            try
                            {
                                doc.LoadXml(status.Data);
                            }
                            catch (Exception ex)
                            {
                                log.Warn("Error loading categories, continuing", ex);
                                break;
                            }
                            foreach (XmlNode node in doc.SelectNodes("categories/category"))
                            {
                                String category = node.SelectSingleNode("name").InnerText.ToLower();
                                String id = node.SelectSingleNode("id").InnerText;
                                String parentNodeId = node.SelectSingleNode("parent-id").InnerText;
                                String key = category;
                                if (!String.IsNullOrEmpty(parentNodeId))
                                    key += ":" + parentNodeId;
                                key = key.ToLower();
                                if (!String.IsNullOrEmpty(key) && !ret.ContainsKey(key))
                                {
                                    Category cat = translator.BuildCategory(node);

                                    ret.Add(key, cat);
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                        page++;
                    }
                }
            }
            else
            {
                log.Error("Error when fetching categoryID, error= " + status.Error + " errorCode = " + status.ErrorCode);
                throw new Exception("Error when fetching categoryID, error= " + status.Error + " errorCode = " + status.ErrorCode);
            }
            return ret;
        }

        private Hashtable GetAllGenres()
        {
            Hashtable ret = new Hashtable();
            CallStatus status = restAPI.MakeGetCall("genres", "");
            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                foreach (XmlNode node in doc.SelectNodes("genres/genre"))
                {
                    String genre = node.SelectSingleNode("name").InnerText.ToLower();
                    if (!String.IsNullOrEmpty(genre) && !ret.ContainsKey(genre))
                    {
                        log.Debug("adding genre " + genre);
                        ret.Add(genre, node.SelectSingleNode("id").InnerText);
                    }
                }
                int page = 2;
                while (true)
                {
                    log.Debug("fetching page " + page + " of genres");
                    status = restAPI.MakeGetCall("genres?page=" + page, "");
                    if (status.Success && !String.IsNullOrEmpty(status.Data))
                    {
                        doc.LoadXml(status.Data);
                        foreach (XmlNode node in doc.SelectNodes("genres/genre"))
                        {
                            String genre = node.SelectSingleNode("name").InnerText.ToLower();
                            if (!String.IsNullOrEmpty(genre) && !ret.ContainsKey(genre))
                            {
                                log.Debug("adding genre " + genre);
                                ret.Add(genre, node.SelectSingleNode("id").InnerText);
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                    page++;
                }

            }
            else
            {
                log.Error("Error when fetching genreID, error= " + status.Error + " errorCode = " + status.ErrorCode);
                throw new Exception("Error when fetching genreID, error= " + status.Error + " errorCode = " + status.ErrorCode);
            }
            return ret;
        }

        public String GetMPAARatingID(String MPAARating)
        {
            CallStatus status = restAPI.MakeGetCall("mpaa_ratings", "");
            String ret = "";
            bool found = false;
            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                foreach (XmlNode node in doc.SelectNodes("mpaa-ratings/mpaa-rating"))
                {
                    if (node.SelectSingleNode("name").InnerText.ToLower().Equals(MPAARating.ToLower()))
                    {
                        ret = node.SelectSingleNode("id").InnerText;
                        found = true;
                        break;
                    }
                }
            }
            else
            {
                log.Error("Error when fetching MPAARatingID, error= " + status.Error + " errorCode = " + status.ErrorCode);
                throw new Exception("Error when fetching MPAARatingID, error= " + status.Error + " errorCode = " + status.ErrorCode);
            }
            if (!found)
            {
                throw new Exception("No MPAARatingID found for " + MPAARating + "! Needs to be added to CubiTV");
            }
            return ret;
        }

        public String GetMexRatingID(String mexRating)
        {
            CallStatus status = restAPI.MakeGetCall("mex_ratings", "");
            String ret = "";
            bool found = false;
            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                foreach (XmlNode node in doc.SelectNodes("mex-ratings/mex-rating"))
                {
                    if (node.SelectSingleNode("name").InnerText.ToLower().Equals(mexRating.ToLower()))
                    {
                        ret = node.SelectSingleNode("id").InnerText;
                        found = true;
                        break;
                    }
                }
            }
            else
            {
                log.Error("Error when fetching MexRatingID, error= " + status.Error + " errorCode = " + status.ErrorCode);
                throw new Exception("Error when fetching MexRatingID, error= " + status.Error + " errorCode = " + status.ErrorCode);
            }
            if (!found)
            {
                throw new Exception("No MexRatingID found for  " + mexRating + "! Needs to be added to CubiTV");
            }
            return ret;
        }

        public String GetVChipRatingID(String vChipRating)
        {
            CallStatus status = restAPI.MakeGetCall("vchip_ratings", "");
            String ret = "";
            bool found = false;
            if (status.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                foreach (XmlNode node in doc.SelectNodes("vchip-ratings/vchip-rating"))
                {
                    if (node.SelectSingleNode("name").InnerText.ToLower().Equals(vChipRating.ToLower()))
                    {
                        ret = node.SelectSingleNode("id").InnerText;
                        found = true;
                        break;
                    }
                }
            }
            else
            {
                log.Error("Error when fetching vChipRating, error= " + status.Error + " errorCode = " + status.ErrorCode);
                throw new Exception("Error when fetching vChipRating, error= " + status.Error + " errorCode = " + status.ErrorCode);
            }
            if (!found)
            {
                throw new Exception("No VChipRating match found for " + vChipRating + "! Needs to be added to CubiTV");
            }
            return ret;
        }

        public int GetCategoryID(ServiceConfig seviceConfig, String categoryName, bool createCategoryIfNotExists)
        {
            return GetCategoryID(serviceConfig, categoryName, GetAllCategories(), createCategoryIfNotExists);
        }

        public int GetCategoryID(ServiceConfig seviceConfig, String categoryName, Hashtable allCategories, bool createCategoryIfNotExists)
        {
            // Hashtable allCategories = GetAllCategories();
            String[] splitCategories = categoryName.Split('/');
            int nodeID = 0;
            if (splitCategories.Count() > 1)
            {
                log.Debug("Category tree");
                for (int i = 0; i < splitCategories.Count(); i++)
                {
                    bool isTopNode = i == 0;
                    //bool isNode = i < splitCategories.Count() - 1;
                    int belongsTo = 0;
                    if (!isTopNode)
                        belongsTo = nodeID;

                    nodeID = GetNodeID(allCategories, splitCategories[i], isTopNode, belongsTo);
                    if (nodeID == 0)
                    {
                        log.Debug("Category " + splitCategories[i] + " doesn't exist");
                        if (!createCategoryIfNotExists)
                            throw new Exception("Category " + splitCategories[i] + " doesn't exist");
                        else
                        {
                            log.Debug("Creating categories for category trees");
                            Category newCategory = CreateCategoryWithTreeNodes(serviceConfig, splitCategories, i, belongsTo, allCategories);
                            string key = newCategory.Name;
                            if (newCategory.ParentID != 0)
                                key += ":" + newCategory.ParentID;
                            key = key.ToLower();
                            if (!allCategories.ContainsKey(key))
                            {
                                log.Debug("Adding new category to Hashtable, id= " + key);
                                allCategories.Add(key, newCategory);
                            }
                            nodeID = newCategory.ID;
                            break;
                        }
                    }
                }
            }
            else
            {
                log.Debug("Not a category tree");
                nodeID = GetFirstMatchingNodeID(allCategories, splitCategories[0]);
                log.Debug("After GetFirstMatchingNodeID, nodeID= " + nodeID.ToString());
                if (nodeID == 0)
                {

                    log.Debug("Category " + splitCategories[0] + " doesn't exist");
                    if (!createCategoryIfNotExists)
                        throw new Exception("Category " + splitCategories[0] + " doesn't exist");
                    else
                    {
                        log.Debug("Category " + splitCategories[0] + " doesn't exist, creating it");
                        Category newCategory = CreateCategoryWithTreeNodes(serviceConfig, splitCategories, 0, 0, allCategories);
                        string key = newCategory.Name;
                        if (newCategory.ParentID != 0)
                            key += ":" + newCategory.ParentID;
                        key = key.ToLower();
                        if (!allCategories.ContainsKey(key))
                        {
                            log.Debug("Adding new category to Hashtable, id= " + key);
                            allCategories.Add(key.ToLower(), newCategory);
                        }
                        nodeID = newCategory.ID;
                    }
                }
            }
            return nodeID;
        }

        private Category CreateCategoryWithTreeNodes(ServiceConfig seviceConfig, string[] categories, int treePossition, int belongsTo, Hashtable allCategories)
        {
            log.Debug("In CreateCategoryWithTreeNodes");
            CubiTVTranslator translator = new CubiTVTranslator(this);
            Category category = null;

            int serviceID = 0;
            int.TryParse(serviceConfig.GetConfigParam("VodServiceID"), out serviceID);
            String defaultCoverLocation = serviceConfig.GetConfigParam("DefaultCoverImage");
            int coverID = 0;
            try
            {
                log.Debug("Creating cover for category");
                coverID = CreateCategoryCover(defaultCoverLocation);
                log.Debug("coverID= " + coverID.ToString());
            }
            catch (Exception ex)
            {
                log.Error("Error creating category cover", ex);
                throw;
            }
            while (treePossition < categories.Count())
            {
                log.Debug("in while loop treePossition = " + treePossition.ToString());
                String categoryName = categories[treePossition];
                categoryName = categoryName.TrimEnd(' ').TrimStart(' ');
                log.Debug("creating category with name " + categoryName);
                String categoryXMLString = CubiTVTranslator.TranslateToCategoryXMLString(categoryName, belongsTo, coverID, categoryName, serviceID);
                log.Debug("XMLSTRING= " + categoryXMLString);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(categoryXMLString);

                CallStatus status = restAPI.MakeAddCall("categories", doc);
                if (status.Success)
                {
                    log.Debug("created category successfully");
                    category = translator.BuildCategory(status.Data);
                    belongsTo = category.ID;
                    string key = category.Name;
                    if (category.ParentID != 0)
                        key += ":" + category.ParentID;
                    key = key.ToLower();
                    if (!allCategories.ContainsKey(key))
                    {
                        log.Debug("Adding new category to Hashtable, id= " + key);
                        allCategories.Add(key.ToLower(), category);
                    }
                    treePossition++;
                }
                else
                {
                    log.Error("Failed to create category in cubitv");
                    throw new Exception("Failed to create category in cubitv");
                }
            }

            return category;
        }

        private int CreateCategoryCover(string defaultCoverLocation)
        {
            Image cover = new Image();
            cover.ClientGUIName = defaultCoverLocation;
            FileInfo fileInfo = new FileInfo(defaultCoverLocation);
            cover.URI = defaultCoverLocation;
            int coverID = CreateCover(cover, fileInfo);
            return coverID;
        }

        private int GetFirstMatchingNodeID(Hashtable allCategories, string category)
        {
            int id = 0;
            Category cat;
            category = category.TrimEnd(' ').TrimStart(' ');
            log.Debug("finding match for " + category);
            String matchKey = "";
            foreach (String key in allCategories.Keys)
            {
                log.Debug("key=" + key);
                if (key.StartsWith(category, StringComparison.OrdinalIgnoreCase))
                {
                    log.Debug("Found match");
                    matchKey = key;
                    break;
                }
            }
            if (!String.IsNullOrEmpty(matchKey))
            {
                try
                {
                    cat = allCategories[matchKey] as Category;
                    log.Debug("id= " + cat.ID.ToString());
                    id = cat.ID;
                }
                catch (Exception ex)
                {
                    log.Error("Error getting categoryID", ex);
                }
            }
            else
            {
                log.Debug("no match found");
            }
            log.Debug("Returning " + id.ToString());
            return id;
        }

        public int GetNodeID(Hashtable table, String treeName, bool isTopNode, int belongsToNode)
        {
            int ret = 0;
            String key = treeName.TrimEnd(' ').TrimStart(' ');
            if (!isTopNode)
            {
                key += ":" + belongsToNode.ToString();
            }
            log.Debug("looking for category with key= " + key);
            Category category = table[key.ToLower()] as Category;
            if (category != null)
            {
                ret = category.ID;
            }
            return ret;
        }


        public Hashtable GetGenreIDs(List<String> listOfGenres)
        {
            log.Debug("fetching genreID for " + listOfGenres.Count + " genres");
            Hashtable genres = GetAllGenres();
            Hashtable ret = new Hashtable();
            log.Debug("fetched " + genres.Count + " genres");
            foreach (String gen in listOfGenres)
            {
                if (genres.ContainsKey(gen))
                {
                    String id = genres[gen] as String;
                    log.Debug("adding id= " + id + " to " + gen);
                    if (!ret.Contains(gen))
                        ret.Add(gen, id);
                }
                else
                {
                    throw new Exception("No genre match for genre " + gen + "! need to be added to CubiTV");
                }
            }
            return ret;
        }


        public String GetGenreID(String genre)
        {
            log.Debug("fetching genreID for " + genre);
            Hashtable genres = GetAllGenres();
            genre = genre.ToLower();
            log.Debug("fetched " + genres.Count + " genres");
            if (genres.ContainsKey(genre))
            {
                String id = genres[genre] as String;
                log.Debug("returning id= " + id);
                return id;
            }
            else
            {
                throw new Exception("No genre match for genre " + genre + "! need to be added to CubiTV");
            }
        }


        /// <summary>
        /// This method registers a subscription Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceData">The object containing the information about the price to add.</param>
        /// <param name="externalContentIDs">A list of one or more external ID to be added to the price</param>
        /// <returns>Returns true if the content was added successfully.</returns>
        public MultipleServicePrice CreateSubscriptionPrice(MultipleServicePrice priceData, ContentData content)
        {
            //CubiTVTranslator translater = new CubiTVTranslator();
            XmlDocument priceXML = translator.TranslateSubscriptionPriceDataToXml(priceData);

            // Add content ID to price xml
            XmlNode subscriptionIDNodeVOD = priceXML.SelectSingleNode("package-offer/vod-ids");
            XmlNode subscriptionIDNodeChannels = priceXML.SelectSingleNode("package-offer/channel-ids");
            XmlNode subscriptionIDNodeCatchupChannels = priceXML.SelectSingleNode("package-offer/catchup-channel-ids");
            XmlNode subscriptionIDNodeServices = priceXML.SelectSingleNode("package-offer/service-ids");

            bool addCatchupChannelServiceToPrice = false;

            //foreach (ulong contentID in priceData.ContentsIncludedInPrice)
            //{
            //    ContentData content = mppWrapper.GetContentDataByObjectID(contentID);
            if (content != null)
            {
                String externalID = ConaxIntegrationHelper.GetCubiTVContentID(this.serviceConfig.ServiceObjectId, content);
                if (!String.IsNullOrEmpty(externalID))
                {
                    if (ConaxIntegrationHelper.GetContentType(content) == ContentType.VOD)
                    {
                        XmlElement newExternalIDNode = priceXML.CreateElement("vod-id");
                        newExternalIDNode.InnerText = externalID;
                        subscriptionIDNodeVOD.AppendChild(newExternalIDNode);
                    }
                    else    // Live
                    {
                        XmlElement newExternalIDNode = priceXML.CreateElement("channel-id");
                        newExternalIDNode.InnerText = externalID;
                        subscriptionIDNodeChannels.AppendChild(newExternalIDNode);
                        if (ConaxIntegrationHelper.IsCatchUpEnabledChannel(this.serviceConfig.ServiceObjectId, content))
                        {
                            XmlElement newCatchupNode = priceXML.CreateElement("catchup-channel-id");
                            newCatchupNode.InnerText = ConaxIntegrationHelper.GetCubiTVCatchUpId(this.serviceConfig.ServiceObjectId, content);
                            subscriptionIDNodeCatchupChannels.AppendChild(newCatchupNode);
                            addCatchupChannelServiceToPrice = true;
                        }
                    }
                }
            }
            //}

            if (addCatchupChannelServiceToPrice)
            {
                string catchupChannelServiceID = GetServiceId("Catchup Channel");
                XmlElement servicePriceE = priceXML.CreateElement("service-id");
                servicePriceE.InnerText = catchupChannelServiceID;
                subscriptionIDNodeServices.AppendChild(servicePriceE);
            }

            XmlNode conaxOfferNode = priceXML.SelectSingleNode("package-offer/conax-contego-offer-data-attributes");

            XmlElement contegoOfferNode = priceXML.CreateElement("conax-product-id");
            contegoOfferNode.InnerText = ConaxIntegrationHelper.GetConaxContegoProductID(priceData);
            conaxOfferNode.AppendChild(contegoOfferNode);

            log.Debug("creating subscriptionPrice with xml= " + priceXML.InnerXml);
            CallStatus status = restAPI.MakeAddCall("package_offers", priceXML);
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);

                SetExternalContentIDOnSubscriptionPrice(priceData, doc);
                return priceData;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        private void SetExternalContentIDOnPrice(MultipleServicePrice price, XmlDocument doc)
        {
            XmlNode idNode = doc.SelectSingleNode("rental-offer/id");
            ConaxIntegrationHelper.SetCubiTVOfferID(price, idNode.InnerText);
        }

        private void SetExternalContentIDOnSubscriptionPrice(MultipleServicePrice price, XmlDocument doc)
        {
            XmlNode idNode = doc.SelectSingleNode("package-offer/id");
            ConaxIntegrationHelper.SetCubiTVOfferID(price, idNode.InnerText);
        }

        /// <summary>
        /// This method updates a Subscription Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The id of the Subscription Price to update</param>
        /// <param name="priceData">The object containing the information to update the price with.</param>
        /// <returns>MultipleServicePrice containing data about the updated Subsription Price.</returns>
        public MultipleServicePrice UpdateSubscriptionPrice(ulong priceID, MultipleServicePrice priceData)
        {
            //CubiTVTranslator translater = new CubiTVTranslator();
            XmlDocument priceXML = translator.TranslateSubscriptionPriceDataToXml(priceData);

            // Add content ID to price xml
            XmlNode subscriptionIDNodeVOD = priceXML.SelectSingleNode("package-offer/vod-ids");
            XmlNode subscriptionIDNodeChannels = priceXML.SelectSingleNode("package-offer/channel-ids");
            XmlNode subscriptionIDNodeCatchupChannels = priceXML.SelectSingleNode("package-offer/catchup-channel-ids");
            XmlNode subscriptionIDNodeServices = priceXML.SelectSingleNode("package-offer/service-ids");

            bool addCatchupChannelServiceToPrice = false;

            foreach (ulong contentID in priceData.ContentsIncludedInPrice)
            {
                ContentData content = mppWrapper.GetContentDataByObjectID(contentID);
                if (content != null)
                {
                    String externalID = ConaxIntegrationHelper.GetCubiTVContentID(this.serviceConfig.ServiceObjectId, content);
                    if (!String.IsNullOrEmpty(externalID))
                    {
                        if (ConaxIntegrationHelper.GetContentType(content) == ContentType.VOD)
                        {
                            XmlElement newExternalIDNode = priceXML.CreateElement("vod-id");
                            newExternalIDNode.InnerText = externalID;
                            subscriptionIDNodeVOD.AppendChild(newExternalIDNode);
                        }
                        else    // Live
                        {
                            XmlElement newExternalIDNode = priceXML.CreateElement("channel-id");
                            newExternalIDNode.InnerText = externalID;
                            subscriptionIDNodeChannels.AppendChild(newExternalIDNode);
                            if (ConaxIntegrationHelper.IsCatchUpEnabledChannel(this.serviceConfig.ServiceObjectId, content))
                            {
                                XmlElement newCatchupNode = priceXML.CreateElement("catchup-channel-id");
                                newCatchupNode.InnerText = ConaxIntegrationHelper.GetCubiTVCatchUpId(this.serviceConfig.ServiceObjectId, content);
                                subscriptionIDNodeCatchupChannels.AppendChild(newCatchupNode);
                                addCatchupChannelServiceToPrice = true;
                            }
                        }
                    }
                }
            }

            if (addCatchupChannelServiceToPrice)
            {
                string catchupChannelServiceID = GetServiceId("Catchup Channel");
                XmlElement servicePriceE = priceXML.CreateElement("service-id");
                servicePriceE.InnerText = catchupChannelServiceID;
                subscriptionIDNodeServices.AppendChild(servicePriceE);
            }

            XmlNode conaxOfferNode = priceXML.SelectSingleNode("package-offer/conax-contego-offer-data-attributes");

            XmlElement contegoOfferNode = priceXML.CreateElement("conax-product-id");
            contegoOfferNode.InnerText = ConaxIntegrationHelper.GetConaxContegoProductID(priceData);
            conaxOfferNode.AppendChild(contegoOfferNode);

            log.Debug("makeUpdateCall priceID = " + priceID.ToString() + " xml = " + priceXML.InnerXml);
            CallStatus status = restAPI.MakeUpdateCall("package_offers", priceID.ToString(), priceXML);
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                XmlDocument doc = new XmlDocument();
                //doc.LoadXml(status.Data);
                //MultipleServicePrice ret = translator.TranslateXmlToPriceData(doc);
                ConaxIntegrationHelper.SetCubiTVOfferID(priceData, priceID.ToString());
                return priceData;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        public MultipleServicePrice UpdateSubscriptionPrice(MultipleServicePrice priceData)
        {
            return UpdateSubscriptionPrice(priceData, null, UpdatePackageOfferType.UpdatePrice);
        }

        public MultipleServicePrice UpdateSubscriptionPrice(MultipleServicePrice priceData, ContentData contentData, UpdatePackageOfferType type)
        {
            // Get packageoffer form Cubi
            String priceID = ConaxIntegrationHelper.GetCubiTVOfferID(priceData);
            List<String> productIds = new List<String>();
            XmlDocument packageOfferDoc = GetSubscriptionPrice(UInt64.Parse(priceID));
            XmlNodeList vodIdNodes = packageOfferDoc.SelectNodes("//package-offer/vods/vod/id");
            foreach (XmlNode vodIdNode in vodIdNodes)
                productIds.Add(vodIdNode.InnerText);
            XmlNodeList channelIdNodes = packageOfferDoc.SelectNodes("//package-offer/channels/channel/id");
            foreach (XmlNode channelIdNode in channelIdNodes)
                productIds.Add(channelIdNode.InnerText);

            // add new content to cubis list
            if (type == UpdatePackageOfferType.AddContent)
            {
                String externalID = ConaxIntegrationHelper.GetCubiTVContentID(this.serviceConfig.ServiceObjectId, contentData);
                productIds.Add(externalID);
            }

            XmlDocument priceXML = translator.TranslateSubscriptionPriceDataToXml(priceData);
            XmlNode subscriptionIDNodeVOD = priceXML.SelectSingleNode("package-offer/vod-ids");
            XmlNode subscriptionIDNodeChannels = priceXML.SelectSingleNode("package-offer/channel-ids");
            XmlNode subscriptionIDNodeCatchupChannels = priceXML.SelectSingleNode("package-offer/catchup-channel-ids");
            XmlNode subscriptionIDNodeServices = priceXML.SelectSingleNode("package-offer/service-ids");
            XmlNode subscriptionIdNodeNpvrChannels = priceXML.SelectSingleNode("package-offer/npvr-channel-ids");

            bool addCatchupChannelServiceToPrice = false;
            bool addNpvrChannelServiceToPrice = false;
            foreach (ulong contentID in priceData.ContentsIncludedInPrice)
            {
                ContentData content = mppWrapper.GetContentDataByObjectID(contentID);
                if (content != null)
                {
                    String externalID = ConaxIntegrationHelper.GetCubiTVContentID(this.serviceConfig.ServiceObjectId, content);
                    if (!String.IsNullOrEmpty(externalID))
                    {
                        // check if this exsit in cubis list
                        if (!productIds.Contains(externalID))
                            continue; // this content doesn't eisit in cubis list, skip it.

                        if (ConaxIntegrationHelper.GetContentType(content) == ContentType.VOD)
                        {
                            XmlElement newExternalIDNode = priceXML.CreateElement("vod-id");
                            newExternalIDNode.InnerText = externalID;
                            subscriptionIDNodeVOD.AppendChild(newExternalIDNode);
                        }
                        else    // Live
                        {
                            XmlElement newExternalIDNode = priceXML.CreateElement("channel-id");
                            newExternalIDNode.InnerText = externalID;
                            subscriptionIDNodeChannels.AppendChild(newExternalIDNode);
                            if (ConaxIntegrationHelper.IsCatchUpEnabledChannel(this.serviceConfig.ServiceObjectId, content))
                            {
                                XmlElement newCatchupNode = priceXML.CreateElement("catchup-channel-id");
                                newCatchupNode.InnerText = ConaxIntegrationHelper.GetCubiTVCatchUpId(this.serviceConfig.ServiceObjectId, content);
                                subscriptionIDNodeCatchupChannels.AppendChild(newCatchupNode);
                                addCatchupChannelServiceToPrice = true;
                            }
                            if (ConaxIntegrationHelper.IsNPVREnabledChannel(this.serviceConfig.ServiceObjectId, content))
                            {
                                XmlElement newNpvrNode = priceXML.CreateElement("npvr-channel-id");
                                newNpvrNode.InnerText = ConaxIntegrationHelper.GetCubiTVNPVRId(this.serviceConfig.ServiceObjectId, content);
                                subscriptionIdNodeNpvrChannels.AppendChild(newNpvrNode);
                                addNpvrChannelServiceToPrice = true;
                            }
                        }
                    }
                }
            }

            if (addCatchupChannelServiceToPrice)
            {
                string catchupChannelServiceID = GetServiceId("Catchup Channel");
                XmlElement servicePriceE = priceXML.CreateElement("service-id");
                servicePriceE.InnerText = catchupChannelServiceID;
                subscriptionIDNodeServices.AppendChild(servicePriceE);
            }
            if (addNpvrChannelServiceToPrice)
            {
                string npvrChannelServiceID = GetServiceId("NPVR");
                XmlElement servicePriceE = priceXML.CreateElement("service-id");
                servicePriceE.InnerText = npvrChannelServiceID;
                subscriptionIDNodeServices.AppendChild(servicePriceE);
            }



            XmlNode conaxOfferNode = priceXML.SelectSingleNode("package-offer/conax-contego-offer-data-attributes");

            XmlElement contegoOfferNode = priceXML.CreateElement("conax-product-id");
            contegoOfferNode.InnerText = ConaxIntegrationHelper.GetConaxContegoProductID(priceData);
            conaxOfferNode.AppendChild(contegoOfferNode);

            log.Debug("makeUpdateCall priceID = " + priceID.ToString() + " xml = " + priceXML.InnerXml);
            CallStatus status = restAPI.MakeUpdateCall("package_offers", priceID.ToString(), priceXML);
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                XmlDocument doc = new XmlDocument();
                //doc.LoadXml(status.Data);
                //MultipleServicePrice ret = translator.TranslateXmlToPriceData(doc);
                ConaxIntegrationHelper.SetCubiTVOfferID(priceData, priceID.ToString());
                return priceData;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }

        /// <summary>
        /// This method deletes a Subscription Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The ID of the content price to delete.</param>
        /// <returns>True if the Subscription Price was deleted successful.</returns>
        public Boolean DeleteSubscriptionPrice(ulong priceID)
        {
            CallStatus status = restAPI.MakeDeleteCall("package_offers", priceID.ToString());
            if (status.Success)
            {
                return status.Success;
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }


        public void AddContentToPackageOffer(MultipleServicePrice servicePrice, ContentData content)
        {

            MultipleServicePrice newPrice = null;
            try
            {
                String cubiTVPriceID = ConaxIntegrationHelper.GetCubiTVOfferID(servicePrice);
                if (!String.IsNullOrEmpty(cubiTVPriceID))
                {
                    newPrice = UpdateSubscriptionPrice(servicePrice, content, UpdatePackageOfferType.AddContent);
                }
                else
                {
                    newPrice = CreateSubscriptionPrice(servicePrice, content);
                }
            }
            catch (Exception e)
            {
                log.Error("Error when creating new subscription price", e);
                throw;
            }
            if (newPrice != null)
            {
                mppWrapper.UpdateServicePrice(newPrice);
            }
        }

        //public void HandleSubscriptionPrice(MultipleServicePrice servicePrice)
        //{

        //    //String externalContentID = ConaxIntegrationHelper.GetCubiTVContentID(content);

        //    MultipleServicePrice newPrice = null;
        //    try
        //    {
        //        String cubiTVPriceID = ConaxIntegrationHelper.GetCubiTVOfferID(servicePrice);
        //        if (!String.IsNullOrEmpty(cubiTVPriceID))
        //        {
        //            newPrice = UpdateSubscriptionPrice(ulong.Parse(cubiTVPriceID), servicePrice);
        //        }
        //        else
        //        {
        //            newPrice = CreateSubscriptionPrice(servicePrice);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        log.Error("Error when creating new subscription price", e);
        //        throw;
        //    }
        //    if (newPrice != null)
        //    {
        //        mppWrapper.UpdateServicePrice(newPrice);
        //    }

        //}

        public void HandleContentPrice(MultipleServicePrice servicePrice, ContentData content)
        {


            MultipleServicePrice newPrice = null;
            try
            {
                newPrice = CreateContentPrice(servicePrice, content);
            }
            catch (Exception e)
            {
                log.Error("Error when creating new content price", e);
                throw;
            }
            if (newPrice != null)
            {
                mppWrapper.UpdateServicePrice(newPrice);
            }

        }

        public List<NPVRRecording> GetNPVRRecording(String externalId)
        {

            List<NPVRRecording> allNPVRRecordings = new List<NPVRRecording>();
            UInt32 page = 1;
            
            while (true)
            {
                List<NPVRRecording> NPVRRecordings = GetNPVRRecording(new List<String> { externalId }, page++);
                if (NPVRRecordings.Count == 0)
                    break;
                allNPVRRecordings.AddRange(NPVRRecordings);
            }

            return allNPVRRecordings;
        }

        public List<NPVRRecording> GetNPVRRecording(List<String> externalIds, Int32 threadsToStart)
        {
            List<NPVRRecording> allNPVRRecordings = new List<NPVRRecording>();
            UInt32 page = 1;

            List<System.Threading.Tasks.Task<List<NPVRRecording>>> TPLTasks = new List<System.Threading.Tasks.Task<List<NPVRRecording>>>();
            // create number of threads to start
            for (; page <= threadsToStart; page++)
            {
                UInt32 _page = page;
                System.Threading.Tasks.Task<List<NPVRRecording>> getNPVRRecordingTask = System.Threading.Tasks.Task<List<NPVRRecording>>.Factory.StartNew(() =>
                {
                    ThreadContext.Properties["TaskName"] = "FetchNewEPGWithRecordingTask";
                    List<NPVRRecording> NPVRRecordings = GetNPVRRecording(externalIds, _page);                    
                    return NPVRRecordings;
                }, TaskCreationOptions.LongRunning);
                TPLTasks.Add(getNPVRRecordingTask);
            }
            while (TPLTasks.Count > 0)
            {
                Int32 taskIndex = System.Threading.Tasks.Task.WaitAny(TPLTasks.ToArray());
                Boolean emptyPagereached = false;

                try
                {
                    var res = TPLTasks[taskIndex].Result;                    
                    if (res.Count > 0)
                        allNPVRRecordings.AddRange(res);
                    else
                        emptyPagereached = true;
                }
                catch (AggregateException aex)
                {
                    aex = aex.Flatten();                    
                    foreach (Exception ex in aex.InnerExceptions)                    
                        log.Error(ex.Message, ex);
                }
                catch (Exception ex)
                {
                    log.Error("Problem handle Task result  " + ex.Message, ex);
                }
                TPLTasks.RemoveAt(taskIndex);

                if (!emptyPagereached) { // empty page not reached yet, still recordings to get.
                    UInt32 _page = page++;
                    System.Threading.Tasks.Task<List<NPVRRecording>> getNPVRRecordingTask = System.Threading.Tasks.Task<List<NPVRRecording>>.Factory.StartNew(() =>
                    {
                        ThreadContext.Properties["TaskName"] = "FetchNewEPGWithRecordingTask";
                        List<NPVRRecording> NPVRRecordings = GetNPVRRecording(externalIds, _page);
                        return NPVRRecordings;
                    }, TaskCreationOptions.LongRunning);
                    TPLTasks.Add(getNPVRRecordingTask);
                }
            }

            return allNPVRRecordings;
        }

        public List<NPVRRecording> GetNPVRRecording(List<String> externalIds) {

            List<NPVRRecording> allNPVRRecordings = new List<NPVRRecording>();
            UInt32 page = 1;

            while (true)
            {
                List<NPVRRecording> NPVRRecordings = GetNPVRRecording(externalIds, page++);
                if (NPVRRecordings.Count == 0)
                    break;
                allNPVRRecordings.AddRange(NPVRRecordings);
            }

            return allNPVRRecordings;
        }

        private List<NPVRRecording> GetNPVRRecording(List<String> externalIds, UInt64 page)
        {
            var wfmConfig = Config.GetConaxWorkflowManagerConfig();
            List<NPVRRecording> NPVRRecordings = new List<NPVRRecording>();
            String apiParam = "npvr_recordings?search[npvr_state_equals]=" + NPVRRecordStateInCubiware.to_record.ToString();
            foreach(String externalId in externalIds) {
                apiParam += "&search[epg_external_event_id_in][]=" + externalId;
            }
            apiParam += "&per_page=" + wfmConfig.MaxGetNPVRRecordingsPerPage + "&page=" + page;

            CallStatus status = restAPI.MakeGetCall(apiParam, "");

            if (status.Success)
            {
                if (String.IsNullOrWhiteSpace(status.Data))
                    return NPVRRecordings;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);
                NPVRRecordings = translator.TranslateXmlToNPVRRecordings(doc);

                return NPVRRecordings;
            }
            else
            {
                throw new Exception("Call failed on Serivce " + this.serviceConfig.ServiceObjectId + ", error= " + status.Error, status.Exception);
            }
        }

        public void UpdateNPVRRecording(Dictionary<ContentData, List<NPVRRecording>> recordingsPerContent)
        {
            List<List<NPVRRecording>> allRecordings = new List<List<NPVRRecording>>();
            XmlDocument npvrXML = new XmlDocument();
            // gather up all recordings and generate bulk update xml
            foreach (KeyValuePair<ContentData, List<NPVRRecording>> kvp in recordingsPerContent) {
                List<NPVRRecording> recordings = kvp.Value;
                Dictionary<String, List<NPVRRecording>> groupedRecordings = GenerateNPVRTask.GroupRecordingsPerGuardTimes(recordings);
                allRecordings.AddRange(groupedRecordings.Values);
            }
            npvrXML = translator.TranslateContentDataToNPVRXml(allRecordings);
                       
            log.Debug("Update NPVR, xml = " + npvrXML.InnerXml);
            CallStatus status = restAPI.MakeAddCall("npvr_recordings/bulk_update_contents", npvrXML);
            if (status.Success)
            {
                //  CubiTVTranslator translator = new CubiTVTranslator();
                //  XmlDocument doc = new XmlDocument();
                //   doc.LoadXml(status.Data);
                //ContentData ret = translator.TranslateXmlToContentData(doc);
            }
            else
            {
                throw new Exception("Call failed, " + ((Int32)status.HttpStatusCode) + " " + status.HttpStatusCode.ToString() + " " + status.Error, status.Exception);
            }             
        }

        public void UpdateNPVRRecording(ContentData content, List<NPVRRecording> recordings, NPVRRecordStateInCubiware recordState)
        {
            Dictionary<String, List<NPVRRecording>> groupedRecordings = GenerateNPVRTask.GroupRecordingsPerGuardTimes(recordings);
            XmlDocument npvrXML = translator.TranslateContentDataToNPVRXml(content, groupedRecordings, recordState);
                       
            log.Debug("Update NPVR, xml = " + npvrXML.InnerXml);
            CallStatus status = restAPI.MakeAddCall("npvr_recordings/bulk_update_contents", npvrXML);
            if (status.Success)
            {
                //  CubiTVTranslator translator = new CubiTVTranslator();
                //  XmlDocument doc = new XmlDocument();
                //   doc.LoadXml(status.Data);
                //ContentData ret = translator.TranslateXmlToContentData(doc);
            }
            else
            {
                throw new Exception("Call failed, error= " + status.Error, status.Exception);
            }
        }


       
        public bool DeleteCatchupEvent(string externalId)
        {
            try
            {
                XmlDocument catchupdoc = GetCatchUpContent(externalId);
                if (catchupdoc == null)
                {
                    log.Warn("Could not find catchup with externalId " + externalId + " so can't delete");
                    return false;
                }
                String cubiContentID = catchupdoc.SelectSingleNode("catchup-event/id").InnerText;
                if (String.IsNullOrEmpty(cubiContentID))
                    return false;

                return DeleteContent(ulong.Parse(cubiContentID));
            }
            catch (Exception exc)
            {
                throw new Exception(
                    "Call failed on Service " + this.serviceConfig.ServiceObjectId + ", error= " + exc.Message, exc);
            }
        }

        #region ICubiTVMWServiceWrapper Members


        public Hashtable GetListOfCubiEpgIds(List<string> externalIds)
        {
            ConaxWorkflowManagerConfig workflowConfig =
                Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault() as
                    ConaxWorkflowManagerConfig;
            int noToFetchPerCall = workflowConfig.MaxExternalIdsToSendPerCall;
            Hashtable ret = new Hashtable();
            List<List<String>> chunks = CommonUtil.SplitIntoChunks(externalIds, noToFetchPerCall);
            foreach (List<String> subList in chunks)
            {
                String parameterString = "";
                foreach (string externalId in subList)
                {
                    if (String.IsNullOrEmpty(parameterString))
                        parameterString += "?search[external_event_id_in][]=" + externalId;
                    else
                        parameterString += "&search[external_event_id_in][]=" + externalId;
                }
                AddCubiEpgIds(ret, parameterString, noToFetchPerCall);
            }
            return ret;
        }

        //private void AddCubiEpgIds(Hashtable ret, string parameterString, ConaxWorkflowManagerConfig workflowConfig)
        //{
        //    UInt32 page = 1;
        //    int repliesPerPage = workflowConfig.MaxReplyPerPageForExternalIdsCall;
        //    while (true)
        //    {
        //        if (!AddCubiEpgIds(ret, parameterString, page, repliesPerPage))
        //            break;
        //        page++;
        //    }
        //}

        private bool AddCubiEpgIds(Hashtable ret, string parameterString, int noToFetchPerCall)
        {

            CallStatus status = restAPI.MakeGetCall("epgs" + parameterString + "&per_page=" + noToFetchPerCall, "");
            List<String> multiplehit = new List<String>();
            if (status.Success)
            {
                //CubiTVTranslator translator = new CubiTVTranslator();
                if (String.IsNullOrWhiteSpace(status.Data))
                    return false;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(status.Data);

                XmlNodeList epgNodes = doc.SelectNodes("//epgs/epg");
                foreach (XmlNode epgNode in epgNodes)
                {
                    XmlNode node = epgNode.SelectSingleNode("external-event-id");
                    if (node != null && epgNode.SelectSingleNode("id") != null)
                    {
                        if (!ret.ContainsKey(node.InnerText)) {
                            ret.Add(node.InnerText, epgNode.SelectSingleNode("id").InnerText);
                        }
                        else { // this is an incorect behaviour, exteran-id should be unique to one epg only.
                            if (!multiplehit.Contains(node.InnerText))
                                multiplehit.Add(node.InnerText);
                        }
                    }
                }

                foreach(String dupplicateId in multiplehit) {
                    log.Error("Multiple EPG was found with same external id " + dupplicateId + ", please correct it in MPP and Cubi.");
                    ret.Remove(dupplicateId);
                }

                return true;
            }
            return false;
        }

        #endregion
    }

    public class MiddleWareRestApiCaller : IMiddleWareRestApiCaller
    {
        private String _baseURL;

        private String _key;

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MiddleWareRestApiCaller(String baseURL, String key)
        {
            _baseURL = baseURL;
            _key = key;
        }

        /// <summary>
        /// This method fetches a object from the rest apy
        /// </summary>
        /// <param name="objectToHandle">The object type to handle, ie products, services</param>
        /// <param name="id">The id of the object to fetch data from, if "" is sent all will be fetched.</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeGetCall(String objectToHandle, String id)
        {
            String address = "";
            try
            {
                String parameter = "";
                if (!String.IsNullOrEmpty(id))
                {
                    parameter = "/" + id;
                }
                address = _baseURL + "/rest/" + objectToHandle + parameter;
                log.Debug("Making GetCall to address" + Environment.NewLine + address);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
               
                String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
           
                request.Method = "GET";
                request.MediaType = "application/xml";
                request.Accept = "*/*";
                WebResponse r = request.GetResponse();
                Stream response = r.GetResponseStream();
                StreamReader sr = new StreamReader(response);
                String reply = sr.ReadToEnd();
                sr.Close();
                response.Close();
                CallStatus status = new CallStatus();
                status.Success = true;
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                if (objectToHandle != "catchup_events")
                    log.Error("WebException making GetCall to address" + Environment.NewLine + address);
                CallStatus status = new CallStatus();
                status.Success = false;
                if (ex.Response != null)
                {
                    string sResponse = "";
                    Stream strm = ex.Response.GetResponseStream();
                    byte[] bytes = new byte[strm.Length];
                    strm.Read(bytes, 0, (int)strm.Length);
                    sResponse = System.Text.Encoding.ASCII.GetString(bytes).Trim();
                    status = XmlReplyParser.ParseReply(sResponse);
                    status.Error = (!String.IsNullOrEmpty(sResponse)) ? sResponse : ex.Message;
                    status.HttpStatusCode = ((HttpWebResponse) ex.Response).StatusCode;
                }
                else
                {
                    status.Error = ex.Message + ":" + ex.StackTrace;
                }
                return status;
            }
            catch (Exception e)
            {
                log.Error("Exception making GetCall to address" + Environment.NewLine + address);
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Error = "Failed on " + address + " " + e.Message;
                status.Exception = e;
                return status;
            }
        }

        public CallStatus MakeAddCall(String objectToHandle, XmlDocument data)
        {
            return MakeAddCall(objectToHandle, data.InnerXml);
        }

        /// <summary>
        /// Creates a object of the specified type.
        /// </summary>
        /// <param name="objectToHandle">The object type to create, ie products, services</param>
        /// <param name="data">The data to use when creating the object</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeAddCall(String objectToHandle, String xmlString)
        {
            string address = "";
            try
            {
                address = _baseURL + "/rest/" + objectToHandle;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                request.Method = "POST";
                request.Accept = "*/*";
                request.ProtocolVersion = HttpVersion.Version11;

                //String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><vod><name>Matrix special</name><cover-id>2767</cover-id><cast></cast><director></director><producer></producer><screenplay></screenplay><runtime>3465466</runtime><description>Nice movie!</description><extended-description>More about this movie!</extended-description><release-date type=\"date\"></release-date><country></country><mpaa-rating-id></mpaa-rating-id><vchip-rating-id></vchip-rating-id><content-rating-ids type=\"array\"><content-rating-id></content-rating-id></content-rating-ids><category-ids type=\"array\"><category-id></category-id></category-ids><genre-ids type=\"array\"><genre-id></genre-id></genre-ids><contents-attributes type='array'><content><profile-id>1</profile-id><trailer>http://video.server.com/content/354632</trailer><source>http://video.server.com/content/967323</source></content></contents-attributes></vod>";
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(xmlString);
                byte[] d = Encoding.UTF8.GetBytes(xmlString);

                if (!(objectToHandle.Equals("covers") || objectToHandle.Equals("epgs/import")))
                {
                    log.Debug("<------------------------------------------------>");
                    log.Debug("Adding " + address + " to cubitv using xml= " + xmlString);
                    log.Debug("<------------------------------------------------>");
                }
                if (objectToHandle.Equals("epgs/import"))
                {
                    try
                    {
                        log.Debug("Sending Epgs to Cubiware on adress " + address);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                request.MediaType = "application/xml";

                request.ContentType = "application/xml;charset=UTF-8";
                request.ContentLength = d.Length;

                Stream requestStream = request.GetRequestStream();

                // Send the request
                requestStream.Write(d, 0, d.Length);
                requestStream.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                String reply = sr.ReadToEnd();

                sr.Close();
                response.Close();
                CallStatus status = new CallStatus();
                status.Success = true;
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                if (ex.Response != null)
                {
                    string sResponse = "";
                    Stream strm = ex.Response.GetResponseStream();
                    byte[] bytes = new byte[strm.Length];
                    strm.Read(bytes, 0, (int)strm.Length);
                    sResponse = System.Text.Encoding.ASCII.GetString(bytes);
                    status.Error = sResponse;
                    status.HttpStatusCode = ((HttpWebResponse)ex.Response).StatusCode;
                    status.Exception = ex;
                }
                return status;
            }
            catch (Exception e)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Error = "Failed on " + address + " " + e.Message;
                status.Exception = e;
                return status;
            }
        }

        public CallStatus MakeUpdateCall(String objectToHandle, String id, XmlDocument data)
        {
            return MakeUpdateCall(objectToHandle, id, data.InnerXml);
        }

        /// <summary>
        /// Updates the specified object.
        /// </summary>
        /// <param name="objectToHandle">The object type to updatae, ie products, services</param>
        /// <param name="id">The id of the object to update</param>
        /// <param name="data">The data to update with, ie content data if creating or updating content</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeUpdateCall(String objectToHandle, String id, String xmlString)
        {
            String address = "";
            try
            {
                address = _baseURL + "/rest/" + objectToHandle + "/" + id;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                request.Method = "PUT";
                request.Accept = "*/*";
                request.ProtocolVersion = HttpVersion.Version11;

                // String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Rental-Offer><Name>Buy Matrix</Name><Price>16.5</Price><Rental-Period>01:00:00</Rental-Period><Cover-Id>1</Cover-Id><Vod-Ids type=\"array\"><Vod-Id>475</Vod-Id></Vod-Ids></Rental-Offer>";
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(xmlString);
                byte[] d = Encoding.UTF8.GetBytes(xmlString);

                log.Debug("<------------------------------------------------>");
                log.Debug("Update " + address + " to cubitv using xml= " + xmlString);
                log.Debug("<------------------------------------------------>");

                request.MediaType = "application/xml";

                request.ContentType = "application/xml;charset=UTF-8";

                request.ContentLength = d.Length;

                Stream requestStream = request.GetRequestStream();

                // Send the request
                requestStream.Write(d, 0, d.Length);
                requestStream.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                String reply = sr.ReadToEnd();
                sr.Close();
                response.Close();

                CallStatus status = new CallStatus();
                status.Success = true;
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                log.Error("Error when calling update package offer with xml = " + xmlString, ex);
                CallStatus status = new CallStatus();
                status.Success = false;
                if (ex.Response != null)
                {
                    string sResponse = "";
                    Stream strm = ex.Response.GetResponseStream();
                    byte[] bytes = new byte[strm.Length];
                    strm.Read(bytes, 0, (int)strm.Length);
                    sResponse = System.Text.Encoding.ASCII.GetString(bytes);
                    status.Error = sResponse;
                }
                return status;
            }
            catch (Exception e)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Error = "Failed on " + address + " " + e.Message;
                status.Exception = e;
                return status;
            }
        }

        /// <summary>
        /// Deletes an object.
        /// </summary>
        /// <param name="objectToHandle">The object type to delete, ie products, services</param>
        /// <param name="id">The id of the object to delete</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeDeleteCall(String objectToHandle, String id)
        {
            String address = "";
            try
            {
                address = _baseURL + "/rest/" + objectToHandle + "/" + id;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                log.Debug("adress for deletecall " + address);
                String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                request.Method = "DELETE";
                request.Accept = "*/*";
                WebResponse r = request.GetResponse();
                Stream response = r.GetResponseStream();
                StreamReader sr = new StreamReader(response);
                String reply = sr.ReadToEnd();
                sr.Close();
                response.Close();

                CallStatus status = new CallStatus();
                status.Success = true;
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                if (ex.Response != null)
                {
                    string sResponse = "";
                    Stream strm = ex.Response.GetResponseStream();
                    byte[] bytes = new byte[strm.Length];
                    strm.Read(bytes, 0, (int)strm.Length);
                    sResponse = System.Text.Encoding.ASCII.GetString(bytes);
                    status.Error = sResponse;
                    status.HttpStatusCode = ((HttpWebResponse)ex.Response).StatusCode;
                    status.Exception = ex;
                }
                return status;
            }
            catch (Exception e)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Error = "Failed on " + address + " " + e.Message;
                status.Exception = e;
                return status;
            }
        }
    }

    public class XmlReplyParser
    {

        /// <summary>
        /// This method parses the reply from the rest api
        /// </summary>
        /// <param name="reply"></param>
        /// <returns></returns>
        public static CallStatus ParseReply(String reply)
        {
            CallStatus ret = new CallStatus();

            return new CallStatus();
        }

    }

    public class Category
    {
        public int TreeID { get; set; }

        public int ID { get; set; }

        public String Name { get; set; }

        public String CoverID { get; set; }

        public String Description { get; set; }

        public int ParentID { get; set; }

        public int IconID { get; set; }

        public int ThumbnailID { get; set; }
    }

    public class CallStatus
    {
        public CallStatus() {
            HttpStatusCode = System.Net.HttpStatusCode.Unused;
        }

        public bool Success;

        public int ErrorCode;

        public String Data;

        public String Error;

        public Exception Exception;

        public HttpStatusCode HttpStatusCode;
    }

    public enum UpdatePackageOfferType
    {
        UpdatePrice,
        AddContent,
        RemoveContent
    }
}
