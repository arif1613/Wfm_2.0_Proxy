using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Xml.Linq;
using System.Xml.XPath;
using log4net;
using System.Reflection;
using System.Security;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using System.Diagnostics;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.EPG
{
    public class XMLTVEPGParser : IEPGParser
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region IEPGParser Members

        private DateTime _executeTime;

        private MovieRatingFormats movieRatingFormat = CommonUtil.GetSystemMovieRatingFormat();
        private TVRatingFormats tvRatingFormat = CommonUtil.GetSystemTVRatingFormat();

        public XMLTVEPGParser()
        {
           
        }

        public void SetExecuteTime(DateTime executeTime)
        {
            _executeTime = executeTime;
        }

        public Dictionary<UInt64, List<EpgContentInfo>> ParseEPGXML(XElement EPGXML, XElement EPGChannelConfigXML, XElement CatchUpFilterConfigXML, TimeZoneInfo feedtimeZone, List<EpgContentInfo> existingEpgs, List<EPGChannel> channels)
        {
            MPPIntegrationServicesWrapper wrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            Dictionary<UInt64, List<EpgContentInfo>> contents = new Dictionary<UInt64, List<EpgContentInfo>>();
            
            ulong expired = 0;
            NPVRHelper nPVRHelper = new NPVRHelper();
            Stopwatch totalTimer = new Stopwatch();
            Stopwatch itemTimer = new Stopwatch();
            Stopwatch checkExpireTimer = new Stopwatch();
            totalTimer.Start();
            List<String> channelMissingList = new List<String>();
            List<String> addedExternalIds = new List<String>();
            
            List<ulong> alreadyLoggedChannels = new List<ulong>();
            foreach (XElement programNode in EPGXML.Elements("programme"))
            {
                try
                {
                    itemTimer.Start();
                   
                    string title = TrimStartAndEnd(programNode.Element("title").Value);
                    string description = "";
                    if (programNode.Element("desc") != null)
                    {
                        description = TrimStartAndEnd(programNode.Element("desc").Value);
                    }
                    // log.Debug("timer1= " + itemTimer.ElapsedMilliseconds.ToString() + "ms");
                    string channel = TrimStartAndEnd(programNode.Attribute("channel").Value);
                    string startString = programNode.Attribute("start").Value;
                    string stopString = programNode.Attribute("stop").Value;

                    var epgChannel = channels.FirstOrDefault(c => c.EPGId.Equals(channel));
                    if (epgChannel == null)
                    {
                        // if not already in the list
                        if (!channelMissingList.Contains(channel)) {
                            channelMissingList.Add(channel);
                            log.Warn("No Channel Id found for Channel " + channel + " in EPGChannelConfigXML.");
                        }
                        // can't create EPG content, no channel id is mapped.
                        //log.Error("EPG Content " + title + " can't be created, No Channel Id found for Channel " + channel + " in EPGChannelConfigXML.");
                        continue;
                    }
                    if (!epgChannel.PublishedInAnyChannel)
                    {
                        if (!alreadyLoggedChannels.Contains(epgChannel.MppContentId))
                        {
                            log.Warn("Channel " + channel + " is not published in any service so ignoring epg");
                            alreadyLoggedChannels.Add(epgChannel.MppContentId);
                        }
                        continue;
                    }

                    DateTime UTCEventStart = GetDateTime(startString, feedtimeZone);
                    DateTime UTCEventEnd = GetDateTime(stopString, feedtimeZone);

                    var programmeId = TrimStartAndEnd(programNode.Attribute("id").Value);

                    if (UTCEventStart == UTCEventEnd)
                    {
                        log.Warn("EPG with programme id: " + programmeId +
                                 " has the same start and end times and will therefore be skipped.");
                        continue;
                    }

                    String externalID = EPGParserHelper.CreateExternalId(epgChannel.MppContentId.ToString(), programmeId, UTCEventStart);
                    if (addedExternalIds.Contains(externalID))
                    {
                        log.Warn("EPG with ExternalId " + externalID + " already existed in feed for channel " + epgChannel.Name + " with id " + epgChannel.MppContentId
                            + ", programId = " + programmeId);
                        continue;
                    }
                 
                    addedExternalIds.Add(externalID);
                    
                    checkExpireTimer.Start();
                    bool isExpired = EPGIngestTask.IsExceedEpgHistoryLimit(UTCEventStart, _executeTime);
                    checkExpireTimer.Stop();
                    checkExpireTimer.Reset();
                    if (isExpired)
                    {
                        expired++;
                        continue;
                    }
                    
                    var epgInfo = new EpgContentInfo {ExistsInMpp = false};
                    // DateTimeOffset startTime;
                    var content = new ContentData
                        {
                            EventPeriodFrom = UTCEventStart,
                            EventPeriodTo = UTCEventEnd,
                            ExternalID = externalID,
                            Name = title
                        };

                    var languageInfo = new LanguageInfo { ISO = "ENG", Title = title };

                    if (!String.IsNullOrEmpty(description))
                    {
                        languageInfo.ShortDescription = TrimStartAndEnd(programNode.Element("desc").Value);
                        languageInfo.LongDescription = TrimStartAndEnd(programNode.Element("desc").Value);
                    }
                    else
                    {
                        languageInfo.ShortDescription = title;
                        languageInfo.LongDescription = title;
                    }
                    content.LanguageInfos.Add(languageInfo);

                    content.Properties.Add(new Property(CatchupContentProperties.Channel, channel));
                    content.Properties.Add(new Property(CatchupContentProperties.EpgId, channel));

                    UInt64 conaxContegoContentID = epgChannel.ConaxContegoContentID;
                    content.Properties.Add(new Property(SystemContentProperties.ConaxContegoContentID, conaxContegoContentID.ToString()));
                    content.Properties.Add(new Property(CatchupContentProperties.CubiChannelId, epgChannel.CubiChannelId));

                    content.Properties.Add(new Property(CatchupContentProperties.EpgIsSynked, "False"));
                    content.Properties.Add(new Property(CatchupContentProperties.NoOfEpgSynkRetries, "0"));

                    CatchupFilterObject filter = EPGParserHelper.GetFilterObject(programNode, CatchUpFilterConfigXML);

                    Boolean enableCatchUp;
                    if (filter != null)
                    {
                        enableCatchUp = filter.catchupEnabled;
                        content.Properties.Add(new Property(CatchupContentProperties.CatchUpHours, filter.availableHours.ToString()));

                        string devices = "";
                        foreach (var item in filter.devices)
                        {
                            devices += (item + ",");
                        }
                        devices = devices.TrimEnd(new char[] { ',' });
                        content.Properties.Add(new Property(CatchupContentProperties.CatchUpDevices, devices));
                    }
                    else
                    {
                        enableCatchUp = EPGParserHelper.EnableCatchUp(epgChannel, channel, EPGChannelConfigXML);
                    }

                    bool enableNPVR = nPVRHelper.NPVRIsEnabledForEvent(epgChannel, programNode, EPGChannelConfigXML, channel);

                    content.Properties.Add(new Property(CatchupContentProperties.EnableCatchUp, enableCatchUp.ToString()));
                    content.Properties.Add(new Property(CatchupContentProperties.EnableNPVR, enableNPVR.ToString()));
                    
                   

                    content.Properties.Add(new Property(CatchupContentProperties.FeedTimezone, feedtimeZone.Id));

                    foreach (XElement episodeInformation in programNode.Elements("episode-num"))
                    {
                        //String type = episodeInformation.Attribute("system").Value;
                        //int splitPosition = episodeInformation.Value.IndexOf(".");
                        //String seriesID = episodeInformation.Value.Substring(2);

                        //String episodeID = episodeInformation.Value.Substring(splitPosition + 1);
                        String eps = episodeInformation.Attribute("system").Value + ":" + episodeInformation.Value;
                        content.Properties.Add(new Property(CatchupContentProperties.EPGIEpisodeInformation, eps));
                    }

                    content = AddRatingsToContent(content, programNode);

                     
                    content.Properties.Add(new Property(CatchupContentProperties.ChannelId, epgChannel.MppContentId.ToString()));
                    epgInfo.ChannelID = epgChannel.MppContentId;
                    content.Properties.Add(new Property(CatchupContentProperties.EpgProgrammeId, programmeId));

                    content.ContentAgreements.AddRange(wrapper.GetAllServicesForAgreementWithName(epgChannel.ContentAgreement));

                    content.ContentRightsOwner = new ContentRightsOwner {Name = epgChannel.ContentRightOwner};

                    string hashString = title + languageInfo.ShortDescription + channel + startString + stopString;
                    foreach (var prop in content.Properties)
                    {
                        hashString += prop.Value;
                    }
                    hashString += epgChannel.GetPublishingHash();
                    epgInfo.MetaDataHash = hashString.GetHashCode().ToString();

                    content.AddPropertyValue(CatchupContentProperties.EpgMetadataHash, epgInfo.MetaDataHash);
                    epgInfo.Content = content;
                    itemTimer.Stop();
                    itemTimer.Reset();

                    if (!contents.ContainsKey(epgInfo.ChannelID))
                        contents.Add(epgInfo.ChannelID, new List<EpgContentInfo>());
                    contents[epgInfo.ChannelID].Add(epgInfo);
                }
                catch (Exception exc)
                {
                    log.Warn("Error processing " + programNode.Element("title").Value + " in feed, continuing with next", exc);
                }
            }
            totalTimer.Stop();
            log.Debug("Finish working with epgfeed, total time " + (totalTimer.ElapsedMilliseconds / 1000).ToString() + "s");
            //log.Debug("Created content, skipped + " + alreadyExisting.ToString() + " already existing and published to Cubiware and skipped " + expired.ToString() + " that had expired eventTo, and used data from " + existsInMpp.ToString() + " content in Mpp");
            return contents;
        }

        private ContentData AddRatingsToContent(ContentData content, XElement programNode)
        {

            foreach (XElement ratingNode in programNode.Elements("rating"))
            {
                String rating = TrimStartAndEnd(ratingNode.Element("value").Value);

                if (ratingNode.Attribute("system").Value.Equals(tvRatingFormat.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    CommonUtil.SetRatingForContent(content, rating, VODnLiveContentProperties.TVRating);
                }
                else if (ratingNode.Attribute("system").Value.Equals(movieRatingFormat.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    CommonUtil.SetRatingForContent(content, rating, VODnLiveContentProperties.MovieRating);
                }
            }

            return content;
        }


        internal String TrimStartAndEnd(String toTrim)
        {
            char[] charsToTrim = { ' ' };
            String ret = toTrim.TrimStart(charsToTrim);
            ret = ret.TrimEnd(charsToTrim);
            return ret;
        }

        internal DateTime GetDateTime(String dateString, TimeZoneInfo feedtimeZone)
        {
            DateTimeOffset startTime;
            String format12 = "yyyyMMddHHmm";
            String format14 = "yyyyMMddHHmmss";
            String format20 = "yyyyMMddHHmmss zzz";

            try
            {
                if (dateString.Length == 12)
                {
                    startTime = DateTimeOffset.ParseExact(dateString, format12, null);
                    return TimeZoneInfo.ConvertTime(startTime.DateTime, feedtimeZone, TimeZoneInfo.Utc);
                }
                else if (dateString.Length == 14)
                {
                    startTime = DateTimeOffset.ParseExact(dateString, format14, null);
                    return TimeZoneInfo.ConvertTime(startTime.DateTime, feedtimeZone, TimeZoneInfo.Utc);                   
                }
                else if (dateString.Length == 20)
                {
                    startTime = DateTimeOffset.ParseExact(dateString, format20, null);
                    return TimeZoneInfo.ConvertTime(startTime.DateTime, feedtimeZone, TimeZoneInfo.Utc);
                }
            }
            catch (Exception excep)
            {
                log.Error("Error parsing date " + dateString + ", supported formats are '" + format12 + "', '" + format14 + "', '" + format20 + "'", excep);
                throw;
            }
            throw new Exception("DateFormat not supported" + dateString);

        }


        #endregion
    }
}
