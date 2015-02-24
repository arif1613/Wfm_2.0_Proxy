using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using System.IO;
using System.Text.RegularExpressions;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.CatchUp;
using System.Xml.Linq;
using System.Xml.XPath;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Unified;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public class IISSmoothCatchupHandler : BaseEncoderCatchupHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override String CreateAssetName(ContentData content, UInt64 serviceObjId, DeviceType deviceType, EPGChannel channel)
        {

            String AssetName = "";

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String EPGChannelConfigXMLUrl = systemConfig.GetConfigParam("EPGChannelConfigXML");
            XElement EPGChannelConfigXMLFile = XElement.Load(EPGChannelConfigXMLUrl);

            String CubiChannelId = content.Properties.FirstOrDefault(p => p.Type.Equals("CubiChannelId", StringComparison.OrdinalIgnoreCase)).Value;
            XElement channelNode = EPGChannelConfigXMLFile.XPathSelectElement("Channels/Channel[@cubiChannelId='" + CubiChannelId + "']");
            String SSManifestGeneratorUrl = channelNode.XPathSelectElement("SS/CompositeWebRoot").Value;
            
            if (!SSManifestGeneratorUrl.EndsWith("/"))
                SSManifestGeneratorUrl += "/";

            AssetName = SSManifestGeneratorUrl + content.ExternalID + ".csm";

            return AssetName;
        }

        public override String GetAssetUrl(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, NPVRRecording recording, EPGChannel epgChannel)
        {
            throw new NotImplementedException();
            //DateTime dtFrom = recording.Start.Value;
            //DateTime dtTo = recording.End.Value;
            //TimeSpan vbegin = UnifiedHelper.GetServerTimeStamp(dtFrom); //start använd handler
            //TimeSpan vend = UnifiedHelper.GetServerTimeStamp(dtTo);

            //String url = epgChannel.SSNPVRWebRoot + content.ID.Value + "/" + content.ExternalID + ".ism/Manifest?";
            //url += "vbegin=" + ((UInt64)vbegin.TotalSeconds).ToString() + "&";
            //url += "vend=" + ((UInt64)vend.TotalSeconds).ToString(); // end
            //return url;
        }

        #region Generate

        public override void GenerateNPVR(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime startTime, DateTime endTime)
        {
            throw new NotFiniteNumberException();
        }

        public override void GenerateManifest(List<String> channelsToProces)
        {
            throw  new NotImplementedException();
            //log.Debug("Start SS Manifest Generatoin.");
            //var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            //String EPGChannelConfigXMLUrl = systemConfig.GetConfigParam("EPGChannelConfigXML");
            //XElement EPGChannelConfigXMLFile = XElement.Load(EPGChannelConfigXMLUrl);
            //// check ´content 
            //List<ContentData> allcontents = new List<ContentData>();
            //List<AvailableDateTime> availableDateTimes = DBManager.Instance.GetSSAvailableDateTime();
            //foreach (AvailableDateTime availableDateTime in availableDateTimes)
            //{
            //    log.Debug("For SS storage: ChannelId " + availableDateTime.ChannelId + " availableDateTimes " + availableDateTime.UTCMaxEndtime.ToString("yyyy-MM-dd HH:mm:ss"));
            //    XElement channelNode = EPGChannelConfigXMLFile.XPathSelectElement("Channels/Channel[@id='" + availableDateTime.ChannelId + "']");
            //    String CROName = channelNode.XPathSelectElement("ContentRightsOwner").Value;
            //    List<ContentData> contents = GetUnprocessedCatchUpContents(availableDateTime, CatchupContentProperties.CatchupSSManifestState, CROName);
            //    log.Debug("Found " + contents.Count + " countent to generate for this channel.");
            //    allcontents.AddRange(contents);
            //}
            //log.Debug("Total " + allcontents.Count + " content to generate SS manifest for.");

            //var workflowConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            //Int32 startTimePendingSec = Int32.Parse(workflowConfig.GetConfigParam("EPGStartTimePendingSec"));
            //Int32 endTimePendingSec = Int32.Parse(workflowConfig.GetConfigParam("EPGEndTimePendingSec"));
            //String EPGChannelConfigXML = workflowConfig.GetConfigParam("EPGChannelConfigXML");
            //XmlDocument epgConfigDoc = new XmlDocument();
            //epgConfigDoc.Load(EPGChannelConfigXML);

            //foreach (ContentData content in allcontents)
            //{
            //    String CubiChannelId = content.Properties.FirstOrDefault(p => p.Type.Equals("CubiChannelId", StringComparison.OrdinalIgnoreCase)).Value;
            //    XmlNode channelNode = epgConfigDoc.SelectSingleNode("EPGChannelConfig/Channels/Channel[@cubiChannelId='" + CubiChannelId + "']");
            //    XmlNode compositeFSRootNode = channelNode.SelectSingleNode("SS/CompositeFSRoot");
            //    XmlNode catchUpWebRootNode = channelNode.SelectSingleNode("SS/CatchUpWebRoot");
            //    try
            //    {
            //        // save old time
            //        DateTime saveOldStartTime = content.EventPeriodFrom.Value;
            //        DateTime saveOldEndTime = content.EventPeriodTo.Value;
            //        // append time
            //        content.EventPeriodFrom = content.EventPeriodFrom.Value.AddSeconds(-1 * startTimePendingSec);
            //        content.EventPeriodTo = content.EventPeriodTo.Value.AddSeconds(endTimePendingSec);


            //        log.Debug("Generate for content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID);
            //        Boolean result = GenerateSSManifest(content, compositeFSRootNode.InnerText, catchUpWebRootNode.InnerText);

            //        // update content
            //        if (result)
            //        {
            //            log.Debug("Update SSManifestState for content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID);
            //            // revert time b4 update
            //            content.EventPeriodFrom = saveOldStartTime;
            //            content.EventPeriodTo = saveOldEndTime;

            //            var property = content.Properties.FirstOrDefault(p => p.Type.Equals("SSManifestState", StringComparison.OrdinalIgnoreCase));
            //            property.Value = ManifestState.Available.ToString("G");
            //            mppWrapper.UpdateContent(content);
            //        }
            //        else
            //        {
            //            log.Debug("Generate of manifests files for content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID + " couldn't complete.");
            //        }


            //    }
            //    catch (Exception ex)
            //    {
            //        log.Error("Failed to generate manifest for content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID, ex);
            //    }
            //}
        }

        private Boolean GenerateSSManifest(ContentData content, String dest, String webRoot)
        {

            String manifestFileName = content.ExternalID + ".csm";
            log.Debug("Generate " + manifestFileName);

            List<SSManifest> ssManifests = DBManager.Instance.GetSSManifests(content);

            //SmoothStreamingMedia
            XmlDocument compManifest = new XmlDocument();
            compManifest.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?><SmoothStreamingMedia MajorVersion=\"2\" MinorVersion=\"0\" Duration=\"0\"/>");

            foreach (SSManifest ssManifest in ssManifests)
            {

                // get chinks
                XmlDocument manifestDoc = new XmlDocument();
                manifestDoc.LoadXml(ssManifest.ManifestData);
                Dictionary<String, List<SSChunk>> manifestChunks = CommonUtil.GetSSChunksFromManifest(manifestDoc, ssManifest.UTCManifestStartTime);

                // find chunks
                Dictionary<String, List<SSChunk>> newmanifestChunks = new Dictionary<String, List<SSChunk>>();
                Int64 clipBegin = 0;
                Int64 clipEnd = Int64.MaxValue;
                foreach (KeyValuePair<String, List<SSChunk>> kvp in manifestChunks)
                {
                    var chunks =
                        from c in kvp.Value
                        where c.UTCStartTime <= content.EventPeriodTo.Value && c.UTCEndTime >= content.EventPeriodFrom.Value
                        orderby c.UTCStartTime
                        select c;

                    List<SSChunk> newChunks = new List<SSChunk>();
                    newChunks.AddRange(chunks);
                    // new liset
                    newmanifestChunks.Add(kvp.Key, newChunks);

                    var startChunk = newChunks.FirstOrDefault();
                    if (clipBegin < startChunk.T)
                        clipBegin = startChunk.T;

                    var endChunk = newChunks.LastOrDefault();
                    if (clipEnd > (endChunk.T + endChunk.D))
                        clipEnd = (endChunk.T + endChunk.D);
                }

                // build clip url
                if (!webRoot.EndsWith("/"))
                    webRoot += "/";
                String manifestFilename = ssManifest.ManifestFileName.Replace("\\", "/").Replace(".ismc", ".ism/Manifest");
                if (manifestFilename.StartsWith("/"))
                    manifestFilename = manifestFilename.Substring(1);
                Uri baseUri = new Uri(webRoot);
                Uri clipUrl = new Uri(baseUri, manifestFilename);

                // build clip section
                XmlElement clipNode = compManifest.CreateElement("Clip");
                clipNode.SetAttribute("Url", clipUrl.ToString());
                clipNode.SetAttribute("ClipBegin", clipBegin.ToString());
                clipNode.SetAttribute("ClipEnd", clipEnd.ToString());

                compManifest.DocumentElement.AppendChild(clipNode);
                BuildClipXML(manifestDoc, clipNode, newmanifestChunks);
            }

            // calc total durration
            XmlNodeList clipNodes = compManifest.SelectNodes("SmoothStreamingMedia/Clip");
            Int64 totDurration = 0;
            foreach (XmlNode clipNode in clipNodes)
            {
                Int64 ClipStart = Int64.Parse(((XmlElement)clipNode).GetAttribute("ClipBegin"));
                Int64 ClipEnd = Int64.Parse(((XmlElement)clipNode).GetAttribute("ClipEnd"));
                totDurration += (ClipEnd - ClipStart);
            }
            XmlNode smoothStreamingMediaNode = compManifest.SelectSingleNode("SmoothStreamingMedia");
            ((XmlElement)smoothStreamingMediaNode).SetAttribute("Duration", totDurration.ToString());

            // save to storage
            String destPath = Path.Combine(dest, manifestFileName);
            log.Debug("Write manifest to " + destPath);

            String destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            StreamWriter file = new StreamWriter(destPath);
            compManifest.Save(file);
            file.Close();

            return true;
        }

        private void BuildClipXML(XmlDocument manifestDoc, XmlElement clipNode, Dictionary<String, List<SSChunk>> newmanifestChunks)
        {

            foreach (XmlNode childNode in manifestDoc.SelectSingleNode("SmoothStreamingMedia").ChildNodes)
            {
                XmlNode newChild = clipNode.OwnerDocument.ImportNode(childNode, true);
                clipNode.AppendChild(newChild);

                // claer old chunks
                foreach (XmlNode chunkNode in newChild.SelectNodes("c"))
                    newChild.RemoveChild(chunkNode);

                // add new chunks
                var chunks = newmanifestChunks.SingleOrDefault(s => s.Key.Equals(((XmlElement)newChild).GetAttribute("Type"), StringComparison.OrdinalIgnoreCase));
                XmlElement cNode = null;
                if (chunks.Value != null)
                {
                    foreach (SSChunk chunk in chunks.Value)
                    {

                        cNode = newChild.OwnerDocument.CreateElement("c");
                        cNode.SetAttribute("t", chunk.T.ToString());
                        newChild.AppendChild(cNode);
                    }
                    if (cNode != null)
                        cNode.SetAttribute("d", chunks.Value.Last().D.ToString());

                    // set count
                    ((XmlElement)newChild).SetAttribute("Chunks", chunks.Value.Count.ToString());
                }
            }
        }
        #endregion

        #region LoadToDB
        public override void ProcessArchive(List<String> channelsToProces)
        {
            log.Debug("ProcessSSArchive start");

            var workflowConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String EPGChannelConfigXML = workflowConfig.GetConfigParam("EPGChannelConfigXML");
            XmlDocument epgConfigDoc = new XmlDocument();
            epgConfigDoc.Load(EPGChannelConfigXML);

            XmlNodeList channelNodes = epgConfigDoc.SelectNodes("EPGChannelConfig/Channels/Channel");
            foreach (XmlElement channelNode in channelNodes)
            {
                String channelId = channelNode.GetAttribute("id");
                log.Debug("Check ChannelId " + channelId);

                XmlNode catchUpFSRootNode = channelNode.SelectSingleNode("SS/CatchUpFSRoot");
                String catchUpFSRoot = catchUpFSRootNode.InnerText;
                XmlElement SSNode = (XmlElement)channelNode.SelectSingleNode("SS");

                TimeZoneInfo sstimeZone;
                try
                {
                    sstimeZone = TimeZoneInfo.FindSystemTimeZoneById(SSNode.GetAttribute("encodeInTimezone"));
                }
                catch (Exception ex)
                {
                    log.Error("Failed to find timeZone with id " + SSNode.GetAttribute("encodeInTimezone"), ex);
                    continue;
                }

                String[] dirs = Directory.GetDirectories(catchUpFSRoot);
                foreach (String dir in dirs)
                {
                    log.Debug("catchups " + dir);
                    String[] segDirs = Directory.GetDirectories(dir);
                    foreach (String segDir in segDirs)
                    {
                        log.Debug("segment " + segDir);
                        String[] files = Directory.GetFiles(segDir);
                        var file = files.FirstOrDefault(f => f.EndsWith(".ismc"));
                        if (file != null)
                        {
                            DateTime? UTCStartTime = GetSSStartTimeInUTC(file, sstimeZone);
                            if (!UTCStartTime.HasValue)
                            {
                                log.Error("failed to get start time from segemnet " + file + " for ChannelId " + channelId);
                                continue;
                            }
                            try
                            {
                                LoadSSManifest(UInt64.Parse(channelId), catchUpFSRoot, file, UTCStartTime.Value);
                            }
                            catch (Exception ex)
                            {
                                log.Error("Failed to load manfiest " + file + " for channelId " + channelId + " to db.", ex);
                            }
                        }
                        else
                        {
                            log.Error("Segment " + segDir + " doesn't have a client manifest file.");
                        }
                    }
                }
            }
            log.Debug("ProcessSSArchive end");
        }

        private void LoadSSManifest(UInt64 channelId, String catchUpFSRoot, String file, DateTime UTCStartTime)
        {
            String manifestFileName = file.Replace(catchUpFSRoot, "");
            Boolean isManifestExist = DBManager.Instance.IsSSManifestExist(channelId, manifestFileName);
            if (isManifestExist)
                return;

            XmlDocument clientDoc = new XmlDocument();
            clientDoc.Load(file);

            DateTime streamStarttime = DateTime.MinValue;
            DateTime streamEndTime = DateTime.MaxValue;
            // load manfiest
            Dictionary<String, List<SSChunk>> manifestChunks = CommonUtil.GetSSChunksFromManifest(clientDoc, UTCStartTime);
            // calc stream start/end time
            Int64 clipBegin = 0;
            Int64 clipEnd = Int64.MaxValue;
            foreach (KeyValuePair<String, List<SSChunk>> kvp in manifestChunks)
            {

                var startChunk = kvp.Value.FirstOrDefault();
                if (clipBegin < startChunk.T)
                    clipBegin = startChunk.T;

                var endChunk = kvp.Value.LastOrDefault();
                if (clipEnd > (endChunk.T + endChunk.D))
                    clipEnd = (endChunk.T + endChunk.D);
            }
            streamStarttime = UTCStartTime.AddTicks(clipBegin);
            streamEndTime = UTCStartTime.AddTicks(clipEnd);

            // write to db
            //Boolean isManifestExist = DBManager.Instance.IsSSManifestExist(cubiChannelId, manifestFileName);
            //if (!isManifestExist) {
            log.Debug("Add channelId:" + channelId + " manifestFileName:" + manifestFileName + " streamStarttime:" + streamStarttime.ToString("yyyy-MM-dd HH:mm:ss") + " streamEndTime:" + streamEndTime.ToString("yyyy-MM-dd HH:mm:ss"));
            SSManifest SSManifest = new SSManifest();
            SSManifest.ChannelId = channelId;
            SSManifest.ManifestFileName = manifestFileName;
            SSManifest.ManifestData = clientDoc.InnerXml;
            SSManifest.UTCManifestStartTime = UTCStartTime;
            SSManifest.UTCStreamStartTime = streamStarttime;
            SSManifest.UTCStreamEndTime = streamEndTime;
            DBManager.Instance.AddSSManifest(SSManifest);
            //}
        }

        private DateTime? GetSSStartTimeInUTC(String fileName, TimeZoneInfo timezone)
        {

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "EnvivioEncoder").SingleOrDefault();
            String FileArchiveRootFolder = systemConfig.GetConfigParam("SSCatchUpFolderDateTimeRegExp");

            Match match = Regex.Match(fileName,
                                      FileArchiveRootFolder,
                                      RegexOptions.IgnoreCase);
            if (match.Success)
            {
                DateTime catchUpStart = DateTime.ParseExact(match.Value, systemConfig.GetConfigParam("SSCatchUpFolderDateTimeFormat"), null);
                DateTime utcTime = TimeZoneInfo.ConvertTime(catchUpStart,
                                                            timezone,
                                                            TimeZoneInfo.Utc);

                return utcTime;
            }
            return null;
        }
        #endregion

        #region Delete
        public override void DeleteCatchupSegments(EPGChannel epgChannel)
        {
            // TODO: IIS handler no longer supported.
            throw new NotImplementedException();
            //List<SSManifest> ssManifests = DBManager.Instance.GetSSManifests(epgChannel.MppContentId, epgChannel.DeleteTimeSSBuffer);
            //log.Debug(ssManifests.Count + " of SS manifest to delete for ChannelId:" + epgChannel.MppContentId + " deletefrom:" + epgChannel.DeleteTimeSSBuffer.ToString("yyyy-MM-dd HH:mm:ss"));
            //EnvivioCatchUpFileHandler catchUpFileHandler = new EnvivioCatchUpFileHandler();
            //catchUpFileHandler.DeleteSSCatchUpFiles(ssManifests, epgChannel.SSCatchUpFSRoot);
            //// remove from db                
            //try
            //{
            //    DBManager.Instance.DeleteSSManifest(epgChannel.MppContentId, epgChannel.DeleteTimeSSBuffer);
            //}
            //catch (Exception ex)
            //{
            //    log.Error("Failed to delete ss manfiest data in DB", ex);
            //}
        }

        public override void DeleteNPVR(ContentData content, Asset assetToDelete)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
