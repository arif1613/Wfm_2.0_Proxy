using System;
using System.Collections.Generic;
using System.Linq;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Text.RegularExpressions;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.MediaInfo;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Carbon;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax
{

    public class ConaxIntegrationHelper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static String ConaxContegoProductIDPattern = @"ConaxContegoProductID=\d+";
        private static String ServiceExtPriceIDPattern = @"ServiceExtPriceID=\d+";
        private static String ConaxContegoContentIDName = "ConaxContegoContentID";
        private static String CoverIDPattern = @"CubiTVCoverID=\d+";

        // Conax contego product ID and CubiTV offer ID will will stored in servicePrice.LongDescription
        // to keep track of the related object on the external systems.

        public static String GetConaxContegoProductID(MultipleServicePrice servicePrice)
        {
            return GetExternalPricetID(servicePrice, ConaxContegoProductIDPattern);
        }

        public static String GetCubiTVOfferID(MultipleServicePrice servicePrice)
        {
            return GetServiceExtPriceID(servicePrice);
        }

        public static String GetServiceExtPriceID(MultipleServicePrice servicePrice)
        {
            String servicePriceID = GetExternalPricetID(servicePrice, ServiceExtPriceIDPattern);
            //if (!String.IsNullOrEmpty(servicePriceID))
            //{
            //    String[] ids = servicePriceID.Split(':');
            //    if (ids.Length > 1)
            //        return ids[1];
            //}
            return servicePriceID;
        }

        private static String GetExternalPricetID(MultipleServicePrice servicePrice, String externalPricetIDPattern)
        {

            Regex rgx = new Regex(externalPricetIDPattern);

            Match match = rgx.Match(servicePrice.LongDescription);
            if (match.Success)
            {
                String[] res = match.Value.Split('=');
                return res[1];
            }

            return "";
        }

        public static void SetConaxContegoProductID(MultipleServicePrice servicePrice, String ConaxContegoProductID)
        {
            SetExternalPriceID(servicePrice, ConaxContegoProductID, ConaxContegoProductIDPattern);
        }

        public static void SetCubiTVOfferID(MultipleServicePrice servicePrice, String CubiTVOfferID)
        {
            SetServiceExtPriceID(servicePrice, CubiTVOfferID);
        }

        public static void SetServiceExtPriceID(MultipleServicePrice servicePrice, String CubiTVOfferID)
        {
            SetExternalPriceID(servicePrice, CubiTVOfferID, ServiceExtPriceIDPattern);
        }

        private static void SetExternalPriceID(MultipleServicePrice servicePrice, String externalPricetID, String externalPricetIDPattern)
        {

            String replaceStr = externalPricetIDPattern.Replace(@"\d+", externalPricetID);

            Regex rgx = new Regex(externalPricetIDPattern);

            Match match = rgx.Match(servicePrice.LongDescription);
            if (match.Success)
            {
                servicePrice.LongDescription = rgx.Replace(servicePrice.LongDescription, replaceStr);
            }
            else
            {
                servicePrice.LongDescription += replaceStr + ",";
            }
        }

        public static String GetConaxContegoContentID(ContentData content)
        {

            var property = content.Properties.Where(p => p.Type == ConaxContegoContentIDName).SingleOrDefault();
            if (property != null)
                return property.Value;

            return "";
        }

        public static String Getcxid(EPGChannel epgChannel)
        {

            var property = epgChannel.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.cxid, StringComparison.OrdinalIgnoreCase));
            if (property != null)
                return property.Value;

            return String.Empty;
        }

        public static String GetCubiTVContentCoverID(ContentData content, ulong serviceObjectID)
        {
            Property externalIDProperty = content.Properties.FirstOrDefault<Property>(p => p.Type.Equals("ServiceExtContentCoverID") &&
                                                                                          p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (externalIDProperty != null)
            {
                String[] ids = externalIDProperty.Value.Split(':');
                if (ids.Length > 1)
                    return ids[1];
            }
            return "";
        }

        public static void SetCubiTVContentCoverID(ulong serviceObjectID, ContentData content, String coverID)
        {
            Property externalIDProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals("ServiceExtContentCoverID") &&
                                                                                           p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (externalIDProperty == null)
            {
                content.Properties.Add(new Property("ServiceExtContentCoverID", serviceObjectID.ToString() + ":" + coverID));
            }
            else
            {
                externalIDProperty.Value = serviceObjectID.ToString() + ":" + coverID;
            }
        }

        public static void SetCubiTVPriceCoverID(MultipleServicePrice servicePrice, String coverID)
        {
            SetCubiTVPriceCoverID(servicePrice, coverID, CoverIDPattern);
        }

        private static void SetCubiTVPriceCoverID(MultipleServicePrice servicePrice, String coverID, String coverIDPattern)
        {

            String replaceStr = coverIDPattern.Replace(@"\d+", coverID);

            Regex rgx = new Regex(coverIDPattern);

            Match match = rgx.Match(servicePrice.LongDescription);
            if (match.Success)
            {
                servicePrice.LongDescription = rgx.Replace(servicePrice.LongDescription, replaceStr);
            }
            else
            {
                servicePrice.LongDescription += replaceStr + ",";
            }
        }

        public static String GetCubiTVPriceCoverID(MultipleServicePrice servicePrice)
        {
            return GetCubiTVPriceCoverID(servicePrice, CoverIDPattern);
        }

        private static String GetCubiTVPriceCoverID(MultipleServicePrice servicePrice, String coverIDPattern)
        {

            Regex rgx = new Regex(coverIDPattern);

            Match match = rgx.Match(servicePrice.LongDescription);
            if (match.Success)
            {
                String[] res = match.Value.Split('=');
                return res[1];
            }

            return "";
        }

        public static void SetConaxContegoContentID(ContentData content, String conaxContegoContentID)
        {

            var property = content.Properties.Where(p => p.Type == ConaxContegoContentIDName).SingleOrDefault();
            if (property == null)
            {
                content.Properties.Add(new Property(ConaxContegoContentIDName, conaxContegoContentID));
            }
            else
            {
                property.Value = conaxContegoContentID;
            }
        }

        public static String GetCubiChannelId(ContentData content)
        {

            Property channelIDProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(CatchupContentProperties.CubiChannelId));
            if (channelIDProperty != null)
                return channelIDProperty.Value;

            return "";
        }

        public static String GetLCN(ContentData content, UInt64 serviceObjectId)
        {

            Property LCNProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(VODnLiveContentProperties.LCN + ":" + serviceObjectId.ToString()));
            if (LCNProperty != null)
                return LCNProperty.Value;

            return "";
        }

        public static Boolean GetRadioChannel(ContentData content) { 
            
            var property = content.Properties.First(p => p.Type.Equals(VODnLiveContentProperties.RadioChannel));
            return Boolean.Parse(property.Value);
        }

        public static Boolean IsCatchUpEnabledChannel(UInt64 serviceObjectID, ContentData content)
        {
            Property prop = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(VODnLiveContentProperties.EnableCatchUp + ":" + serviceObjectID));
            if (prop != null)
                return Boolean.Parse(prop.Value);

            return false;
        }

        public static void SetCubiTVCatchUpId(UInt64 serviceObjectID, ContentData content, String id)
        {

            Property prop = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(VODnLiveContentProperties.CubiCatchUpId) &&
                                                                              p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (prop == null)
            {
                content.Properties.Add(new Property(VODnLiveContentProperties.CubiCatchUpId, serviceObjectID.ToString() + ":" + id));
            }
            else
            {
                prop.Value = serviceObjectID.ToString() + ":" + id;
            }
        }

        public static String GetCubiTVCatchUpId(UInt64 serviceObjectID, ContentData content)
        {

            Property cuProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals("CubiCatchUpId") &&
                                                                                    p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (cuProperty != null)
            {
                String[] ids = cuProperty.Value.Split(':');
                if (ids.Length > 1)
                    return ids[1];
            }

            return "";
        }

        public static Boolean IsNPVREnabledChannel(UInt64 serviceObjectID, ContentData content)
        {
            Property prop = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(VODnLiveContentProperties.EnableNPVR + ":" + serviceObjectID));
            if (prop != null)
                return Boolean.Parse(prop.Value);

            return false;
        }

        public static void SetCubiTVNPVRId(UInt64 serviceObjectID, ContentData content, String id)
        {

            Property prop = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals("CubiNPVRId") &&
                                                                              p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (prop == null)
            {
                content.Properties.Add(new Property("CubiNPVRId", serviceObjectID.ToString() + ":" + id));
            }
            else
            {
                prop.Value = serviceObjectID.ToString() + ":" + id;
            }
        }

        public static String GetCubiTVNPVRId(UInt64 serviceObjectID, ContentData content)
        {

            Property cuProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals("CubiNPVRId") &&
                                                                                    p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (cuProperty != null)
            {
                String[] ids = cuProperty.Value.Split(':');
                if (ids.Length > 1)
                    return ids[1];
            }

            return "";
        }

        public static String GetCubiTVContentID(UInt64 serviceObjectID, ContentData content)
        {

            Property externalIDProperty = content.Properties.FirstOrDefault<Property>(p => p.Type.Equals("ServiceExtContentID") &&
                                                                                           p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (externalIDProperty != null)
            {
                String[] ids = externalIDProperty.Value.Split(':');
                if (ids.Length > 1)
                    return ids[1];
            }
            return "";
        }

        public static void SetCubiTVContentID(UInt64 serviceObjectID, ContentData content, String cubiTVContentID)
        {

            Property externalIDProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(VODnLiveContentProperties.ServiceExtContentID) &&
                                                                                            p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (externalIDProperty == null)
            {
                content.Properties.Add(new Property(VODnLiveContentProperties.ServiceExtContentID, serviceObjectID.ToString() + ":" + cubiTVContentID));
            }
            else
            {
                externalIDProperty.Value = serviceObjectID.ToString() + ":" + cubiTVContentID;
            }
        }

        public static void SetCubiEpgId(UInt64 serviceObjectID, ContentData content, String cubiEpgId)
        {

            Property cubiEpgIdProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(CatchupContentProperties.CubiEpgId) &&
                                                                                            p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (cubiEpgIdProperty == null)
            {
                content.Properties.Add(new Property(CatchupContentProperties.CubiEpgId, serviceObjectID.ToString() + ":" + cubiEpgId));
            }
            else
            {
                cubiEpgIdProperty.Value = serviceObjectID.ToString() + ":" + cubiEpgId;
            }
        }

        public static NPVRRecordingsstState GetServiceNPVRRecordingsstState(UInt64 serviceObjectID, ContentData content)
        {
            Property proeprty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(CatchupContentProperties.ServiceNPVRRecordingsstState) &&
                                                                                            p.Value.StartsWith(serviceObjectID.ToString() + ":"));

            if (proeprty != null)
            {
                return (NPVRRecordingsstState)Enum.Parse(typeof(NPVRRecordingsstState), proeprty.Value.Split(':')[1], true);
            }
            return NPVRRecordingsstState.NotSpecified;
        }

        public static Property SetServiceNPVRRecordingsstState(UInt64 serviceObjectID, ContentData content, NPVRRecordingsstState state)
        {
            Property proeprty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(CatchupContentProperties.ServiceNPVRRecordingsstState) &&
                                                                                                            p.Value.StartsWith(serviceObjectID.ToString() + ":"));
            if (proeprty == null)
            {
                proeprty = new  Property(CatchupContentProperties.ServiceNPVRRecordingsstState, serviceObjectID.ToString() + ":" + state.ToString("G"));
                content.Properties.Add(proeprty);
            }
            else
            {
                proeprty.Value = serviceObjectID.ToString() + ":" + state.ToString("G");
            }
            return proeprty;
        }

        public static String GetCubiEpgId(UInt64 serviceObjectID, ContentData content)
        {

            Property cubiEpgIdProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals(CatchupContentProperties.CubiEpgId) &&
                                                                                            p.Value.StartsWith(serviceObjectID.ToString() + ":"));

            if (cubiEpgIdProperty != null) {
                return cubiEpgIdProperty.Value.Split(':')[1];
            }
            return String.Empty;
        }

        public static string GetMezzanineName(ContentData content, bool trailer)
        {
            Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == trailer);
            return asset.Name;
        }

        public static Resolution GetResolution(Asset asset)
        {
            Resolution resolution = new Resolution();
            Property widthProperty = asset.Properties.FirstOrDefault(a => a.Type.Equals("ResolutionWidth"));
            Property heightProperty = asset.Properties.FirstOrDefault(a => a.Type.Equals("ResolutionHeight"));
            if (widthProperty == null || heightProperty == null)
            {
                //throw new Exception("No width or height resolution was found for asset");
                Console.WriteLine("No height or weight informations found for :" + asset.Name);
                return null;
            }
            else
            {

                resolution.Width = int.Parse(widthProperty.Value);
                resolution.Height = int.Parse(heightProperty.Value);
                return resolution;
            }
        }

        public static void SetResolution(Asset asset, int width, int height)
        {
            Property widthProperty = new Property() { Type = "ResolutionWidth", Value = width.ToString() };
            Property heightProperty = new Property() { Type = "ResolutionHeight", Value = height.ToString() };
            asset.Properties.Add(widthProperty);
            asset.Properties.Add(heightProperty);
        }

        public static List<String> GetAudioTrackLanguages(Asset asset)
        {
            List<String> languages = new List<string>();
            var languageProperties = asset.Properties.Where<Property>(p => p.Type.Equals("AudioTrackLanguage"));
            foreach (Property languageProperty in languageProperties)
            {
                if (!String.IsNullOrEmpty(languageProperty.Value))
                {
                    int languageEndPos = languageProperty.Value.IndexOf(":");
                    if (languageEndPos != -1)
                    {
                        languages.Add(languageProperty.Value.Substring(0, languageEndPos));
                    }
                }
            }
            return languages;
        }

        public static List<String> GetAudioTrackLanguagePids(Asset asset)
        {
            List<String> languages = new List<string>();
            var languageProperties = asset.Properties.Where<Property>(p => p.Type.Equals("AudioTrackLanguage"));
            foreach (Property languageProperty in languageProperties)
            {
                if (!String.IsNullOrEmpty(languageProperty.Value))
                {
                    int languagePidStartPos = languageProperty.Value.IndexOf(":");
                    if (languagePidStartPos != -1)
                    {
                        languages.Add(languageProperty.Value.Substring(languagePidStartPos + 1));
                    }
                }
            }
            return languages;
        }

        public static String GetLanguageForPid(Asset asset, String pid)
        {
            String languageCode = "";
            List<String> languages = GetAudioTrackLanguageWithPids(asset);
            foreach (String languageWithPid in languages)
            {
                String[] lan = languageWithPid.Split(':');
                if (!String.IsNullOrEmpty(lan[1]) && lan[1].Equals(pid))
                {
                    languageCode = lan[0];
                    break;
                }
            }
            return languageCode;
        }

        public static List<String> GetAudioTrackLanguageWithPids(Asset asset)
        {
            List<String> languages = new List<string>();
            var languageProperties = asset.Properties.Where<Property>(p => p.Type.Equals("AudioTrackLanguage"));
            foreach (Property languageProperty in languageProperties)
            {
                if (!String.IsNullOrEmpty(languageProperty.Value))
                {
                    languages.Add(languageProperty.Value);
                }
            }
            return languages;
        }

        public static void AddAudioTrackLanguages(Asset asset, List<AudioInfo> audioInfos)
        {
            foreach (AudioInfo audioInfo in audioInfos)
            {
                String threeLetterISO = CommonUtil.GetThreeLetterISOLanguageName(audioInfo.Language);
                Property audioTrackProperty = new Property() { Type = "AudioTrackLanguage", Value = threeLetterISO + ":" + audioInfo.ID };
                log.Debug("adding audiotrack " + threeLetterISO + ":" + audioInfo.ID);
                asset.Properties.Add(audioTrackProperty);
            }
        }

        public static void AddSubtitleLanguages(Asset asset, List<SubtitleInfo> subtitleInfos)
        {
            String subtitleFormat = "";
            foreach (SubtitleInfo subtitleInfo in subtitleInfos)
            {
                String threeLetterISO = CommonUtil.GetThreeLetterISOLanguageName(subtitleInfo.Language);
                Property subtitleProperty = new Property() { Type = "SubtitleLanguage", Value = threeLetterISO };
                log.Debug("adding subtitle " + subtitleInfo.Language);
                asset.Properties.Add(subtitleProperty);
                if (String.IsNullOrEmpty(subtitleFormat))
                    subtitleFormat = subtitleInfo.Format;
            }
            Property subtitleFormatProperty = new Property() { Type = "SubtitleFormat", Value = subtitleFormat };
            log.Debug("adding subtitle format " + subtitleFormat);
            asset.Properties.Add(subtitleFormatProperty);
        }

        public static String GetSubtitleFormat(Asset asset)
        {
            String ret = "";
            Property subtitleProperty = asset.Properties.FirstOrDefault<Property>(p => p.Type.Equals("SubtitleFormat", StringComparison.OrdinalIgnoreCase));
            if (subtitleProperty != null)
                ret = subtitleProperty.Value;
            return ret;
        }

        public static List<String> GetSubtitleLanguages(Asset asset)
        {
            List<String> ret = new List<string>();
            foreach (Property property in asset.Properties.Where<Property>(p => p.Type.Equals("SubtitleLanguage", StringComparison.OrdinalIgnoreCase)))
                ret.Add(property.Value);
            return ret;
        }

        public static String GetConaxContegoDeviceProfile(ContentData content, XmlDocument conaxContegoDeviceMapXML)
        {
            XmlNodeList deviceProfileNodes = conaxContegoDeviceMapXML.SelectNodes("DeviceProfiles/DeviceProfile");
            List<String> deviceProperties = new List<String>();

            // get all device type form alla assets.
            var assets = content.Assets.Where(a => a.IsTrailer == false);
            foreach (Asset asset in assets)
            {
                var properties = asset.Properties.Where(p => p.Type == "DeviceType");
                if (properties != null)
                {
                    foreach (Property property in properties)
                    {
                        if (!deviceProperties.Contains(property.Value))
                            deviceProperties.Add(property.Value);
                    }
                }
            }

            // find a profile with the same device types
            foreach (XmlElement deviceProfileNode in deviceProfileNodes)
            {
                XmlNodeList deviceNodes = deviceProfileNode.SelectNodes("Device");

                // same number of device types, start matching.
                Boolean match = false;
                if (deviceProperties.Count == deviceNodes.Count)
                {
                    foreach (XmlElement deviceNode in deviceNodes)
                    {
                        match = false;
                        foreach (String devicePropertie in deviceProperties)
                        {
                            if (devicePropertie.Equals(deviceNode.GetAttribute("type")))
                            {
                                match = true;
                                break;
                            }
                        }
                        if (!match)
                            break; // found no match break go to next profile
                    }
                }
                // found profile
                if (match)
                {
                    return deviceProfileNode.GetAttribute("name");
                }
            }

            return "";
        }

        public static ContentType GetContentType(ContentData content)
        {
            var property = content.Properties.SingleOrDefault(c => c.Type.Equals(SystemContentProperties.ContentType));
            if (property != null)
            {
                ContentType ct = (ContentType)Enum.Parse(typeof(ContentType), property.Value, true);
                return ct;
            }
            return ContentType.NotSpecified;
        }

        public static ResolutionType GetResolutionType(Asset asset)
        {
            ResolutionType ret = ResolutionType.NotSpecified;
            var property = asset.Properties.SingleOrDefault(p => p.Type.Equals("ResolutionType"));
            if (property != null)
            {
                try
                {
                    ret = (ResolutionType)Enum.Parse(typeof(ResolutionType), property.Value, true);
                }
                catch (Exception ex)
                {
                    throw new Exception("Enum Parse Error, no matching resolution type", ex);
                }

                return ret;
            }
            else
            {
                throw new Exception("No Resolution type property was found for asset");
            }

        }

        public static void HandlePublishedTo(ContentData content, ulong serviceObjectID)
        {
            Property publishedTo = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals("PublishedToService") && p.Value.Equals(serviceObjectID.ToString()));
            if (publishedTo == null)
            {
                content.Properties.Add(new Property() { Type = "PublishedToService", Value = serviceObjectID.ToString() });
            }
        }

        public static JobState GetCurrentJobState(RequestParameters parameters, bool trailer)
        {
            String basket = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket;
            JobState jobState = null;
            if (!String.IsNullOrEmpty(basket))
            {
                JobStates jobStates;
                log.Debug("basket have value");
                try
                {
                    jobStates = JsonHelper.JsonDeserialize<JobStates>(basket);
                    if (!trailer)
                        jobState = jobStates.State;
                    else
                        jobState = jobStates.TrailerState;
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Something went wrong when deserializing jobstate, Starting encoding from start", exc);
                }
            }
            return jobState;
        }

        public static void SetCurrentJobState(JobState jobState, RequestParameters parameters, bool trailer)
        {
            log.Debug("Setting current jobstate");
            JobStates states = new JobStates();
            String basket = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket;
            try
            {
                if (!String.IsNullOrEmpty(basket))
                {
                    states = JsonHelper.JsonDeserialize<JobStates>(basket);
                }
                if (!trailer)
                    states.State = jobState;
                else
                    states.TrailerState = jobState;
                parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket = JsonHelper.JsonSerializer<JobStates>(states);
               
                
            }
            catch (Exception exc)
            {
                log.Error("Something went wrong when deserializing jobstate, Starting encoding from start", exc);
            }
        }


        public static string CheckForExistingJobID(RequestParameters parameters, bool trailer)
        {
            String basket = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket;
            String ret = "";

            //log.Debug("In check for existingJobID basket = " + basket);
            if (!String.IsNullOrEmpty(basket))
            {
                string[] jobIds = basket.Split(';');
                if (jobIds.Count() == 1)
                {
                    //   log.Debug("jobids = 1");
                    String value = jobIds[0];
                    if (!trailer && !String.IsNullOrEmpty(value)) // trailer is in second id spot
                    {
                        ret = value.Replace("jobID=", "");
                    }
                }
                else if (jobIds.Count() == 2)
                {
                    //    log.Debug("JobIds = 2");
                    if (!trailer)
                    {
                        //     log.Debug("not trailer");
                        String value = jobIds[0];
                        ret = value.Replace("jobID=", "");
                        //  log.Debug("returning " + ret);
                    }
                    else
                    {
                        //   log.Debug("trailer");
                        String value = jobIds[1];
                        //  log.Debug("value= " + value);
                        ret = value.Replace("trailerJobID=", "");
                        //   log.Debug("returning " + ret);
                    }
                }

            }
            return ret;
        }

        //public static void SetResolution(Asset asset, String resolution)
        //{
        //    var property = asset.Properties.SingleOrDefault(c => c.Type.Equals("Resolution"));
        //    if (property != null)
        //    {
        //        property.Value = resolution;
        //    }
        //    else
        //    {
        //        property = new Property("Resolution", resolution);
        //        asset.Properties.Add(property);
        //    }
        //}


        public static String GetConaxContegoMaturityRating(ContentData content)
        {
            return "UNRATED";
        }

        public static String GetURIProfile(ContentData content)
        {
            String uriProfile = null;
            var property = content.Properties.SingleOrDefault(p => p.Type == "URIProfile");
            if (property != null)
            {
                uriProfile = property.Value;
            }
            return uriProfile;
        }

        internal static void AddUuidProperty(ContentData content, String uuid, ulong serviceObjectId)
        {
            content.Properties.Add(new Property(VODnLiveContentProperties.UUID + ":" + serviceObjectId, uuid));
        }

        internal static Property GetUuidProperty(ContentData content, ulong serviceObjectId)
        {
            Property uuidProperty =
                content.Properties.FirstOrDefault(
                    p => p.Type.Equals(VODnLiveContentProperties.UUID + ":" + serviceObjectId));
            return uuidProperty;
        }

        internal static void AddAssetFormatTypeToAsset(Asset asset, AssetFormatType aft)
        {
            Property assetFormatType = new Property() { Type = "AssetFormatType", Value = aft.ToString() };
            asset.Properties.Add(assetFormatType);
        }

        internal static AssetFormatType GetAssetFormatTypeFromAsset(Asset asset)
        {
            Property assetFormatType = asset.Properties.SingleOrDefault<Property>(p => p.Type.Equals("AssetFormatType", StringComparison.OrdinalIgnoreCase));
            if (assetFormatType == null)
                throw new Exception("No assetFormatType was found on asset");
            AssetFormatType ret = AssetFormatType.HTTPLiveStreaming;
            try
            {
                ret = (AssetFormatType)Enum.Parse(typeof(AssetFormatType), assetFormatType.Value);
            }
            catch (Exception ex)
            {
                log.Error("Error parsing to enum from value " + assetFormatType.Value);
            }
            return ret;
        }

        internal static List<AssetFormatType> GetEncodingTypes(ContentData content, bool isTrailer)
        {
            List<AssetFormatType> ret = new List<AssetFormatType>();
            foreach (Asset asset in content.Assets.Where<Asset>(a => a.IsTrailer == isTrailer))
            {
                AssetFormatType assetFormatType = GetAssetFormatTypeFromAsset(asset);
                if (!ret.Contains(assetFormatType))
                    ret.Add(assetFormatType);
            }
            return ret;
        }

        internal static void AddDisplayAspectRatio(Asset asset, string displayAspectRatio)
        {
            Property displayAspectRatioProperty = new Property() { Type = "DisplayAspectRatio", Value = displayAspectRatio };
            log.Debug("adding displayAspectRatio " + displayAspectRatio);
            asset.Properties.Add(displayAspectRatioProperty);
        }

        internal static String GetDisplayAspectRatio(Asset asset)
        {
            String displayAspectRatio = "";
            Property displayAspectRatioProperty = asset.Properties.FirstOrDefault(r => r.Type == "DisplayAspectRatio");
            if (displayAspectRatioProperty != null)
                displayAspectRatio = displayAspectRatioProperty.Value;
            log.Debug("returning displayAspectRatio " + displayAspectRatio);
            return displayAspectRatio;
        }

        internal static void SetProfileUsedProperty(ContentData content, string profileName, bool trailer)
        {
            String propertyName = "ProfileUsedForEncoder";
            if (trailer)
            {
                propertyName = "Trailer" + propertyName;
            }
            List<Property> profileUsedProperty = content.Properties.Where(p => p.Type.Equals(propertyName)).ToList();
            if (!profileUsedProperty.Any())
            {
                //profileUsedProperty = new Property() { Type = propertyName, Value = profileName };
                content.Properties.Add(new Property() {Type = propertyName, Value = profileName});
            }
            else
            {
                profileUsedProperty.FirstOrDefault().Value = profileName;
            }
        }

        internal static Property SetServiceHasRecordingProperty(UInt64 serviceObjectID, ContentData content, Boolean hasRecordings)
        {
            var property = content.Properties.FirstOrDefault<Property>(p => p.Type.Equals(CatchupContentProperties.EPGHasRecordingInService + ":" + serviceObjectID.ToString()));
            if (property == null)
            {
                property = new Property(CatchupContentProperties.EPGHasRecordingInService + ":" + serviceObjectID.ToString(), hasRecordings.ToString());
                content.Properties.Add(property);
            }            
            property.Value = hasRecordings.ToString();
            return property;
        }

        public static Property GetServiceHasRecordingProperty(UInt64 serviceObjectID, ContentData content)
        {
            var property = content.Properties.FirstOrDefault<Property>(p => p.Type.Equals(CatchupContentProperties.EPGHasRecordingInService + ":" + serviceObjectID.ToString()));            
            return property;
        }

        internal static Int32 ServiceHasRecordingPropertyStateCount(ContentData content, Boolean hasRecordings)
        {
            Int32 count = content.Properties.Count(p => p.Type.StartsWith(CatchupContentProperties.EPGHasRecordingInService) &&
                                                        p.Value.Equals(hasRecordings.ToString(), StringComparison.OrdinalIgnoreCase));
            return count;
        }

        internal static void SetDoneGettingRecordingsFromTenantsProperty(ContentData content)
        {
            var property = content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.DoneGettingRecordingsFromTenants, StringComparison.OrdinalIgnoreCase));
            if (property == null)
            {
                content.AddPropertyValue(CatchupContentProperties.DoneGettingRecordingsFromTenants, "true");
            }
        }

        internal static bool HaveDoneGettingRecordingsFromTenantsProperty(ContentData content)
        {
            bool ret = false;
            var property = content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.DoneGettingRecordingsFromTenants, StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                ret = true;
            }
            return ret;
        }

        internal static Property SetReadyToNPVRPurgeProperty(ContentData content, Boolean readyToPurge)
        {
            var property = content.Properties.FirstOrDefault<Property>(p => p.Type.Equals(CatchupContentProperties.ReadyToNPVRPurge, StringComparison.OrdinalIgnoreCase));
            if (property == null)
            {
                property = new Property(CatchupContentProperties.ReadyToNPVRPurge, readyToPurge.ToString());
                content.Properties.Add(property);
            }
            property.Value = readyToPurge.ToString();
            return property;
        }

        public static Property GetReadyToNPVRPurgeProperty(ContentData content)
        {
            var property = content.Properties.FirstOrDefault<Property>(p => p.Type.Equals(CatchupContentProperties.ReadyToNPVRPurge, StringComparison.OrdinalIgnoreCase));
            return property;
        }

        public static Property GetNPVRAssetArchiveState(ContentData content, String serviceServiceViewLanugageISO, DeviceType devcie)
        {
            String propertyName = CatchupContentProperties.NPVRAssetArchiveState + ":" + serviceServiceViewLanugageISO + ":" +
                                  devcie.ToString();

            return content.Properties.FirstOrDefault(p => p.Type.Equals(propertyName));
        }

        public static Property SetNPVRAssetArchiveState(ContentData content, String serviceServiceViewLanugageISO, DeviceType devcie, NPVRAssetArchiveState NPVRAssetState)
        {
            String propertyName = CatchupContentProperties.NPVRAssetArchiveState + ":" + serviceServiceViewLanugageISO + ":" +
                                  devcie.ToString();

            var NPVRAssetStateProerpty = content.Properties.FirstOrDefault(p => p.Type.Equals(propertyName));
            if (NPVRAssetStateProerpty == null) {
                NPVRAssetStateProerpty = new Property(propertyName, "");
                content.Properties.Add(NPVRAssetStateProerpty);
            }
            NPVRAssetStateProerpty.Value = NPVRAssetState.ToString();

            return NPVRAssetStateProerpty;
        }

        public static List<Property> SetALLNPVRAssetArchiveState(ContentData content, NPVRAssetArchiveState NPVRAssetState)
        {
            List<Property> res = new List<Property>();
            var NPVRAssetProperties = content.Properties.Where(p => p.Type.StartsWith(CatchupContentProperties.NPVRAssetArchiveState));
            foreach (Property npvrAssetProperty in NPVRAssetProperties)
            {
                npvrAssetProperty.Value = NPVRAssetState.ToString();
                res.Add(npvrAssetProperty);
            }
            return res;
        }

        public static List<Property> SetNPVRAssetArchiveStateByAssetName(ContentData content, String assetName, NPVRAssetArchiveState NPVRAssetState)
        {
            var sameAssets = content.Assets.Where(a => a.Name.Equals(assetName));
            List<Property> NPVRAssetStateProperties = new List<Property>();
            foreach (Asset sameAsset in sameAssets)
            {                
                DeviceType device = ConaxIntegrationHelper.GetDeviceType(sameAsset);
               // log.Debug("GetNPVRAssetArchiveState for device " + device);
                Property NPVRAssetStateProperty = ConaxIntegrationHelper.GetNPVRAssetArchiveState(content, sameAsset.LanguageISO, device);
                NPVRAssetStateProperty.Value = NPVRAssetState.ToString();
                NPVRAssetStateProperties.Add(NPVRAssetStateProperty);
            }

            return NPVRAssetStateProperties;
        }

        public static List<Property> GetAllNPVRAssetArchiveStateByState(ContentData content, NPVRAssetArchiveState NPVRAssetState)
        {
            var assetSateProperteis = content.Properties.Where(p => p.Type.StartsWith(CatchupContentProperties.NPVRAssetArchiveState) &&
                                                                    p.Value.Equals(NPVRAssetState.ToString()));
            return assetSateProperteis.ToList();
        }

        public static List<Asset> GetAllNPVRAssetByState(ContentData content, NPVRAssetArchiveState NPVRAssetState)
        {
            List<Asset> NPVRAssets = new List<Asset>();
            List<Property> assetSateProperteis = ConaxIntegrationHelper.GetAllNPVRAssetArchiveStateByState(content, NPVRAssetState);

            foreach (Property assetSatePropertei in assetSateProperteis)
            {
                String[] propertyName = assetSatePropertei.Type.Split(':');
                var NPVRAsset = content.Assets.FirstOrDefault(a => a.LanguageISO != null &&
                                                          a.LanguageISO.Equals(propertyName[1]) &&
                                                          a.Properties.Count(p => p.Type.Equals(CatchupContentProperties.DeviceType) &&
                                                                                  p.Value.Equals(propertyName[2])) > 0 &&
                                                          a.Properties.Count(p => p.Type.Equals(CatchupContentProperties.AssetType) &&
                                                                                  p.Value.Equals(AssetType.NPVR.ToString())) > 0);
                if (NPVRAsset != null)
                    NPVRAssets.Add(NPVRAsset);
            }

            return NPVRAssets;
        }

        public static List<Asset> GetAllCatchupAsset(ContentData content)
        {
            var catchupAssets = content.Assets.Where(a => a.Properties.Count(p => p.Type.Equals(CatchupContentProperties.AssetType) &&
                                                                               p.Value.Equals(AssetType.Catchup.ToString())) > 0);

            return catchupAssets.ToList();
        }

        public static List<Asset> GetAllNPVRAsset(ContentData content)
        {            
            var NPVRAssets = content.Assets.Where(a => a.Properties.Count(p => p.Type.Equals(CatchupContentProperties.AssetType) &&
                                                                               p.Value.Equals(AssetType.NPVR.ToString())) > 0);

            return NPVRAssets.ToList();
        }

        public static DeviceType GetDeviceType(Asset asset)
        {
            var deviceProperty = asset.Properties.First(a => a.Type.Equals(CatchupContentProperties.DeviceType));
            DeviceType device = (DeviceType)Enum.Parse(typeof(DeviceType), deviceProperty.Value, true);
            return device;
        }

        public static Property GetNPVRArchiveTimesProperty(ContentData content)
        {
            var NPVRArchiveTimesProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.NPVRArchiveTimes));
            return NPVRArchiveTimesProperty;            
        }

        public static List<DateTime> GetNPVRArchiveTimes(ContentData content)
        {
            List<DateTime> res = new List<DateTime>();
            //var NPVRArchiveTimesProperty = content.Properties.First(p => p.Type.StartsWith(CatchupContentProperties.NPVRArchiveTimes));
            var NPVRArchiveTimesProperty = GetNPVRArchiveTimesProperty(content);
            if (NPVRArchiveTimesProperty == null)
                return res;

            String[] archvieTimes = NPVRArchiveTimesProperty.Value.Split(':');
            if (archvieTimes.Length != 2)
                return res;

            foreach (String time in archvieTimes[1].Split('|'))
            {
                if (!String.IsNullOrWhiteSpace(time))                
                    res.Add(DateTime.ParseExact(time, "yyyyMMddHHmmss", null));                
            }
            return res;
        }

        public static Property AddNPVRArchiveTimes(ContentData content, DateTime dateTime)
        {
            //var NPVRArchiveTimesProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.NPVRArchiveTimes + ":" + content.ID.Value));
            var NPVRArchiveTimesProperty = GetNPVRArchiveTimesProperty(content);
            if (NPVRArchiveTimesProperty == null) {
                NPVRArchiveTimesProperty = new Property(CatchupContentProperties.NPVRArchiveTimes, "");
                content.Properties.Add(NPVRArchiveTimesProperty);
            }

            String[] archvieTimes = NPVRArchiveTimesProperty.Value.Split(':');
            String archvieTime = "";
            if (archvieTimes.Length == 2)
                archvieTime = archvieTimes[1];

            if (!String.IsNullOrWhiteSpace(archvieTime))            
                archvieTime += "|";

            NPVRArchiveTimesProperty.Value = content.ID.Value + ":" + archvieTime + dateTime.ToString("yyyyMMddHHmmss");
            
            return NPVRArchiveTimesProperty;
        }

        public static Boolean GetEnableCatchUp(ContentData content, UInt64 serviceObjectId)
        {
            return Boolean.Parse(content.Properties.Single(p => p.Type.Equals(CatchupContentProperties.EnableCatchUp + ":" + serviceObjectId)).Value);
        }

        public static Boolean GetEnableNPVR(ContentData content, UInt64 serviceObjectId)
        {
            return Boolean.Parse(content.Properties.Single(p => p.Type.Equals(CatchupContentProperties.EnableNPVR + ":" + serviceObjectId)).Value);
        }

        public static bool IsPublishedToService(ulong serviceObjectId, EPGChannel channel)
        {

            return channel.Properties.Exists(
                    p =>
                        p.Type.Equals(VODnLiveContentProperties.PublishedToService) &&
                        p.Value.Equals(serviceObjectId.ToString())); 

        }

        //public static Property RemoveLastNPVRArchiveTime(ContentData content)
        //{
        //    var NPVRArchiveTimesProperty = content.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRArchiveTimes));
        //    if (!String.IsNullOrWhiteSpace(NPVRArchiveTimesProperty.Value))
        //    {                
        //        int pos = NPVRArchiveTimesProperty.Value.LastIndexOf("|");
        //        if (pos != -1)
        //            NPVRArchiveTimesProperty.Value = NPVRArchiveTimesProperty.Value.Substring(0, pos);
        //        else
        //            NPVRArchiveTimesProperty.Value = "";
        //    }

        //    return NPVRArchiveTimesProperty;
        //}

        public static Property RemoveFirstNPVRArchiveTime(ContentData content)
        {
            //var NPVRArchiveTimesProperty = content.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRArchiveTimes));
            var NPVRArchiveTimesProperty = GetNPVRArchiveTimesProperty(content);
            String[] archiveTimes = NPVRArchiveTimesProperty.Value.Split(':');
            if (archiveTimes.Length != 2)
                return NPVRArchiveTimesProperty;

            if (!String.IsNullOrWhiteSpace(archiveTimes[1]))
            {
                int pos = archiveTimes[1].IndexOf("|");
                if (pos != -1)
                    NPVRArchiveTimesProperty.Value = content.ID.Value + ":" + archiveTimes[1].Substring(pos + 1);
                else
                    NPVRArchiveTimesProperty.Value = "";
            }

            return NPVRArchiveTimesProperty;
        }

        internal static Property GetLastAttemptStateInService(ContentData contentData, UInt64 serviceObjectId)
        {
            Property lastAttemptProperty = contentData.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.LastAttemptStateInService + ":" + serviceObjectId.ToString()));
            return lastAttemptProperty;
        }

        internal static Property SetLastAttemptStateInService(ContentData contentData, UInt64 serviceObjectId, LastAttemptState lastAttempt)
        {
            String propertyName = CatchupContentProperties.LastAttemptStateInService + ":" + serviceObjectId.ToString();
            Property lastAttemptProperty = contentData.Properties.SingleOrDefault(p => p.Type.Equals(propertyName));
            if (lastAttemptProperty != null) {
                lastAttemptProperty.Value = lastAttempt.ToString();
            } else {
                lastAttemptProperty = new Property(propertyName, lastAttempt.ToString());
                contentData.Properties.Add(lastAttemptProperty);
            }
           
            return lastAttemptProperty;
        }

        internal static Boolean HasNoFailedAttempts(ContentData contentData)
        {            
            List<Property> successfulTries = contentData.Properties.Where(p => p.Type.StartsWith(CatchupContentProperties.LastAttemptStateInService) &&
                                                                               p.Value.Equals(LastAttemptState.Succeeded.ToString())).ToList();

            // has number of success lastt ries == to number of connected servcies
            return (successfulTries.Count == contentData.PublishInfos.Count);
        }


        internal static Property GetNPVRRecordingsstState(ContentData contentData)
        {
            Property NPVRRecordingsstStateProperty = contentData.Properties.FirstOrDefault(p =>p.Type.Equals(CatchupContentProperties.NPVRRecordingsstState, StringComparison.OrdinalIgnoreCase));

            return NPVRRecordingsstStateProperty;
        }

    }
}
