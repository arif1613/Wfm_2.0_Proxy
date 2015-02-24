using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using System.Xml.Linq;
using System.Xml.XPath;
using log4net;
using System.Reflection;
using System.Security;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.EPG
{
    public class DNAEPGParser : IEPGParser
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        

        #region IEPGParser Members


        public void SetExecuteTime(DateTime executeTime)
        {
            
        }
        public Dictionary<UInt64, List<EpgContentInfo>> ParseEPGXML(XElement EPGXML, XElement EPGChannelConfigXML, XElement CatchUpFilterConfigXML, TimeZoneInfo feedtimeZone, List<EpgContentInfo> existingEpgs, List<EPGChannel> channels)
        {
            Dictionary<UInt64, List<EpgContentInfo>> contents = new Dictionary<UInt64, List<EpgContentInfo>>();
            //List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();
            
            //foreach (XElement eventNode in EPGXML.Elements("EVENT")) {
            //    ContentData content = new ContentData();

            //    CatchupFilterObject filter = EPGParserHelper.GetFilterObject(eventNode, CatchUpFilterConfigXML);

            //    content.Name = eventNode.Element("TITLE").Value;

            //    LanguageInfo languageInfo = new LanguageInfo();

            //    languageInfo.ISO = "ENG";
            //    languageInfo.Title = SecurityElement.Escape(eventNode.Element("TITLE").Value);
            //    if (!String.IsNullOrEmpty(eventNode.Element("DESCRIPTION").Value))
            //    {
            //        languageInfo.ShortDescription = SecurityElement.Escape(eventNode.Element("DESCRIPTION").Value);
            //        languageInfo.LongDescription = SecurityElement.Escape(eventNode.Element("DESCRIPTION").Value);
            //    }
            //    else {
            //        languageInfo.ShortDescription = SecurityElement.Escape(eventNode.Element("TITLE").Value);
            //        languageInfo.LongDescription = SecurityElement.Escape(eventNode.Element("TITLE").Value);
            //    }
            //    content.LanguageInfos.Add(languageInfo);


            //    if (eventNode.Element("CATEGORY") != null && !String.IsNullOrEmpty(eventNode.Element("CATEGORY").Value))
            //        content.Properties.Add(new Property(CatchupContentProperties.Category, eventNode.Element("CATEGORY").Value));

            //    content.Properties.Add(new Property(CatchupContentProperties.Channel, eventNode.Element("CHANNEL").Value));
            //    content.Properties.Add(new Property(CatchupContentProperties.EpgId, eventNode.Element("CHANNEL").Value));

            //    var epgChannel = channels.FirstOrDefault(c => c.EPGId.Equals(eventNode.Element("CHANNEL").Value));
            //    if (epgChannel == null)
            //    {   // can't create EPG content, no channel id is mapped.
            //        log.Error("EPG Content " + eventNode.Element("TITLE").Value + " can't be created, No Channel Id found for Channel " + eventNode.Element("CHANNEL").Value + " in EPGChannelConfigXML.");
            //        continue;
            //    }

            //    //UInt64 conaxContegoContentID = EPGParserHelper.GetConaxContegoContentID(eventNode.Element("CHANNEL").Value, EPGChannelConfigXML);
            //    UInt64 conaxContegoContentID = epgChannel.ConaxContegoContentID;
            //    content.Properties.Add(new Property(CatchupContentProperties.ConaxContegoContentID, conaxContegoContentID.ToString()));
            //    content.Properties.Add(new Property(CatchupContentProperties.CubiChannelId, epgChannel.CubiChannelId));

            //    Boolean enableCatchUp = false;
            //    Boolean enableNPVR = false;
            //    if (filter != null)
            //    {
            //        enableCatchUp = filter.catchupEnabled;
            //        content.Properties.Add(new Property(CatchupContentProperties.CatchUpHours, filter.availableHours.ToString()));

            //        string devices = "";
            //        foreach (var item in filter.devices)
            //        {
            //            devices += (item + ",");
            //        }
            //        devices = devices.TrimEnd(new char[] { ',' });
            //        content.Properties.Add(new Property(CatchupContentProperties.CatchUpDevices, devices));
            //    }
            //    else
            //    {
            //        enableCatchUp = EPGParserHelper.EnableCatchUp(eventNode.Element("CHANNEL").Value, EPGChannelConfigXML);
            //    }
            //    enableNPVR = EPGParserHelper.EnableNPVR(eventNode.Element("CHANNEL").Value, EPGChannelConfigXML);

            //    content.Properties.Add(new Property(CatchupContentProperties.EnableCatchUp, enableCatchUp.ToString()));
            //    content.Properties.Add(new Property(CatchupContentProperties.EnableNPVR, enableNPVR.ToString()));

            //    content.Properties.Add(new Property(CatchupContentProperties.FeedTimezone, feedtimeZone.Id));

            //    DateTime eventDate = DateTime.ParseExact(eventNode.Element("DATE").Value, "d.M.yyyy", null);
            //    DateTime fromTime = DateTime.ParseExact(eventNode.Element("TIME_FROM").Value, "HH:mm:ss", null);

            //    String[] timeTo = eventNode.Element("TIME_TO").Value.Split(':');
            //    //DateTime length = DateTime.ParseExact(eventNode.Element("TIME_TO").Value, "HH:mm:ss", null);
            //    DateTime eventStart = eventDate.AddSeconds(fromTime.TimeOfDay.TotalSeconds);
            //    DateTime eventEnd = eventStart.AddSeconds((new TimeSpan(Int32.Parse(timeTo[0]), 
            //                                                           Int32.Parse(timeTo[1]), 
            //                                                           Int32.Parse(timeTo[2]))).TotalSeconds);
                    

            //    // handle timezone
            //    DateTime UTCEventStart = TimeZoneInfo.ConvertTime(eventStart,
            //                                                      feedtimeZone,
            //                                                      TimeZoneInfo.Utc);

            //    DateTime UTCEventEnd = TimeZoneInfo.ConvertTime(eventEnd,
            //                                                      feedtimeZone,
            //                                                      TimeZoneInfo.Utc);

            //    content.EventPeriodFrom = UTCEventStart;
            //    content.EventPeriodTo = UTCEventEnd;
                
            //    //String externalID = eventNode.Element("CHANNEL").Value + "-" + 
            //    //                    eventNode.Element("TITLE").Value + "-" +
            //    //                    eventNode.Element("DATE").Value + "-" +
            //    //                    eventNode.Element("TIME_FROM").Value;
            //    //String channeld = EPGParserHelper.GetChannelId(eventNode.Element("CHANNEL").Value, EPGChannelConfigXML);
            //    //if (String.IsNullOrEmpty(channeld))
            //    //{   // can't create EPG content, no channel id is mapped.
            //    //    log.Error("EPG Content " + eventNode.Element("TITLE").Value + " can't be created, No Channel Id found for Channel " + eventNode.Element("CHANNEL").Value + " in EPGChannelConfigXML.");
            //    //    continue;
            //    //}
            //    content.Properties.Add(new Property(CatchupContentProperties.ChannelId, epgChannel.Id.ToString()));

            //    content.ExternalID = EPGParserHelper.CreateExternalID(epgChannel.Id.ToString(), content.EventPeriodFrom.Value);

            //    ContentAgreement contentAgreement = new ContentAgreement();
            //    //contentAgreement.Name = EPGParserHelper.GetContentAgreement(eventNode.Element("CHANNEL").Value, EPGChannelConfigXML);
            //    contentAgreement.Name = epgChannel.ContentAgreement;
            //    content.ContentAgreements.Add(contentAgreement);

            //    content.ContentRightsOwner = new ContentRightsOwner();
            //    //content.ContentRightsOwner.Name = EPGParserHelper.GetContentRightsOwner(eventNode.Element("CHANNEL").Value, EPGChannelConfigXML);
            //    content.ContentRightsOwner.Name = epgChannel.ContentRightOwner;

            //    contents.Add(content);
            //}

            return contents;
        }

        #endregion

    }
}
