using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using log4net;
using System.Reflection;
using System.Security;
using System.Collections;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Translation;
using System.Diagnostics;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins
{
    public class CubiTVTranslator
    {


        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        MovieRatingFormats movieRatingFormat = CommonUtil.GetSystemMovieRatingFormat();
        TVRatingFormats tvRatingFormat = CommonUtil.GetSystemTVRatingFormat();

        #region IXmlTranslate Members


        ICubiTVMWServiceWrapper wrapper = null;

        public CubiTVTranslator(ICubiTVMWServiceWrapper wrapper)
        {
            this.wrapper = wrapper;
        }

        public XmlDocument CompleteCatchUpEventXml(ContentData contentData, XmlDocument sourceDoc, Double keepCatchupAliveInHour)
        {

            try
            {
                //var channelProeprty = contentData.Properties.FirstOrDefault(p => p.Type.Equals("Channel", StringComparison.OrdinalIgnoreCase));
                var feedTimezoneProperty = contentData.Properties.FirstOrDefault(p => p.Type.Equals("FeedTimezone", StringComparison.OrdinalIgnoreCase));
                TimeZoneInfo feedtimeZone;
                try
                {
                    feedtimeZone = TimeZoneInfo.FindSystemTimeZoneById(feedTimezoneProperty.Value);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to find timeZone with id " + feedTimezoneProperty.Value, ex);
                }

                DateTime startTime = TimeZoneInfo.ConvertTime(contentData.EventPeriodTo.Value,
                                                              TimeZoneInfo.Utc,
                                                              feedtimeZone);
                DateTime endTime = TimeZoneInfo.ConvertTime(contentData.EventPeriodTo.Value,
                                                            TimeZoneInfo.Utc,
                                                            feedtimeZone);
                // append alive time
                endTime = endTime.AddHours(keepCatchupAliveInHour);


                sourceDoc.SelectSingleNode("catchup-event/name").InnerText = contentData.Name;


                XmlElement contentsAttributesNode = (XmlElement)sourceDoc.SelectSingleNode("catchup-event/contents");
                contentsAttributesNode.RemoveAll();
                contentsAttributesNode.SetAttribute("type", "array");
                foreach (Asset asset in contentData.Assets)
                {
                    var properties = asset.Properties.Where(p => p.Type.Equals("DeviceType", StringComparison.OrdinalIgnoreCase));
                    foreach (Property p in properties)
                    {
                        String profileId = wrapper.GetProfileID(p.Value);

                        XmlElement contentNode = sourceDoc.CreateElement("content");
                        XmlElement profileIdNode = sourceDoc.CreateElement("profile-id");
                        profileIdNode.InnerText = profileId;
                        profileIdNode.SetAttribute("type", "integer");
                        contentNode.AppendChild(profileIdNode);

                        XmlElement sourceNode = sourceDoc.CreateElement("source");
                        sourceNode.InnerText = asset.Name;
                        contentNode.AppendChild(sourceNode);

                        XmlElement highDefinitionNode = sourceDoc.CreateElement("high-definition");
                        highDefinitionNode.SetAttribute("type", "boolean");
                        highDefinitionNode.InnerText = "false";
                        contentNode.AppendChild(highDefinitionNode);

                        contentsAttributesNode.AppendChild(contentNode);
                    }
                }

                XmlElement catchupEventAvailabilitiesAttributesNode = (XmlElement)sourceDoc.SelectSingleNode("catchup-event/catchup-event-availabilities");
                catchupEventAvailabilitiesAttributesNode.RemoveAll();
                catchupEventAvailabilitiesAttributesNode.SetAttribute("type", "array");
                foreach (Asset asset in contentData.Assets)
                {
                    var properties = asset.Properties.Where(p => p.Type.Equals("DeviceType", StringComparison.OrdinalIgnoreCase));
                    foreach (Property p in properties)
                    {
                        String profileId = wrapper.GetProfileID(p.Value);

                        XmlElement catchupEventAvailabilityNode = sourceDoc.CreateElement("catchup-event-availability");
                        XmlElement profileIdNode = sourceDoc.CreateElement("profile-id");
                        profileIdNode.InnerText = profileId;
                        profileIdNode.SetAttribute("type", "integer");
                        catchupEventAvailabilityNode.AppendChild(profileIdNode);

                        XmlElement availablefromNode = sourceDoc.CreateElement("available-from");
                        availablefromNode.InnerText = startTime.ToString("yyyyMMddHHmmss");
                        availablefromNode.SetAttribute("type", "datetime");
                        catchupEventAvailabilityNode.AppendChild(availablefromNode);

                        XmlElement availableTillNode = sourceDoc.CreateElement("available-till");
                        availableTillNode.InnerText = endTime.ToString("yyyyMMddHHmmss");
                        availableTillNode.SetAttribute("type", "datetime");
                        catchupEventAvailabilityNode.AppendChild(availableTillNode);

                        catchupEventAvailabilitiesAttributesNode.AppendChild(catchupEventAvailabilityNode);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Error when translating content xml", e);
                throw;
            }

            return sourceDoc;
        }

        public List<NPVRRecording> TranslateXmlToNPVRRecordings(XmlDocument contentXml)
        {
            List<NPVRRecording> recordings = new List<NPVRRecording>();
            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(s => s.SystemName == SystemConfigNames.ConaxWorkflowManager);

            foreach(XmlNode recordingNode in contentXml.SelectNodes("//npvr-recordings/npvr-recording")) {

                NPVRRecording recording = new NPVRRecording();

                XmlNode idNode = recordingNode.SelectSingleNode("id");
                XmlNode epgidNode = recordingNode.SelectSingleNode("epg/id");
                XmlNode startNode = recordingNode.SelectSingleNode("start-at");
                XmlNode endNode = recordingNode.SelectSingleNode("end-at");
                XmlNode epgExternalId = recordingNode.SelectSingleNode("epg/external-event-id");
                XmlNode recordState = recordingNode.SelectSingleNode("npvr-state");

                recording.Id = Int32.Parse(idNode.InnerText);
                recording.EpgId = Int32.Parse(epgidNode.InnerText);
                recording.Start = DateTime.Parse(startNode.InnerText).ToUniversalTime();
                recording.End = DateTime.Parse(endNode.InnerText).ToUniversalTime();
                if (epgExternalId != null)
                    recording.EPGExternalID = epgExternalId.InnerText;
                recording.RecordState = (NPVRRecordStateInCubiware)Enum.Parse(typeof(NPVRRecordStateInCubiware), recordState.InnerText, true);

                // append default pre and post guards.
                recording.Start = recording.Start.Value.AddSeconds(-1 * managerConfig.NPVRRecordingPreGuardInSec);
                recording.End = recording.End.Value.AddSeconds(managerConfig.NPVRRecordingPostGuardInSec);

                recordings.Add(recording);
            }
            return recordings;
        }

        public XmlDocument TranslateContentDataToNPVRXml(ContentData contentData, Dictionary<String, List<NPVRRecording>> groupedRecordings, NPVRRecordStateInCubiware recordState)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                //var deviceAndAssetMapping = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "DeviceAndAssetMapping").SingleOrDefault();
                //IEnumerable<KeyValuePair<String, String>> deviceAssetMap = deviceAndAssetMapping.ConfigParams;
                XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", "");
                doc.AppendChild(declaration);

                XmlElement bulkNode = doc.CreateElement("npvr-recordings-bulk-update-contents");
                XmlAttribute attribute = doc.CreateAttribute("type");
                attribute.Value = "array";
                bulkNode.Attributes.Append(attribute);
                doc.AppendChild(bulkNode);
                foreach (List<NPVRRecording> recordings in groupedRecordings.Values)
                {
                    NPVRRecording recording = recordings[0];

                    foreach (NPVRRecordingSource source in recording.Sources)
                    {
                        //AssetFormatType formatType = (AssetFormatType)Enum.Parse(typeof(AssetFormatType), kvp.Value, true);

                        //if (formatType == AssetFormatType.SmoothStreaming && String.IsNullOrEmpty(recording.SSURL))
                        //{
                        //    continue;
                        //}
                        //else if (formatType == AssetFormatType.HTTPLiveStreaming && String.IsNullOrEmpty(recording.HLSURL))
                        //{
                        //    continue;
                        //}
                        XmlElement contentNode = doc.CreateElement("npvr-recordings-bulk-update-content");
                        bulkNode.AppendChild(contentNode);
                        String url = source.Url;
                        //if (formatType == AssetFormatType.SmoothStreaming)
                        //    url = recording.SSURL;
                        //else
                        //    url = recording.HLSURL;
                        XmlElement profileNameNode = doc.CreateElement("profile-name");
                        profileNameNode.InnerText = source.Device.ToString();
                        contentNode.AppendChild(profileNameNode);
                        XmlElement npvrRecordingNode = doc.CreateElement("npvr-recording-attributes");
                        contentNode.AppendChild(npvrRecordingNode);

                        XmlElement npvrStateNode = doc.CreateElement("npvr-state");
                        npvrStateNode.InnerText = recordState.ToString();
                        npvrRecordingNode.AppendChild(npvrStateNode);
                        if (recordState != NPVRRecordStateInCubiware.failed)
                        {
                            XmlElement attributeNode = doc.CreateElement("content-attributes");
                            contentNode.AppendChild(attributeNode);

                            XmlElement sourceNode = doc.CreateElement("source");
                            sourceNode.InnerText = url;
                            attributeNode.AppendChild(sourceNode);

                            XmlElement definitionNode = doc.CreateElement("high-definition");
                            definitionNode.InnerText = "false";
                            attributeNode.AppendChild(definitionNode);
                        }
                        else
                        {
                            XmlElement npvrFailedReasonNode = doc.CreateElement("failure-reason");
                            npvrFailedReasonNode.InnerText = "5";
                            npvrRecordingNode.AppendChild(npvrFailedReasonNode);
                        }
                        XmlElement recordingsNode = doc.CreateElement("recording-ids");
                        XmlAttribute recordingsAttribute = doc.CreateAttribute("type");
                        recordingsAttribute.Value = "array";
                        recordingsNode.Attributes.Append(recordingsAttribute);
                        contentNode.AppendChild(recordingsNode);

                        foreach (NPVRRecording rec in recordings)
                        {
                            XmlElement recordingNode = doc.CreateElement("recording-id");
                            recordingNode.InnerText = rec.Id.Value.ToString();
                            recordingsNode.AppendChild(recordingNode);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Error when translating NPVR xml", e);
                throw;
            }

            return doc;
        }

        public XmlDocument TranslateContentDataToNPVRXml(List<List<NPVRRecording>> groupedRecordings)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", "");
                doc.AppendChild(declaration);

                XmlElement bulkNode = doc.CreateElement("npvr-recordings-bulk-update-contents");
                XmlAttribute attribute = doc.CreateAttribute("type");
                attribute.Value = "array";
                bulkNode.Attributes.Append(attribute);
                doc.AppendChild(bulkNode);                
                foreach (List<NPVRRecording> recordings in groupedRecordings)
                {
                    NPVRRecording recording = recordings[0];

                    foreach (NPVRRecordingSource source in recording.Sources)
                    {
                        XmlElement contentNode = doc.CreateElement("npvr-recordings-bulk-update-content");
                        bulkNode.AppendChild(contentNode);
                        String url = source.Url;

                        XmlElement profileNameNode = doc.CreateElement("profile-name");
                        profileNameNode.InnerText = source.Device.ToString();
                        contentNode.AppendChild(profileNameNode);
                        XmlElement npvrRecordingNode = doc.CreateElement("npvr-recording-attributes");
                        contentNode.AppendChild(npvrRecordingNode);

                        XmlElement npvrStateNode = doc.CreateElement("npvr-state");
                        npvrStateNode.InnerText = recording.RecordState.ToString();
                        npvrRecordingNode.AppendChild(npvrStateNode);
                        if (recording.RecordState != NPVRRecordStateInCubiware.failed)
                        {
                            XmlElement attributeNode = doc.CreateElement("content-attributes");
                            contentNode.AppendChild(attributeNode);

                            XmlElement sourceNode = doc.CreateElement("source");
                            sourceNode.InnerText = url;
                            attributeNode.AppendChild(sourceNode);

                            XmlElement definitionNode = doc.CreateElement("high-definition");
                            definitionNode.InnerText = "false";
                            attributeNode.AppendChild(definitionNode);
                        }
                        else
                        {
                            XmlElement npvrFailedReasonNode = doc.CreateElement("failure-reason");
                            npvrFailedReasonNode.InnerText = "5";
                            npvrRecordingNode.AppendChild(npvrFailedReasonNode);
                        }
                        XmlElement recordingsNode = doc.CreateElement("recording-ids");
                        XmlAttribute recordingsAttribute = doc.CreateAttribute("type");
                        recordingsAttribute.Value = "array";
                        recordingsNode.Attributes.Append(recordingsAttribute);
                        contentNode.AppendChild(recordingsNode);

                        foreach (NPVRRecording rec in recordings)
                        {
                            XmlElement recordingNode = doc.CreateElement("recording-id");
                            recordingNode.InnerText = rec.Id.Value.ToString();
                            recordingsNode.AppendChild(recordingNode);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Error when translating NPVR xml", e);
                throw;
            }

            return doc;
        }

        public XmlDocument TranslateContentDataToCatchUpEventXml(ContentData contentData, XmlDocument sourceDoc, Double keepCatchupAliveInHour, Int32 coverID)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                //var channelProeprty = contentData.Properties.FirstOrDefault(p => p.Type.Equals("Channel", StringComparison.OrdinalIgnoreCase));
                var feedTimezoneProperty = contentData.Properties.FirstOrDefault(p => p.Type.Equals("FeedTimezone", StringComparison.OrdinalIgnoreCase));

                DateTime startTime = contentData.EventPeriodTo.Value;

                DateTime endTime = contentData.EventPeriodTo.Value;

                // append alive time
                endTime = endTime.AddHours(keepCatchupAliveInHour);


                String XMLTVXML = "";

                XMLTVXML += "<catchup-event>";

                XMLTVXML += "<id type=\"integer\">" + sourceDoc.SelectSingleNode("catchup-event/id").InnerText + "</id>";
                XMLTVXML += "<epg-id type=\"integer\">" + sourceDoc.SelectSingleNode("catchup-event/epg-id").InnerText + "</epg-id>";
                XMLTVXML += "<cover-id type=\"integer\">" + coverID + "</cover-id>";
                XMLTVXML += "<catchup-channel-id type=\"integer\">" + sourceDoc.SelectSingleNode("catchup-event/related-channel-id").InnerText + "</catchup-channel-id>";
                XMLTVXML += "<name>" + SecurityElement.Escape(contentData.Name) + "</name>";
                XMLTVXML += "<contents-attributes type=\"array\">";

                // get catchup assets
                var catchupAssets =
                   contentData.Assets.Where(
                       a => a.Properties.Count(p => p.Type.Equals(CatchupContentProperties.AssetType) &&
                                                    p.Value.ToLower().Equals(AssetType.Catchup.ToString().ToLower())) > 0);
                foreach (Asset asset in catchupAssets)
                {
                    var properties = asset.Properties.Where(p => p.Type.Equals(CatchupContentProperties.DeviceType, StringComparison.OrdinalIgnoreCase));
                    foreach (Property p in properties)
                    {
                        String profileId = wrapper.GetProfileID(p.Value);
                        XMLTVXML += "<content>";
                        XMLTVXML += "<profile-id>" + profileId + "</profile-id>";
                        XMLTVXML += "<source>" + SecurityElement.Escape(asset.Name) + "</source>";
                        XMLTVXML += "<high-definition type=\"boolean\">false</high-definition>";
                        XMLTVXML += "<conax-contego-content-data-attributes>";
                        XMLTVXML += "<conax-content-id>" + contentData.Properties.First(pr => pr.Type.Equals(CatchupContentProperties.ConaxContegoContentID, StringComparison.OrdinalIgnoreCase)).Value + "</conax-content-id>";
                        XMLTVXML += "</conax-contego-content-data-attributes>";
                        XMLTVXML += "</content>";
                    }
                }
                XMLTVXML += "</contents-attributes><catchup-event-availabilities-attributes type=\"array\">";
                foreach (Asset asset in catchupAssets)
                {
                    var properties = asset.Properties.Where(p => p.Type.Equals(CatchupContentProperties.DeviceType, StringComparison.OrdinalIgnoreCase));
                    foreach (Property p in properties)
                    {
                        String profileId = wrapper.GetProfileID(p.Value);
                        XMLTVXML += "<catchup-event-availability>";
                        XMLTVXML += "<profile-id>" + profileId + "</profile-id>";
                        XMLTVXML += "<available-from>" + startTime.ToString("yyyyMMddHHmmss") + "</available-from>";
                        XMLTVXML += "<available-till>" + endTime.ToString("yyyyMMddHHmmss") + "</available-till>";
                        XMLTVXML += "</catchup-event-availability>";
                    }
                }
                XMLTVXML += "</catchup-event-availabilities-attributes></catchup-event>";


                doc.LoadXml(XMLTVXML);
            }
            catch (Exception e)
            {
                log.Error("Error when translating content xml", e);
                throw;
            }
            return doc;
        }

        public XmlDocument TranslateContentDataToXMLTVXml(List<ContentData> contents, UInt64 serviceObjectId, Double keepCatchupAliveInHour)
        {
           
            XmlDocument doc = new XmlDocument();
            String programmeNodes = "";
            try
            {
                foreach (ContentData contentData in contents)
                {
                    EPGChannel epgChannel = EPGIngestTask.GetCachedEPGChannel(contentData);
                    if (!ConaxIntegrationHelper.IsPublishedToService(serviceObjectId, epgChannel))
                        continue;
                    var cubiChannelIdProeprty =
                        contentData.Properties.FirstOrDefault(
                            p =>
                                p.Type.Equals(CatchupContentProperties.CubiChannelId, StringComparison.OrdinalIgnoreCase));
                    var enableCatchUpProperty =
                        contentData.Properties.FirstOrDefault(
                            p =>
                                p.Type.Equals(CatchupContentProperties.EnableCatchUp, StringComparison.OrdinalIgnoreCase));
                    var enableNPVRProperty =
                        contentData.Properties.FirstOrDefault(
                            p => p.Type.Equals(CatchupContentProperties.EnableNPVR, StringComparison.OrdinalIgnoreCase));
                    var feedTimezoneProperty =
                        contentData.Properties.FirstOrDefault(
                            p =>
                                p.Type.Equals(CatchupContentProperties.FeedTimezone, StringComparison.OrdinalIgnoreCase));
                    //enableCatchUpProperty.Value = "True";


                    DateTime startTime = contentData.EventPeriodFrom.Value;

                    DateTime endTime = contentData.EventPeriodTo.Value;

                    programmeNodes += "<programme start=\"" + startTime.ToString("yyyyMMddHHmmss") +
                                "\" stop=\"" + endTime.ToString("yyyyMMddHHmmss") +
                                "\" channel=\"" + cubiChannelIdProeprty.Value + "\">";

                    foreach (LanguageInfo languageInfo in contentData.LanguageInfos)
                    {
                        programmeNodes += "<title>" + SecurityElement.Escape(languageInfo.Title) + "</title>";
                        if (!String.IsNullOrEmpty(languageInfo.LongDescription))
                            programmeNodes += "<desc>" + SecurityElement.Escape(languageInfo.LongDescription) + "</desc>";
                        else if (!String.IsNullOrEmpty(languageInfo.ShortDescription))
                            programmeNodes += "<desc>" + SecurityElement.Escape(languageInfo.ShortDescription) + "</desc>";
                    }

                    //var episodeProperty = contentData.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.EPGIEpisodeInformation) && p.Value.Contains("dd_progid"));
                    //if (episodeProperty != null)
                    //{
                    //    XMLTVXML += "<episode-num system=\"dd_progid\">EP00003022.00665</episode-num>"; // episodeProperty.Value;
                    //}
                    var EPGIEpisodeInformationProerpties =
                        contentData.Properties.Where(p => p.Type.Equals(CatchupContentProperties.EPGIEpisodeInformation));
                    if (EPGIEpisodeInformationProerpties != null)
                    {
                        foreach (Property EPGIEpisodeInformationProperty in EPGIEpisodeInformationProerpties)
                        {
                            Int32 pos = EPGIEpisodeInformationProperty.Value.IndexOf(":");
                            String type = EPGIEpisodeInformationProperty.Value.Substring(0, pos);
                            String value = EPGIEpisodeInformationProperty.Value.Substring(pos + 1);

                            programmeNodes += "<episode-num system=\"" + type + "\">" + value + "</episode-num>";
                                // episodeProperty.Value;
                        }
                    }

                    String recordableString = "0";
                    //Property enableNPVR = contentData.Properties.FirstOrDefault(pr => pr.Type.Equals(CatchupContentProperties.EnableNPVR));
                    //if (enableNPVRProperty != null)
                    //{
                    //    bool recordable;
                    //    bool.TryParse(enableNPVRProperty.Value, out recordable);
                    //    if (recordable)
                    //        recordableString = "1";
                    //}
                    if (epgChannel.ServiceEpgConfigs[serviceObjectId].EnableNpvr)
                    {
                        if (enableNPVRProperty != null)
                        {
                            bool recordable;
                            bool.TryParse(enableNPVRProperty.Value, out recordable);
                            if (recordable)
                                recordableString = "1";
                        }
                    }
                    programmeNodes += "<recordable>" + recordableString + "</recordable>";

                    programmeNodes += AddRatingsToXmlTvXml(contentData);


                    //if (Boolean.Parse(enableNPVRProperty.Value))
                    //{ // add NPVR data
                    programmeNodes += "<external-event-id>" + contentData.ExternalID + "</external-event-id>";
                    //}

                    double catchupHour = keepCatchupAliveInHour;
                    var property =
                        contentData.Properties.FirstOrDefault(
                            p => p.Type.Equals("CatchUpHours", StringComparison.OrdinalIgnoreCase));
                    if (property != null)
                    {
                        catchupHour = double.Parse(property.Value);
                    }
                    //if (Boolean.Parse(enableCatchUpProperty.Value))
                    if (epgChannel.ServiceEpgConfigs[serviceObjectId].EnableCatchup &&
                        Boolean.Parse(enableCatchUpProperty.Value))
                    {
                        // catchup specific data
                        DateTime cuStart = endTime;
                        DateTime cuEnd = endTime.AddHours(catchupHour);
                        programmeNodes += "<catch-up>";
                        programmeNodes += "<event-id>" + contentData.ExternalID + "</event-id>";
                        programmeNodes += "<available-from>" + cuStart.ToString("yyyyMMddHHmmss") + "</available-from>";
                        programmeNodes += "<available-till>" + cuEnd.ToString("yyyyMMddHHmmss") + "</available-till>";
                        programmeNodes += "</catch-up>";
                    }

                    programmeNodes += "</programme>";


                }
                if (programmeNodes != "")
                    doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?><tv>" + programmeNodes + "</tv>");
              
               // stopWatch.Stop();
                //log.Debug("Building epg xml took " + stopWatch.ElapsedMilliseconds.ToString() + "ms for " +
                //          contents.Count.ToString() + " contents");
            }
            catch (Exception e)
            {
                log.Error("Error when translating content xml, programmeNodes: " + programmeNodes, e);
                throw;
            }
            return doc;
        }

        private String AddRatingsToXmlTvXml(ContentData content)
        {
            var movieRating = CommonUtil.GetRatingForContent(content, VODnLiveContentProperties.MovieRating);
            var tvRating = CommonUtil.GetRatingForContent(content, VODnLiveContentProperties.TVRating);
            
            var xmlString = "";

            if (!String.IsNullOrEmpty(movieRating))
            {
                xmlString += "<rating system=\"" + movieRatingFormat + "\">";
                xmlString += "<value>" + movieRating + "</value>";
                xmlString += "</rating>";
            }
            if (!String.IsNullOrEmpty(tvRating))
            {
                xmlString += "<rating system=\"" + tvRatingFormat + "\">";
                xmlString += "<value>" + tvRating.Replace("None","") + "</value>";
                xmlString += "</rating>";
            }

            return xmlString;
        }

        public XmlDocument TranslateContentDataToXml(ContentData contentData, ulong serviceObjectID, ServiceConfig serviceConfig, bool createCategoryIfNotExists, bool updateCall)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                var conaxConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
                Property metadataMappingProperty = contentData.Properties.SingleOrDefault<Property>(p => p.Type.Equals("MetadataMappingConfigurationFileName"));
                if (metadataMappingProperty == null || String.IsNullOrEmpty(metadataMappingProperty.Value))
                {
                    log.Error("Datamappigvalidation for CubiTV failed, No metadatamapping property was found!");
                }

                String propertyName = "";
                doc.LoadXml(ContentXmlTemplate);
                XmlNode nameNode = doc.SelectSingleNode("vod/name");
                String title = GetTitle(contentData);
                nameNode.InnerText = title;

                HandleAssets(doc, contentData, serviceObjectID, updateCall);

                //var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "CubiTV").SingleOrDefault();
                String coverID = ConaxIntegrationHelper.GetCubiTVContentCoverID(contentData, serviceObjectID);
                XmlNode coverNode = doc.SelectSingleNode("vod/cover-id");
                //var ContentPropertyConfig = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "ContentProperty").SingleOrDefault();

                if (!String.IsNullOrEmpty(coverID))
                {
                    coverNode.InnerText = coverID;
                }
                else
                {
                    doc.SelectSingleNode("vod").RemoveChild(coverNode);
                }
                XmlNode castNode = doc.SelectSingleNode("vod/cast");
                //propertyName = Config.GetConfig().CastPropertyName;
                propertyName = VODnLiveContentProperties.Cast;
                Property property = contentData.Properties.SingleOrDefault<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                if (property != null)
                {
                    castNode.InnerText = property.Value.Replace(';', ',');
                }
                //handle casts with properties

                XmlNode directorNode = doc.SelectSingleNode("vod/director");
                //propertyName = Config.GetConfig().DirectorPropertyName;
                propertyName = VODnLiveContentProperties.Director;
                var directors = contentData.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));

                List<String> l = new List<String>();
                foreach (Property p in directors)
                {
                    l.Add(p.Value);
                }
                directorNode.InnerText = String.Join(",", l.ToArray());
               

                XmlNode producerNode = doc.SelectSingleNode("vod/producer");
                //propertyName = Config.GetConfig().ProducerPropertyName;
                propertyName = VODnLiveContentProperties.Producer;
                var producers = contentData.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                l = new List<String>();
                foreach (Property p in producers)
                {
                    l.Add(p.Value);
                }

                producerNode.InnerText = String.Join(",", l.ToArray());

                XmlNode screenPlayNode = doc.SelectSingleNode("vod/screenplay");
                //propertyName = Config.GetConfig().ScreenPlayPropertyName;
                propertyName = VODnLiveContentProperties.ScreenPlay;
                var screenPlays = contentData.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                l = new List<String>();
                foreach (Property p in screenPlays)
                {
                    l.Add(p.Value);
                }
                screenPlayNode.InnerText = String.Join(",", l.ToArray());
                //Handle screenPlay with properties
                
               

                AddRatingsToContentXml(doc, contentData, metadataMappingProperty, serviceObjectID);

               







                XmlNode categorysNode = doc.SelectSingleNode("vod/category-ids");
                //propertyName = Config.GetConfig().CategoryPropertyName;
                propertyName = VODnLiveContentProperties.Category;
                IEnumerable<Property> properties = contentData.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                String translatedCategoryPropertyValue = "";
                List<String> values = new List<string>();
                foreach (Property p in properties)
                {
                    translatedCategoryPropertyValue = MetadataMappingHelper.GetCategoryForService(metadataMappingProperty.Value, serviceObjectID, p.Value);
                    values.Add(translatedCategoryPropertyValue);
                }
                Hashtable catTable = wrapper.GetCategoryIDs(serviceConfig, values, createCategoryIfNotExists);
                foreach (int id in catTable.Values)
                {
                    XmlNode categoryNode = doc.CreateElement("category-id");
                    log.Debug("adding id= " + id);
                    try
                    {
                        categoryNode.InnerText = id.ToString();
                        log.Debug("Set categoryID on node to " + categoryNode.InnerText);
                        categorysNode.AppendChild(categoryNode);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error getting category from hashtable", ex);
                    }
                    
                }
                // fix with properties

                XmlNode genresNode = doc.SelectSingleNode("vod/genre-ids");
                //propertyName = Config.GetConfig().GenrePropertyName;
                propertyName = VODnLiveContentProperties.Genre;
                properties = contentData.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                String translatedGenrePropertyValue = "";
                values = new List<string>();
                foreach (Property p in properties)
                {
                    translatedGenrePropertyValue = MetadataMappingHelper.GetGenreForService(metadataMappingProperty.Value, serviceObjectID, p.Value);
                    values.Add(translatedGenrePropertyValue.ToLower());
                }
                Hashtable genTable = wrapper.GetGenreIDs(values);
                foreach (String key in genTable.Keys)
                {
                    XmlNode genreNode = doc.CreateElement("genre-id");
                    genreNode.InnerText = genTable[key] as String;
                    log.Debug("Set genreID on node to " + genreNode.InnerText);
                    genresNode.AppendChild(genreNode);
                }
                // fix with properties

                XmlNode runtimeNode = doc.SelectSingleNode("vod/runtime");

                try
                {
                    int minutes = (int)contentData.RunningTime.Value.TotalMinutes;
                    string runtime = minutes.ToString();
                    runtimeNode.InnerText = runtime;
                }
                catch (Exception exc)
                {
                    log.Warn("Error parsing runningtime, continuing ingesting", exc);
                }


                if (contentData.LanguageInfos.Count > 0)
                {
                    XmlNode descriptionNode = doc.SelectSingleNode("vod/description");
                    descriptionNode.InnerText = contentData.LanguageInfos[0].ShortDescription;

                    XmlNode extendedDescriptionNode = doc.SelectSingleNode("vod/extended-description");

                    extendedDescriptionNode.InnerText = contentData.LanguageInfos[0].LongDescription;
                }
                XmlNode releaseDateNode = doc.SelectSingleNode("vod/year");
                if (contentData.ProductionYear != 0)
                {
                    String date = contentData.ProductionYear.ToString();
                  
                    releaseDateNode.InnerText = date;
                }


                XmlNode countryNode = doc.SelectSingleNode("vod/country");

                propertyName = VODnLiveContentProperties.Country;
                property = contentData.Properties.SingleOrDefault<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                if (property != null)
                {
                    countryNode.InnerText = property.Value;
                }

                log.Debug("Translated XML = " + doc.InnerXml);

            }
            catch (Exception e)
            {
                log.Error("Error when translating content xml", e);
                throw;
            }
            return doc;
        }


        private XmlDocument AddRatingsToContentXml(XmlDocument doc, ContentData content, Property metadataMappingProperty, ulong serviceObjectId)
        {
            var movieRating = CommonUtil.GetRatingForContent(content, VODnLiveContentProperties.MovieRating);
            var tvRating = CommonUtil.GetRatingForContent(content, VODnLiveContentProperties.TVRating);

            var errorMessage = "";
                
            if (movieRating == null && tvRating == null)
            {
                errorMessage = "Cannot create content xml to update CubiTV. Both MovieRating and TVRating are missing.";
                log.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            var translatedMovieRatingValue = movieRating != null
                                                 ? MetadataMappingHelper.GetRatingForService(
                                                     metadataMappingProperty.Value, serviceObjectId, movieRating,
                                                     VODnLiveContentProperties.MovieRating)
                                                 : null;

            var translatedTVRatingValue = tvRating != null
                                                  ? MetadataMappingHelper.GetRatingForService(
                                                      metadataMappingProperty.Value, serviceObjectId, tvRating,
                                                      VODnLiveContentProperties.TVRating)
                                                  : null;

            if (String.IsNullOrEmpty(translatedMovieRatingValue) && String.IsNullOrEmpty(translatedTVRatingValue))
            {
                errorMessage = "Cannot create content xml to update CubiTV. Translations for both MovieRating and TVRating is missing.";
                log.Error(errorMessage);
                throw new Exception(errorMessage);
            }
                
            if (!String.IsNullOrEmpty(translatedMovieRatingValue))
            {
               // var movieRatingFormat = CommonUtil.GetSystemMovieRatingFormat();
                
                if (movieRatingFormat == MovieRatingFormats.MPAA)
                {
                    var node = doc.SelectSingleNode("vod/mpaa-rating-id");
                    if(node != null)
                        node.InnerText = wrapper.GetMPAARatingID(translatedMovieRatingValue);
                    
                }
                else if (movieRatingFormat == MovieRatingFormats.MEX)
                {
                    var node = doc.SelectSingleNode("vod/mex-rating-id");
                    if (node != null)
                        node.InnerText = wrapper.GetMexRatingID(translatedMovieRatingValue);
                }
                
            }
            if (!String.IsNullOrEmpty(translatedTVRatingValue))
            {
                //var tvRatingFormat = CommonUtil.GetSystemTVRatingFormat();
                
                if (tvRatingFormat == TVRatingFormats.VCHIP)
                {
                    var node = doc.SelectSingleNode("vod/vchip-rating-id");
                    if (node != null)
                        node.InnerText = wrapper.GetVChipRatingID(translatedTVRatingValue);
                }
            }
            doc = RemoveEmptyRatingNodesFromXml(doc);
            
            return doc;

        }

        private XmlDocument RemoveEmptyRatingNodesFromXml(XmlDocument doc)
        {
            var nodes = new List<XmlNode>
            {
                doc.SelectSingleNode("vod/mpaa-rating-id"),
                doc.SelectSingleNode("vod/vchip-rating-id"),
                doc.SelectSingleNode("vod/mex-rating-id")
            };

            XmlNode vodNode = doc.SelectSingleNode("vod");
            foreach (var n in nodes)
            {
                if(n.InnerText == "")
                    vodNode.RemoveChild(n);
            }
            
            return doc;
        }

        private string GetTitle(ContentData contentData)
        {
            LanguageInfo language = contentData.LanguageInfos[0];
            return language.Title;
        }

        public Category BuildCategory(XmlNode categoryNode)
        {
            Category category = new Category();
            category.ID = int.Parse(categoryNode.SelectSingleNode("id").InnerText);
            if (categoryNode.SelectSingleNode("parent-id") != null && !String.IsNullOrEmpty(categoryNode.SelectSingleNode("parent-id").InnerText))
                category.ParentID = int.Parse(categoryNode.SelectSingleNode("parent-id").InnerText);

            category.CoverID = categoryNode.SelectSingleNode("cover-id").InnerText;
            category.Description = XmlUtil.UnescapeXML(categoryNode.SelectSingleNode("description").InnerText);
            if (categoryNode.SelectSingleNode("category-tree-id") != null && !String.IsNullOrEmpty(categoryNode.SelectSingleNode("category-tree-id").InnerText))
                category.TreeID = int.Parse(categoryNode.SelectSingleNode("category-tree-id").InnerText);
            if (categoryNode.SelectSingleNode("title-icon-id") != null && !String.IsNullOrEmpty(categoryNode.SelectSingleNode("title-icon-id").InnerText))
                category.IconID = int.Parse(categoryNode.SelectSingleNode("title-icon-id").InnerText);
            return category;
        }

        public Category BuildCategory(string xml)
        {
            Category category = new Category();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode categoryNode = doc.SelectSingleNode("category");
            XmlNode valueNode = categoryNode.SelectSingleNode("name");
            category.Name = XmlUtil.UnescapeXML(valueNode.InnerText);

            valueNode = categoryNode.SelectSingleNode("id");
            if (valueNode != null && !String.IsNullOrEmpty(valueNode.InnerText))
                category.ID = int.Parse(valueNode.InnerText);

            valueNode = categoryNode.SelectSingleNode("category-tree-id");
            if (valueNode != null && !String.IsNullOrEmpty(valueNode.InnerText))
                category.TreeID = int.Parse(valueNode.InnerText);

            valueNode = categoryNode.SelectSingleNode("cover-id");
            if (valueNode != null && !String.IsNullOrEmpty(valueNode.InnerText))
                category.CoverID = valueNode.InnerText;

            valueNode = categoryNode.SelectSingleNode("description");
            if (valueNode != null && !String.IsNullOrEmpty(valueNode.InnerText))
                category.Description = XmlUtil.UnescapeXML(valueNode.InnerText);

            valueNode = categoryNode.SelectSingleNode("thumbnail-id");
            if (valueNode != null && !String.IsNullOrEmpty(valueNode.InnerText))
                category.ThumbnailID = int.Parse(valueNode.InnerText);


            valueNode = categoryNode.SelectSingleNode("title-icon-id");
            if (valueNode != null && !String.IsNullOrEmpty(valueNode.InnerText))
                category.IconID = int.Parse(valueNode.InnerText);

            valueNode = categoryNode.SelectSingleNode("parent-id");
            if (valueNode != null && !String.IsNullOrEmpty(valueNode.InnerText))
                category.ParentID = int.Parse(valueNode.InnerText);
            return category;
        }

        public static String TranslateToCategoryXMLString(String name, int parentNode, int coverID, String description, int categoryTreeID)
        {
            String ret = "<category>";
            ret += "<service-id>" + categoryTreeID.ToString() + "</service-id>";
            ret += "<cover-id>" + coverID.ToString() + "</cover-id>";
            ret += "<name>" + SecurityElement.Escape(name) + "</name>";
            ret += "<description>" + SecurityElement.Escape(description) + "</description>";
            if (parentNode != 0)
                ret += "<parent-id>" + parentNode.ToString() + "</parent-id>";
            ret += "</category>";
            return ret;
        }

        private void HandleAssets(XmlDocument doc, ContentData content, ulong serviceObjectID, bool updateCall)
        {
            //Asset source = content.Assets.First<Asset>(a => !a.IsTrailer && a.Properties.FirstOrDefault<Property>(p => p.Type.Equals("DeviceType") && p.Value.Equals(deviceType)) != null);
            XmlNode sourceNode = doc.SelectSingleNode("vod/contents-attributes");

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "CubiTV").SingleOrDefault();
            var serviceConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == serviceObjectID);
            if (serviceConfig == null)
            {
                log.Warn("Can't find configuration for service with objectID " + serviceObjectID.ToString());
                throw new Exception("Can't find configuration for service with objectID " + serviceObjectID.ToString());
            }
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

            String fileAreaRoot = managerConfig.GetConfigParam("FileAreaRoot");
            String trailerFileAreaRoot = managerConfig.GetConfigParam("FileAreaRoot");
           
            if (managerConfig.ConfigParams.ContainsKey("FileAreaTrailerRoot") && !String.IsNullOrEmpty(managerConfig.GetConfigParam("FileAreaTrailerRoot")))
                trailerFileAreaRoot = managerConfig.GetConfigParam("FileAreaTrailerRoot");
            
            if (!fileAreaRoot.EndsWith(@"\"))
                fileAreaRoot += @"\";
            if (!trailerFileAreaRoot.EndsWith(@"\"))
                trailerFileAreaRoot += @"\";

            String drmName = serviceConfig.GetConfigParam("DrmName");
            String cdnId = "";
            if (serviceConfig.ConfigParams.ContainsKey("CDNID"))
                cdnId = serviceConfig.GetConfigParam("CDNID");
            String httpRoot = serviceConfig.GetConfigParam("MovieHttpRoot");

            String trailerHttpRoot = httpRoot;

            if (serviceConfig.ConfigParams.ContainsKey("TrailerHttpRoot") &&
                !String.IsNullOrEmpty(serviceConfig.GetConfigParam("TrailerHttpRoot")))
                trailerHttpRoot = serviceConfig.GetConfigParam("TrailerHttpRoot");

            Dictionary<String, String> allProfilesInSystem = null;

            if (updateCall)
            {
                allProfilesInSystem = wrapper.GetAllProfiles();
                log.Debug("UpdateCall, fetched " + allProfilesInSystem.Count().ToString() + " profiles");
            }

            List<String> usedProfileIds = new List<string>();
            Dictionary<Asset, List<String>> assets = GetAllDeviceAssets(content, false);
            //  Dictionary<Asset, List<String>> trailerAssets = GetAllDeviceAssets(content, true);

            foreach (Asset asset in assets.Keys)
            {
                foreach (String deviceType in assets[asset])
                {
                    XmlNode contentNode = doc.CreateElement("content");
                    XmlNode dataNode = doc.CreateElement("profile-id");
                    //Property property = asset.Properties.SingleOrDefault<Property>(p => p.Type.Equals("DeviceType"));
                    String profileID = wrapper.GetProfileID(deviceType);
                    dataNode.InnerText = profileID; //GetProfileID(property.Value);
                    log.Debug("added " + deviceType + " to used profiles");
                    usedProfileIds.Add(deviceType.ToLower());
                    contentNode.AppendChild(dataNode);

                    if (!String.IsNullOrWhiteSpace(cdnId)) {
                        dataNode = doc.CreateElement("cdn-id");
                        dataNode.InnerText = cdnId;
                        contentNode.AppendChild(dataNode);
                    }

                    dataNode = doc.CreateElement("drm-type"); //doc.SelectSingleNode("contents-attributes/content/drm-type");
                    dataNode.InnerText = drmName;
                    contentNode.AppendChild(dataNode);

                    dataNode = doc.CreateElement("conax-contego-content-data-attributes"); //doc.SelectSingleNode("contents-attributes/content/drm-type");
                    XmlNode contentIDNode = doc.CreateElement("conax-content-id");
                    contentIDNode.InnerText = ConaxIntegrationHelper.GetConaxContegoContentID(content);
                    dataNode.AppendChild(contentIDNode);
                    contentNode.AppendChild(dataNode);

                    //dataNode = doc.SelectSingleNode("contents-attributes/content/drm-type/drm-attributes/conax-content-id");
                    // dataNode.InnerText = ConaxIntegrationHelper.GetConaxContegoContentID(content);


                    dataNode = doc.CreateElement("source");
                    log.Debug("Url before replace = " + asset.Name);
                    String url = asset.Name.Replace(fileAreaRoot, "");
                    log.Debug("url after replace = " + url);
                    url = url.Replace(@"\", "/");
                    url = httpRoot + url;
                    url = url.Replace(" ", "%20");
                    log.Debug("http url = " + url);
                    dataNode.InnerText = url;

                    contentNode.AppendChild(dataNode);

                    Asset trailer = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer && a.Properties.FirstOrDefault<Property>(p => p.Type.ToLower().Equals("devicetype") && p.Value.Equals(deviceType)) != null);
                    if (trailer != null)
                    {
                        dataNode = doc.CreateElement("trailer");
                        String trailerUrl = trailer.Name.Replace(trailerFileAreaRoot, "");
                        trailerUrl = trailerUrl.Replace(@"\", "/");
                        trailerUrl = trailerHttpRoot + trailerUrl;
                        trailerUrl = trailerUrl.Replace(" ", "%20");
                        dataNode.InnerText = trailerUrl;
                        contentNode.AppendChild(dataNode);
                    }

                    XmlElement highDefinitionNode = doc.CreateElement("high-definition");
                    highDefinitionNode.SetAttribute("type", "boolean");
                    highDefinitionNode.InnerText = "false";
                    contentNode.AppendChild(highDefinitionNode);
                    // fix with property
                    sourceNode.AppendChild(contentNode);
                }

            }
            List<String> missingProfileIds = new List<string>();
            if (updateCall)
            {
                foreach (String key in allProfilesInSystem.Keys)
                {
                    log.Debug("Checking if key " + key + " with value " + allProfilesInSystem[key] + " was used");
                    if (!usedProfileIds.Contains(key.ToLower()))
                    {
                        log.Debug("didnt use " + key + ", adding delete node");
                        XmlNode contentNode = doc.CreateElement("content");
                        XmlNode dataNode = doc.CreateElement("profile-id");
                        XmlNode destroyNode = doc.CreateElement("_destroy");
                        
                        dataNode.InnerText = allProfilesInSystem[key];
                        destroyNode.InnerText = "true";
                        contentNode.AppendChild(dataNode);
                        contentNode.AppendChild(destroyNode);
                        log.Debug("Profile with name " + key + " wasn't used");
                        sourceNode.AppendChild(contentNode);
                    }
                }

            }
        }

        private Dictionary<Asset, List<String>> GetAllDeviceAssets(ContentData content, bool getTrailers)
        {
            // List<String> devices = new List<string>();
            Dictionary<Asset, List<String>> assetByDeviceType = new Dictionary<Asset, List<String>>();
            foreach (Asset asset in content.Assets.Where<Asset>(a => a.IsTrailer == getTrailers))
            {
                foreach (Property p in asset.Properties.Where<Property>(p => p.Type.ToLower().Equals("devicetype")))
                {
                    if (assetByDeviceType.ContainsKey(asset))
                    {
                        assetByDeviceType[asset].Add(p.Value);
                    }
                    else
                    {
                        List<String> devices = new List<string>();
                        devices.Add(p.Value);
                        assetByDeviceType.Add(asset, devices);
                    }
                }
            }
            return assetByDeviceType;
        }

        public ValueObjects.ContentData TranslateXmlToContentData(XmlDocument contentXml)
        {
            ContentData content = new ContentData();

            try
            {
                XmlNode nameNode = contentXml.SelectSingleNode("vod/name");
                content.Name = XmlUtil.UnescapeXML(nameNode.InnerText);


                // Asset asset = FindAsset(contentData.Assets, false);
                XmlNode sourceNode = contentXml.SelectSingleNode("vod/contents/content");
                AddAssetsToContentData(content, sourceNode);

                XmlNode coverNode = contentXml.SelectSingleNode("vod/cover");
                //coverNode.InnerXml = "<id type=\"integer\">2767</id><image-file-name>IMG_2218.JPG</image-file-name><image-file-size type=\"integer\">1335892</image-file-size><image-content-type>image/jpeg</image-content-type>";
                //handle images somehow

                XmlNode castNode = contentXml.SelectSingleNode("vod/cast");
                //handle casts with properties

                XmlNode directorNode = contentXml.SelectSingleNode("vod/director");
                //Handle director with properties

                XmlNode screenPlayNode = contentXml.SelectSingleNode("vod/screenPlay");
                //Handle screenPlay with properties

                XmlNode runtimeNode = contentXml.SelectSingleNode("vod/runtime");
                int runtime;
                if (Int32.TryParse(runtimeNode.InnerText, out runtime))
                {
                    content.RunningTime = new TimeSpan(0, runtime, 0);
                }

                LanguageInfo languageInfo = new LanguageInfo();
                XmlNode descriptionNode = contentXml.SelectSingleNode("vod/description");
                if (languageInfo != null)
                {
                    languageInfo.ShortDescription = descriptionNode.InnerText;
                }

                XmlNode extendedDescriptionNode = contentXml.SelectSingleNode("vod/extended-description");
                if (extendedDescriptionNode != null)
                {
                    languageInfo.LongDescription = extendedDescriptionNode.InnerText;
                }
                content.LanguageInfos.Add(languageInfo);

                //XmlNode highDefinitionNode = contentXml.SelectSingleNode("vod/high-definition");
                // fix with property

                XmlNode releaseDateNode = contentXml.SelectSingleNode("vod/release-date");
                if (releaseDateNode != null && !String.IsNullOrEmpty(releaseDateNode.InnerText))
                {
                    content.EventPeriodFrom = DateTime.Parse(releaseDateNode.InnerText);
                }

                XmlNode countryNode = contentXml.SelectSingleNode("vod/country");
                //releaseDateNode.InnerText = contentData.PublishInfos[0].;


                XmlNode ratingNode = contentXml.SelectSingleNode("vod/mpaa-rating-id");
                // fix with properties

                ratingNode = contentXml.SelectSingleNode("vod/vchip-rating-id");
                // fix with properties

                XmlNode contentRatingsNode = contentXml.SelectSingleNode("vod/content-rating-ids");
                // take care of ratings?

                XmlNode categorysNode = contentXml.SelectSingleNode("vod/category-ids");
                // fix with properties

                XmlNode genresNode = contentXml.SelectSingleNode("vod/genre-ids");
            }
            catch (Exception e)
            {
                log.Error("Error when translating content xml", e);
                throw;
            }
            return content;
        }

        #endregion

        private void AddAssetsToContentData(ContentData content, XmlNode sourceNode)
        {
            XmlNode dataNode = sourceNode.SelectSingleNode("trailer");
            if (dataNode != null)
            {
                Asset trailer = new Asset();
                trailer.Name = XmlUtil.UnescapeXML(dataNode.InnerText.Replace("<server>/", ""));
                dataNode = sourceNode.SelectSingleNode("profile-id");

                trailer.IsTrailer = true;
                trailer.LanguageISO = "";
                trailer.DeliveryMethod = Enums.DeliveryMethod.Stream;

                content.Assets.Add(trailer);
            }

            dataNode = sourceNode.SelectSingleNode("source");
            if (dataNode != null)
            {
                Asset source = new Asset();
                source.Name = XmlUtil.UnescapeXML(dataNode.InnerText.Replace("<server>/", ""));
                dataNode = sourceNode.SelectSingleNode("profile-id");
                // dataNode.InnerText = ""; // what to set here?

                dataNode = sourceNode.SelectSingleNode("trailer");
                source.Name = XmlUtil.UnescapeXML(dataNode.InnerText.Replace("<server>/", ""));
                source.IsTrailer = false;
                source.LanguageISO = "";
                source.DeliveryMethod = Enums.DeliveryMethod.Stream;
                content.Assets.Add(source);
            }
        }


        #region IXmlTranslate Members

        public XmlDocument TranslatePriceDataToXml(MultipleServicePrice priceData)
        {
            return TranslatePriceDataToXml(priceData, null);
        }

        public XmlDocument TranslatePriceDataToXml(MultipleServicePrice priceData, ContentData content)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(PriceXmlTemplateTo);
                XmlNode nameNode = doc.SelectSingleNode("rental-offer/name");
                String priceName = priceData.Title;
                if (priceName.Length > 64)
                    priceName = priceName.Substring(0, 64);
                nameNode.InnerText = priceName;

                XmlNode amountNode = doc.SelectSingleNode("rental-offer/price");
                String priceString = priceData.Price.ToString();
                priceString = priceString.Replace(',', '.');
                amountNode.InnerText = priceString;

                //var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "CubiTV").SingleOrDefault();
                //String coverID = systemConfig.GetConfigParam("PriceCoverID");

                XmlNode rentalPeriodNode = doc.SelectSingleNode("rental-offer/rental-period");

                String rentalPeriodString = "";
                long periodLength = priceData.ContentLicensePeriodLength;
                if (periodLength == 0)
                    periodLength = 1;
                switch (priceData.ContentLicensePeriodLengthTime)
                {
                    case Enums.LicensePeriodUnit.Months:
                        long periods = periodLength;
                        if (periods > 3)
                            periods = 3;
                        periods = 30 * periods;
                        rentalPeriodString = periods.ToString() + ":00:00";
                        break;
                    case Enums.LicensePeriodUnit.Weeks:
                        long weekPeriods = periodLength;
                        if (weekPeriods > 14)
                            weekPeriods = 14;
                        periods = 7 * weekPeriods;
                        rentalPeriodString = String.Format("{0:00}", periods) + ":00:00";
                        break;
                    case Enums.LicensePeriodUnit.Days:
                        if (periodLength > 99)
                            periodLength = 99;
                        rentalPeriodString = String.Format("{0:00}", periodLength) + ":00:00";
                        break;
                    case Enums.LicensePeriodUnit.Hours:
                        long days = 0;
                        long hours = periodLength;
                        if (periodLength > 24)
                        {
                            days = periodLength / 24;
                            hours = periodLength % 24;
                        }
                        rentalPeriodString = String.Format("{0:00}", days) + ":" + String.Format("{0:00}", hours) + ":00";
                        break;

                }


                rentalPeriodNode.InnerText = rentalPeriodString;

                if (content != null && content.EventPeriodFrom.HasValue)
                {
                    XmlNode availableFromNode = doc.SelectSingleNode("rental-offer/available-from");
                    if (availableFromNode == null)
                    {
                        availableFromNode = doc.CreateElement("available-from");
                        availableFromNode.InnerText = content.EventPeriodFrom.Value.ToString("yyyy-MM-dd HH:mm:ss");
                        doc.SelectSingleNode("rental-offer").AppendChild(availableFromNode);
                    }
                }

                if (content != null && content.EventPeriodTo.HasValue)
                {
                    XmlNode availableToNode = doc.SelectSingleNode("rental-offer/available-till");
                    if (availableToNode == null)
                    {
                        availableToNode = doc.CreateElement("available-till");
                        availableToNode.InnerText = content.EventPeriodTo.Value.ToString("yyyy-MM-dd HH:mm:ss");
                        doc.SelectSingleNode("rental-offer").AppendChild(availableToNode);
                    }
                }

            }
            catch (Exception e)
            {
                log.Error("Error when translating price xml", e);
                throw;
            }
            return doc;
        }


        public XmlDocument TranslateSubscriptionPriceDataToXml(MultipleServicePrice priceData)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(SubscriptionPriceTemplate);
                XmlNode nameNode = doc.SelectSingleNode("package-offer/name");
                String priceName = priceData.Title;
                if (priceName.Length > 64)
                    priceName = priceName.Substring(0, 64);
                nameNode.InnerText = priceName;

                XmlNode amountNode = doc.SelectSingleNode("package-offer/price");
                String priceString = priceData.Price.ToString();
                priceString = "0.0";
                amountNode.InnerText = priceString;


                XmlNode coverNode = doc.SelectSingleNode("package-offer/cover-id");
                
                String coverID = ConaxIntegrationHelper.GetCubiTVPriceCoverID(priceData);
                if (!String.IsNullOrEmpty(coverID))
                {
                    coverNode.InnerText = coverID;
                }
                else
                {
                    doc.SelectSingleNode("package-offer").RemoveChild(coverNode);
                }

                //XmlNode rentalPeriodNode = doc.SelectSingleNode("package-offer/rental-period");

                //String rentalPeriodString = "";
                //long periodLength = priceData.ContentLicensePeriodLength;
                //switch (priceData.ContentLicensePeriodLengthTime)
                //{
                //    case Enums.LicensePeriodUnit.Months:
                //        long periods = periodLength;
                //        if (periods > 3)
                //            periods = 3;
                //        periods = 30 * periods;
                //        rentalPeriodString = periods.ToString() + ":00:00";
                //        break;
                //    case Enums.LicensePeriodUnit.Weeks:
                //        long weekPeriods = periodLength;
                //        if (weekPeriods > 14)
                //            periods = 14;
                //        periods = 7 * weekPeriods;
                //        rentalPeriodString = weekPeriods.ToString() + ":00:00";
                //        break;
                //    case Enums.LicensePeriodUnit.Days:
                //        rentalPeriodString = periodLength.ToString() + ":00:00";
                //        break;
                //    case Enums.LicensePeriodUnit.Hours:
                //        rentalPeriodString = "00:" + periodLength.ToString() + ":00";
                //        break;

                //}


                //rentalPeriodNode.InnerText = rentalPeriodString;
            }
            catch (Exception e)
            {
                log.Error("Error when translating subscriptionPrice xml", e);
                throw;
            }
            return doc;
        }

        public MultipleServicePrice TranslateXmlToPriceData(XmlDocument priceXml)
        {
            return new MultipleServicePrice();
        }

        private Asset FindAsset(IList<Asset> assets, bool trailer)
        {
            return assets.FirstOrDefault<Asset>(asset => asset.IsTrailer == trailer);
        }

        public XmlDocument CreateCoverXml(Image cover, string data)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(CoverTemplate);
            XmlNode nameNode = doc.SelectSingleNode("cover/image-file-name");
            nameNode.InnerText = cover.URI;

            XmlNode fileSize = doc.SelectSingleNode("cover/image-file-size");
            fileSize.InnerText = data.Length.ToString();

            String imageType = "image/jpeg";
            if (cover.URI.EndsWith(".png"))
            {
                imageType = "image/png";
            }
            XmlNode imageTypeNode = doc.SelectSingleNode("cover/image-content-type");
            imageTypeNode.InnerText = imageType;

            XmlNode imageNode = doc.SelectSingleNode("cover/image");
            imageNode.InnerText = data;
            return doc;

        }

        public XmlDocument TranslateToCatchUpChannelXML(MultipleContentService service, ContentData content)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement ccElement = doc.CreateElement("catchup-channel");
            doc.AppendChild(ccElement);

            var langinfo = content.LanguageInfos.First(l => l.ISO.Equals(service.ServiceViewMatchRules[0].ServiceViewLanugageISO, StringComparison.OrdinalIgnoreCase));

            CreateChildElement(doc, ccElement, "related-channel-id", ConaxIntegrationHelper.GetCubiTVContentID(service.ObjectID.Value, content), null);
            CreateChildElement(doc, ccElement, "name", "Catchup " + langinfo.Title, null);
            CreateChildElement(doc, ccElement, "cover-id", ConaxIntegrationHelper.GetCubiTVContentCoverID(content, service.ObjectID.Value), null);
            var adult = content.Properties.SingleOrDefault<Property>(p => p.Type.ToLower().Equals(VODnLiveContentProperties.IsAdult.ToLower()));
            if (adult != null)
            {
                CreateChildElement(doc, ccElement, "is-adult", adult.Value, "boolean");
            }
            //CreateChildElement(doc, ccElement, "category-id", "", null);

            return doc;
        }

        public XmlDocument TranslateToNPVRChannelXML(MultipleContentService service, ContentData content)
        {
            String serviceId = wrapper.GetServiceId("NPVR");            

            XmlDocument doc = new XmlDocument();
            XmlElement ccElement = doc.CreateElement("npvr-channel");
            doc.AppendChild(ccElement);

            var langinfo = content.LanguageInfos.First(l => l.ISO.Equals(service.ServiceViewMatchRules[0].ServiceViewLanugageISO, StringComparison.OrdinalIgnoreCase));

            CreateChildElement(doc, ccElement, "related-channel-id", ConaxIntegrationHelper.GetCubiTVContentID(service.ObjectID.Value, content), null);
            CreateChildElement(doc, ccElement, "name", "NPVR " + langinfo.Title, null);
            CreateChildElement(doc, ccElement, "channel-id", ConaxIntegrationHelper.GetCubiChannelId(content), null);
            CreateChildElement(doc, ccElement, "lcn", ConaxIntegrationHelper.GetLCN(content, service.ObjectID.Value), "integer");
            CreateChildElement(doc, ccElement, "cover-id", ConaxIntegrationHelper.GetCubiTVContentCoverID(content, service.ObjectID.Value), null);
            //CreateChildElement(doc, ccElement, "category-id", "", null);
            CreateChildElement(doc, ccElement, "service-id", serviceId, null);

            var adult = content.Properties.SingleOrDefault<Property>(p => p.Type.ToLower().Equals(VODnLiveContentProperties.IsAdult.ToLower()));
            if (adult != null)
            {
                CreateChildElement(doc, ccElement, "is-adult", adult.Value, "boolean");
            }

            XmlElement contentAttributesE = doc.CreateElement("contents-attributes");
            contentAttributesE.SetAttribute("type", "array");
            ccElement.AppendChild(contentAttributesE);

            string conaxContentID = ConaxIntegrationHelper.GetConaxContegoContentID(content);

            // get assets for connected servcie
            var allAssets = content.Assets.Where(a => a.LanguageISO.Equals(service.ServiceViewMatchRules[0].ServiceViewLanugageISO, StringComparison.OrdinalIgnoreCase));
            // filter out DVB source, only IP source for NPVR channel
            var filteredAssets = allAssets.Where(a => CommonUtil.GetStreamType(a.Name) == StreamType.IP);

            foreach (Asset asset in filteredAssets)
            {
                var hdProperty = asset.Properties.First(p => p.Type.Equals(VODnLiveContentProperties.HighDefinition, StringComparison.OrdinalIgnoreCase));
                Boolean hd = Boolean.Parse(hdProperty.Value);
                foreach (Property p in asset.Properties.Where(p => p.Type.Equals(VODnLiveContentProperties.DeviceType, StringComparison.OrdinalIgnoreCase)))
                {
                    XmlElement contentE = doc.CreateElement("content");
                    contentAttributesE.AppendChild(contentE);

                    CreateChildElement(doc, contentE, "profile-id", wrapper.GetProfileID(p.Value), null);
                    CreateChildElement(doc, contentE, "source", asset.Name, null);
                    CreateChildElement(doc, contentE, "high-definition", hd.ToString().ToLower(), "boolean");
                    XmlElement conaxDataE = doc.CreateElement("conax-contego-content-data-attributes");
                    contentE.AppendChild(conaxDataE);
                    CreateChildElement(doc, conaxDataE, "conax-content-id", conaxContentID, null);
                }
            }

            return doc;
        }

        public XmlDocument TranslateToChannelXML(ContentData content, MultipleContentService service)
        {
            string serviceId = wrapper.GetServiceId("TV");

            var langinfo = content.LanguageInfos.First(l => l.ISO.Equals(service.ServiceViewMatchRules[0].ServiceViewLanugageISO, StringComparison.OrdinalIgnoreCase));

            XmlDocument doc = new XmlDocument();

            XmlElement channelE = doc.CreateElement("channel");
            doc.AppendChild(channelE);

            CreateChildElement(doc, channelE, "name", langinfo.Title, null);
            CreateChildElement(doc, channelE, "channel-id", ConaxIntegrationHelper.GetCubiChannelId(content), null);
            CreateChildElement(doc, channelE, "lcn", ConaxIntegrationHelper.GetLCN(content, service.ObjectID.Value), "integer");
            CreateChildElement(doc, channelE, "radio-channel", ConaxIntegrationHelper.GetRadioChannel(content).ToString().ToLower(), "boolean");
            CreateChildElement(doc, channelE, "cover-id", ConaxIntegrationHelper.GetCubiTVContentCoverID(content, service.ObjectID.Value), null);
            CreateChildElement(doc, channelE, "service-id", serviceId, null);

            var adult = content.Properties.SingleOrDefault<Property>(p => p.Type.ToLower().Equals(VODnLiveContentProperties.IsAdult.ToLower()));
            if (adult != null)
            {
                CreateChildElement(doc, channelE, "is-adult", adult.Value, "boolean");
            }

            // create genres elements

            //XmlElement genresE = doc.CreateElement("genre-ids");
            //genresE.SetAttribute("type", "array");
            //channelE.AppendChild(genresE);

            //var ContentPropertyConfig = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "ContentProperty").SingleOrDefault();
            //string propertyName = ContentPropertyConfig.GetConfigParam("GenrePropertyName");

            //List<string> values = new List<string>();
            //foreach (Property p in content.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower())))
            //{
            //    values.Add(p.Value.ToLower());
            //}
            //Hashtable genTable = wrapper.GetGenreIDs(values);
            //foreach (String key in genTable.Keys)
            //{
            //    string genreId = genTable[key] as string;
            //    CreateChildElement(doc, genresE, "genre-id", genreId, null);
            //    log.Debug("Set genreID on node to " + genreId);
            //}

            XmlElement contentAttributesE = doc.CreateElement("contents-attributes");
            contentAttributesE.SetAttribute("type", "array");
            channelE.AppendChild(contentAttributesE);

            string conaxContentID = ConaxIntegrationHelper.GetConaxContegoContentID(content);

            var allAssets = content.Assets.Where(a => a.LanguageISO.Equals(service.ServiceViewMatchRules[0].ServiceViewLanugageISO, StringComparison.OrdinalIgnoreCase));
            // filter OUT IP source if DVB exist, prior DVB.
            List<Asset> filteredAssets = new List<Asset>();
            var DVBAssets = allAssets.Where(a => CommonUtil.GetStreamType(a.Name) == StreamType.DVB);
            filteredAssets.AddRange(DVBAssets);
            foreach(Asset asset in allAssets) {
                // add the other device srouces.
                var deviceTypeProperty = asset.Properties.First(p => p.Type.Equals(VODnLiveContentProperties.DeviceType));
                var hitCounts = DVBAssets.Select(a => a.Properties.Count(p => p.Type.Equals(deviceTypeProperty.Type) &&
                                                                              p.Value.Equals(deviceTypeProperty.Value)));
                Int32 hit = hitCounts.Sum(c => c);
                if (hit == 0)
                    filteredAssets.Add(asset);                
            }


            foreach (Asset asset in filteredAssets)
            {
                var hdProperty = asset.Properties.First(p => p.Type.Equals(VODnLiveContentProperties.HighDefinition, StringComparison.OrdinalIgnoreCase));
                Boolean hd = Boolean.Parse(hdProperty.Value);
                foreach (Property p in asset.Properties.Where(p => p.Type.Equals(VODnLiveContentProperties.DeviceType, StringComparison.OrdinalIgnoreCase)))
                {
                    XmlElement contentE = doc.CreateElement("content");
                    contentAttributesE.AppendChild(contentE);

                    CreateChildElement(doc, contentE, "profile-id", wrapper.GetProfileID(p.Value), null);
                    CreateChildElement(doc, contentE, "source", asset.Name, null);
                    CreateChildElement(doc, contentE, "high-definition", hd.ToString().ToLower(), "boolean");
                    XmlElement conaxDataE = doc.CreateElement("conax-contego-content-data-attributes");
                    contentE.AppendChild(conaxDataE);
                    CreateChildElement(doc, conaxDataE, "conax-content-id", conaxContentID, null);
                }
            }

            var uuidProperty = ConaxIntegrationHelper.GetUuidProperty(content, service.ObjectID.Value);
            if (uuidProperty != null)
            {
                XmlElement externalChannelAttributesE = doc.CreateElement("external-channel-attributes");
                channelE.AppendChild(externalChannelAttributesE);

                XmlElement uuidE = doc.CreateElement("uuid");
                uuidE.SetAttribute("type", "string");
                uuidE.InnerText = uuidProperty.Value;
                externalChannelAttributesE.AppendChild(uuidE);
            }

            return doc;
        }

        private void CreateChildElement(XmlDocument doc, XmlElement parentElement, string newElementName, string newElementInnerText, string typeAttributeValue)
        {
            XmlElement e = doc.CreateElement(newElementName);
            if (!string.IsNullOrEmpty(newElementInnerText))
            {
                e.InnerText = newElementInnerText;
            }
            if (!string.IsNullOrEmpty(typeAttributeValue))
            {
                e.SetAttribute("type", typeAttributeValue);
            }

            parentElement.AppendChild(e);
        }


        private String ContentXmlTemplate = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><vod><name></name><cover-id></cover-id><cast></cast><director></director><producer></producer><screenplay></screenplay><runtime></runtime><description></description><extended-description></extended-description><high-definition type=\"boolean\">false</high-definition><year></year><country></country><mpaa-rating-id></mpaa-rating-id><vchip-rating-id></vchip-rating-id><mex-rating-id></mex-rating-id><content-rating-ids type=\"array\"></content-rating-ids><category-ids type=\"array\"></category-ids><genre-ids type=\"array\"></genre-ids><contents-attributes type=\"array\"></contents-attributes></vod>";

        private String ContentXmlTemplateTo = "<vod><name></name><cover><id type=\"integer\"></id><image-file-name></image-file-name><image-file-size type=\"integer\"></image-file-size><image-content-type></image-content-type></cover><cast></cast><director></director><producer></producer><screenplay></screenplay><runtime></runtime><description></description><extended-description></extended-description><high-definition type=\"boolean\"></high-definition><release-date type=\"date\"></release-date><country></country><mpaa-rating-id></mpaa-rating-id><vchip-rating-id></vchip-rating-id><content-rating-ids type=\"array\"><content-rating-id></content-rating-id></content-rating-ids><category-ids type=\"array\"><category-id></category-id></category-ids><genre-ids type=\"array\"><genre-id></genre-id><genre-id></genre-id></genre-ids><contents-attributes type=\"array\"><content><profile-id>64</profile-id><trailer></trailer><source></source><high-definition type=\"boolean\"></high-definition><drm-type></drm-type><conax-contego-content-data-attributes><conax-content-id></conax-id></conax-contego-content-data-attributes></content></contents-attributes></vod>";

        private String PriceXmlTemplate = "<rental-offer><name></name><price></price><rental-period></rental-period><cover></cover><vods type=\"array\"></vods></rental-offer>";

        private String PriceXmlTemplateTo = "<rental-offer><name></name><price></price><rental-period></rental-period><cover-id></cover-id><vod-ids type=\"array\"></vod-ids><channel-ids type=\"array\"></channel-ids><conax-contego-offer-data-attributes><conax-product-id></conax-product-id></conax-contego-offer-data-attributes></rental-offer>";
        //private String PriceXmlTemplateToChannel = "<rental-offer><name></name><price></price><rental-period></rental-period><cover-id></cover-id><channel-ids type=\"array\"></channel-ids><conax-contego-offer-data-attributes><conax-product-id></conax-product-id></conax-contego-offer-data-attributes></rental-offer>";
        //private String PriceXmlTemplateTo = "<rental-offer><name></name><price></price><rental-period></rental-period><cover-id></cover-id><vod-ids type=\"array\"></vod-ids></rental-offer>";

        private String SubscriptionPriceTemplate = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><package-offer><name></name><price></price><cover-id></cover-id><vod-ids type=\"array\"></vod-ids><channel-ids type=\"array\"></channel-ids><catchup-channel-ids type=\"array\"></catchup-channel-ids><npvr-channel-ids type=\"array\"></npvr-channel-ids><service-ids type=\"array\"></service-ids><lineups type=\"array\"></lineups><conax-contego-offer-data-attributes></conax-contego-offer-data-attributes></package-offer>";

        private String CoverTemplate = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><cover><image-file-name></image-file-name><image-file-size type=\"integer\"></image-file-size><image-content-type>image/png</image-content-type><image></image></cover>";

        #endregion
    }
}
