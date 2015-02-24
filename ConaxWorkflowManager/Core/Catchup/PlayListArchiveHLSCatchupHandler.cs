using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.CatchUp;
using System.Threading;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using System.Diagnostics;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public abstract class PlayListArchiveHLSCatchupHandler : BaseEncoderCatchupHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected String systemName = "Base";
        protected ICatchUpFileHandler catchUpFileHandler;

        public override String CreateAssetName(ContentData content, UInt64 serviceObjId, DeviceType deviceType, EPGChannel channel)
        {
            String AssetName = "NoStream";

            return AssetName;
        }

        #region Generate

        public override void GenerateManifest(List<String> channelsToProces)
        {
            log.Debug("Start Generate HLS manifests.");

            GenerateCatchupManifest(channelsToProces);

            //GenerateNPVRManifest(channelsToProces);
        }

        public override void GenerateNPVR(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime startTime, DateTime endTime)
        {
            throw new NotImplementedException();
            //List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();
            //// check content 
            //List<ContentData> allcontents = new List<ContentData>();
            //allcontents = GetContentToProccess(CatchupContentProperties.NPVRHLSManifestState, channelsToProces);

            //log.Debug("Total " + allcontents.Count + " content to generate HLS NPVR assset for.");
            //foreach (ContentData content in allcontents)
            //{
            //    log.Debug("Proccess content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID);
            //    try
            //    {
            //        Dictionary<UInt64, List<NPVRRecording>> allRecordigns = GetAllRecordingsForContent(content);


            //        // move segements
            //        ArchiveHLSAsset(content, allRecordigns);

            //        // generate playlist
            //        log.Debug("Generate for content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID);
            //        GenerateRecordingManifest(content, allRecordigns);
            //        // update state in mpp.
            //        var NPVRHLSManifestStateProeprty = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.NPVRHLSManifestState, StringComparison.OrdinalIgnoreCase));
            //        NPVRHLSManifestStateProeprty.Value = ManifestState.Available.ToString("G");
            //        mppWrapper.UpdateContent(content);
            //        log.Debug("HLS asset archived for content " + content.Name + " " + content.ExternalID);

            //        // update recordings in cubi
            //        foreach (MultipleContentService servcie in content.ContentAgreements[0].IncludedServices)
            //        {
            //            CubiTVMiddlewareServiceWrapper cubiWrapper = CubiTVMiddlewareManager.Instance(servcie.ObjectID.Value);
            //            // get recordings
            //            List<NPVRRecording> recordings = new List<NPVRRecording>();
            //            if (allRecordigns.TryGetValue(servcie.ObjectID.Value, out recordings) &&
            //                recordings.Count > 0)
            //            {
            //                // has recordings
            //                EPGChannel epgChannel = CatchupHelper.GetEPGChannel(content);
            //                if (!epgChannel.HLSNPVRWebRoot.EndsWith("/"))
            //                    epgChannel.HLSNPVRWebRoot += "/";
            //                foreach (NPVRRecording recording in recordings)
            //                {
            //                    // build hls url for recordings
            //                    String startStr = recording.Start.Value.ToString("yyyyMMddHHmmss");
            //                    String endStr = recording.End.Value.ToString("yyyyMMddHHmmss");

            //                    String url = epgChannel.HLSNPVRWebRoot + content.ObjectID.Value + "/" + startStr + "_" + endStr + "-index.m3u8";                                
            //                    recording.HLSURL = url;

            //                    // update recording in cubi
            //                    cubiWrapper.UpdateNPVRRecording(content, recording);
            //                }
            //            }
            //            // update state in mpp.
            //            content.Properties.Add(new Property(CatchupContentProperties.NPVRServiceUpdatedWithHLS, servcie.ObjectID.Value.ToString()));
            //            mppWrapper.UpdateContent(content);
            //            log.Debug("all recordings with HLS updated in service " + servcie.ObjectID.Value + " for content " + content.Name + " " + content.ExternalID);
            //        }

            //        // all done update state in mpp.
            //        var MPVRAllRecordingsUpdatedWithHLSProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.NPVRAllRecordingsUpdatedWithHLS, StringComparison.OrdinalIgnoreCase));
            //        MPVRAllRecordingsUpdatedWithHLSProperty.Value = true.ToString();
            //        mppWrapper.UpdateContent(content);
            //        log.Debug("all recordings updated with HLS in all connected services for content " + content.Name + " " + content.ExternalID);
                    
            //    }catch(Exception ex) {
            //        log.Error("Failed to Generate vod assets or update recordings for content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID, ex);
            //    }
            //}
        }

        private void ArchiveHLSAsset(ContentData content, Dictionary<UInt64, List<NPVRRecording>> allRecordigns)
        {
            

                //Dictionary<String, List<HLSChunk>> hlsChunks
            //FileSystemHandler filHandler = new FileSystemHandler();

            //String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;
            ////var channel = channels.First(c => c.Id == UInt64.Parse(channelId));

            //// get min start and max end recording time
            //DateTime minStart = DateTime.MaxValue;
            //DateTime maxEnd = DateTime.MinValue;
            //GetMinStartNMaxEnd(content, out minStart, out maxEnd);

            //log.Debug("minStart: " + minStart.ToString("yyyy-MM-dd HH:mm:ss") + " maxEnd:" + maxEnd.ToString());
            //// check if current time is lesser than end time + post guard.
            //if (DateTime.UtcNow < maxEnd)
            //{
            //    log.Debug("DateTime.UtcNow: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " is lesser maxEnd:" + maxEnd.ToString() + ", not ready yet skip archive content " + content.ID + " " + content.ExternalID + " for now.");
            //    continue; // not passing post gaurd yet, try again later.
            //}
            //// check if endtime is changed.
            //if (maxEnd == DateTime.MinValue)
            //{
            //    log.Debug("maxEnd is still minvalue, which means no recordings found, mark no recording state for content " + content.ID + " " + content.ExternalID);
            //    var recordingProepty = content.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRRecordingsstState));
            //    recordingProepty.Value = NPVRRecordingsstState.NoRecordings.ToString("G");
            //    mppWrapper.UpdateContent(content);
            //    continue;
            //}

            //minStart = new DateTime(2013, 02, 13, 15, 21, 00);
            //maxEnd = new DateTime(2013, 02, 13, 15, 25, 00);

            //// get chunks data for NPVR content
            //Dictionary<String, List<HLSChunk>> hlsChunks = GetHLSChunks(UInt64.Parse(channelId), minStart, maxEnd);

            //// copy segments
            //List<EPGChannel> epgChannels = CatchupHelper.GetAllEPGChannelsFromConfigOnly();
            //var channelProperty = content.Properties.First(p => p.Type.Equals(CatchupContentProperties.ChannelId));
            //var epgChannel = epgChannels.First(c => c.Id == UInt64.Parse(channelProperty.Value));

            //String fromDir = epgChannel.HLSCatchUpFSRoot;
            //String toDir = Path.Combine(epgChannel.HLSNPVRFSRoot, content.ObjectID.Value.ToString());

            //log.Debug("Start copy segments from " + fromDir + " to " + toDir + " for content " + content.Name + " " + content.ID + " " + content.ExternalID);
            //foreach(KeyValuePair<String, List<HLSChunk>> kvp in hlsChunks) {
            //    foreach(HLSChunk hlsChunk in kvp.Value) {

            //        String fromPath = Path.Combine(fromDir, hlsChunk.URI).Replace("/", "\\");
            //        String toPath = Path.Combine(toDir, hlsChunk.URI).Replace("/", "\\");                 
            //        try
            //        {
            //            filHandler.CopyTo(fromPath, toPath);
            //        } catch (Exception ex) {
            //            log.Error("Failed to copy segment from " + fromPath + " to " + toPath + " for content " + content.Name + " " + content.ID + " " + content.ExternalID, ex);
            //            throw;
            //        }
            //    }
            //}
            //log.Debug("Done copy segments from " + fromDir + " to " + toDir + " for content " + content.Name + " " + content.ID + " " + content.ExternalID);
        }

        private void GenerateCatchupManifest(List<String> channelsToProces)
        {
            throw new NotImplementedException();
            //List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();

            //// check ´content 
            //List<ContentData> allcontents = new List<ContentData>();
            //allcontents = GetContentToProccess(CatchupContentProperties.CatchupHLSManifestState, channelsToProces);

            //log.Debug("Total " + allcontents.Count + " content to generate HLS manifest for.");

            //var workflowConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            //Int32 startTimePendingSec = Int32.Parse(workflowConfig.GetConfigParam("EPGStartTimePendingSec"));
            //Int32 endTimePendingSec = Int32.Parse(workflowConfig.GetConfigParam("EPGEndTimePendingSec"));
            
            //// generate manifest
            //foreach (ContentData content in allcontents)
            //{
            //    String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;
            //    var channel = channels.First(c => c.MppContentId == UInt64.Parse(channelId));
                 
            //    try
            //    {
            //        // save old time
            //        DateTime saveOldStartTime = content.EventPeriodFrom.Value;
            //        DateTime saveOldEndTime = content.EventPeriodTo.Value;
            //        // append time
            //        content.EventPeriodFrom = content.EventPeriodFrom.Value.AddSeconds(-1 * startTimePendingSec);
            //        content.EventPeriodTo = content.EventPeriodTo.Value.AddSeconds(endTimePendingSec);

            //        log.Debug("Generate for content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID);
            //        Boolean result = GenerateHLSManifest(content, channel.HLSCompositeFSRoot, channel.HLSCatchUpWebRoot);

            //        // update content
            //        if (result)
            //        {
            //            log.Debug("Update HLSManifestState for content " + content.Name + " ID:" + content.ID.Value + " ExtID:" + content.ExternalID);
            //            // revert time b4 update
            //            content.EventPeriodFrom = saveOldStartTime;
            //            content.EventPeriodTo = saveOldEndTime;

            //            var property = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.CatchupHLSManifestState, StringComparison.OrdinalIgnoreCase));
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

        private Dictionary<String, List<HLSChunk>> GetHLSChunks(UInt64 channelId, DateTime UTCStartTime, DateTime UTCEndTime)
        {
            Dictionary<String, List<HLSChunk>> HLSChunks = new Dictionary<String, List<HLSChunk>>();

            List<String> manifestNames = DBManager.Instance.GetHLSManifestNames(channelId, UTCStartTime, UTCEndTime);
            foreach(String manifestName in manifestNames) {

                List<HLSChunk> chunks = DBManager.Instance.GetHLSChunks(channelId, manifestName, UTCStartTime, UTCEndTime);
                HLSChunks.Add(manifestName, chunks);
            }

            return HLSChunks;
        }

        //private Boolean GenerateRecordingManifest(ContentData content, Dictionary<UInt64, List<NPVRRecording>> allRecordings)
        //{
        //    Boolean result = false;

        //    UInt64 channelId = UInt64.Parse(content.Properties.First(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value);
        //    List<EPGChannel> epgChannels = CatchupHelper.GetAllEPGChannelsFromConfigOnly();
        //    var epgChannel = epgChannels.First(c => c.MppContentId == channelId);
        //    String webroot = epgChannel.HLSNPVRWebRoot + "/" + content.ObjectID.Value.ToString();
        //    String dest = Path.Combine(epgChannel.HLSNPVRFSRoot, content.ObjectID.Value.ToString());

        //    // find unique time combo
        //    List<String> uniqueTimeCombo = new List<String>();
        //    foreach(KeyValuePair<UInt64, List<NPVRRecording>> kvp in allRecordings) {
        //        foreach(NPVRRecording recording in kvp.Value) {

        //            recording.Start = new DateTime(2013, 02, 13, 15, 21, 00);
        //            recording.End = new DateTime(2013, 02, 13, 15, 25, 00);

        //            String startStr = recording.Start.Value.ToString("yyyyMMddHHmmss");
        //            String endStr = recording.End.Value.ToString("yyyyMMddHHmmss");

        //            if (!uniqueTimeCombo.Contains(startStr + "_" + endStr))
        //                uniqueTimeCombo.Add(startStr + "_" + endStr);
        //        }
        //    }

        //    // Generate manifest files
        //    foreach(String uniqueTime in uniqueTimeCombo) {
        //        String[] times = uniqueTime.Split('_');
        //        DateTime start = DateTime.ParseExact(times[0], "yyyyMMddHHmmss", null);
        //        DateTime end = DateTime.ParseExact(times[1], "yyyyMMddHHmmss", null);

        //        Dictionary<String, List<HLSChunk>> hlsChunks = GetHLSChunks(channelId, start, end);
        //        foreach(KeyValuePair<String, List<HLSChunk>> kvp in hlsChunks) {

                    
        //            String manifest = BuildHLSManifest(kvp.Value, webroot);
        //            String destPath = Path.Combine(dest, uniqueTime + "-" + kvp.Key);
        //            log.Debug("Write manifest to " + destPath);

        //            String destDir = Path.GetDirectoryName(destPath);
        //            if (!Directory.Exists(destDir))
        //                Directory.CreateDirectory(destDir);

        //            StreamWriter file = new StreamWriter(destPath);
        //            file.Write(manifest);
        //            file.Close();
        //        }


        //        if (hlsChunks.Count > 0) {
        //            String indexFileName = uniqueTime + "-" + "index.m3u8";
        //            log.Debug("Generate " + indexFileName);
        //            List<String> brManifestes = new List<String>();
        //            brManifestes.AddRange(hlsChunks.Keys.ToArray());
        //            String idxManifest = BuildHLSIndexManifest(uniqueTime, channelId, brManifestes);

        //            String idxDestPath = Path.Combine(dest, indexFileName);
        //            log.Debug("Write manifest to " + idxDestPath);
        //            StreamWriter file = new StreamWriter(idxDestPath);
        //            file.Write(idxManifest);
        //            file.Close();
        //        }
        //    }

        //    return result;
        //}

        private Boolean GenerateHLSManifest(ContentData content, String dest, String webRoot)
        {
            List<String> manifestNames = DBManager.Instance.GetHLSManifestNames(content);
            Boolean result = false;

            foreach (String manifestName in manifestNames)
            {
                String manifestFileName = content.ExternalID + "-" + manifestName;
                log.Debug("Generate " + manifestFileName);

                List<HLSChunk> hlsChunks = DBManager.Instance.GetHLSChunks(content, manifestName);

                String manifest = BuildHLSManifest(hlsChunks, webRoot);
                String destPath = Path.Combine(dest, manifestFileName);
                log.Debug("Write manifest to " + destPath);

                String destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                StreamWriter file = new StreamWriter(destPath);
                file.Write(manifest);
                file.Close();

            }

            // generate index manifest
            if (manifestNames.Count > 0)
            {
                String indexFileName = content.ExternalID + "-" + "index.m3u8";
                log.Debug("Generate " + indexFileName);
                String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;
                String idxManifest = BuildHLSIndexManifest(content.ExternalID, UInt64.Parse(channelId), manifestNames);

                String idxDestPath = Path.Combine(dest, indexFileName);
                log.Debug("Write manifest to " + idxDestPath);
                StreamWriter file = new StreamWriter(idxDestPath);
                file.Write(idxManifest);
                file.Close();

                result = true;
            }
            return result;
        }

        private String BuildHLSIndexManifest(String externalID, UInt64 channelId, List<String> manifestNames)
        {
            String result = "";
            String newLine = Environment.NewLine;

            result += "#EXTM3U" + newLine;

            foreach (String manifestName in manifestNames)
            {
                String streaminfo = DBManager.Instance.GetHLSIndexStream(channelId, manifestName);
                if (!String.IsNullOrEmpty(streaminfo))
                {
                    result += "#EXT-X-STREAM-INF:" + streaminfo + newLine;
                    result += externalID + "-" + manifestName + newLine;
                }
            }

            return result;
        }

        private String BuildHLSManifest(List<HLSChunk> hlsChunks, String webRoot)
        {
            String result = "";
            String newLine = Environment.NewLine;

            result += "#EXTM3U" + newLine;
            result += "#EXT-X-VERSION:" + hlsChunks[0].EXTXVERSION.ToString() + newLine;
            result += "#EXT-X-TARGETDURATION:" + hlsChunks[0].EXTXTARGETDURATION.ToString() + newLine;
            result += "#EXT-X-MEDIA-SEQUENCE:" + hlsChunks[0].EXTXMEDIASEQUENCE.ToString() + newLine;

            String previousIV = String.Empty;
            foreach (HLSChunk chunk in hlsChunks)
            {

                if (!String.IsNullOrEmpty(chunk.EXTXKEY))
                {
                    if (String.IsNullOrEmpty(previousIV) || !IsSameIVSequence(previousIV, chunk.EXTXKEY))
                        result += "#EXT-X-KEY:" + chunk.EXTXKEY + newLine;
                }

                result += "#EXTINF:" + chunk.EXTINF + newLine;

                if (!webRoot.EndsWith("/"))
                    webRoot += "/";
                Uri baseUri = new Uri(webRoot);
                Uri myUri = new Uri(baseUri, chunk.URI.Replace("\\", "/"));
                result += myUri.ToString() + newLine;

                previousIV = chunk.EXTXKEY;
            }
            result += "#EXT-X-ENDLIST" + newLine;

            return result;
        }

        private Boolean IsSameIVSequence(String key1, String key2)
        {
            String IV1 = "";
            String IV2 = "";

            foreach (String sl in key1.Split(','))
            {
                if (sl.Contains("IV"))
                    IV1 = sl.Split('=')[1];
            }

            foreach (String sl in key2.Split(','))
            {
                if (sl.Contains("IV"))
                    IV2 = sl.Split('=')[1];
            }

            String sub1 = IV1.Substring(0, 18);
            String sub2 = IV2.Substring(0, 18);

            return (sub1 == sub2);
        }

        //private List<ContentData> GetContentToProccess(String type, List<String> channelsToProces)
        //{
        //    List<EPGChannel> channels = CatchupHelper.GetAllEPGChannels();
        //    // check content 
        //    List<ContentData> allcontents = new List<ContentData>();
        //    List<AvailableDateTime> availableDateTimes = DBManager.Instance.GetHLSAvailableDateTime(channelsToProces);
        //    foreach (AvailableDateTime availableDateTime in availableDateTimes)
        //    {
        //        log.Debug("For HLS storage: ChannelId " + availableDateTime.ChannelId + " availableDateTimes " + availableDateTime.UTCMaxEndtime.ToString("yyyy-MM-dd HH:mm:ss"));
        //        var channel = channels.First(c => c.MppContentId == availableDateTime.ChannelId);
        //        String CROName = channel.ContentRightOwner;
        //        List<ContentData> contents = GetUnprocessedCatchUpContents(availableDateTime, type, CROName);
        //        log.Debug("Found " + contents.Count + " countent to generate for this channel.");
        //        allcontents.AddRange(contents);
        //    }

        //    return allcontents;
        //}
        #endregion

        #region LoadToDB
        public override void ProcessArchive(List<String> channelsToProces)
        {
            log.Debug("ProcessHLSArchive start");

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == this.systemName).SingleOrDefault();
            String FileArchiveRootFolder = systemConfig.GetConfigParam("FileArchiveRootFolder");
            var workflowConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String EPGChannelConfigXML = workflowConfig.GetConfigParam("EPGChannelConfigXML");

            XmlDocument epgConfigDoc = new XmlDocument();
            epgConfigDoc.Load(EPGChannelConfigXML);

            if (!Directory.Exists(FileArchiveRootFolder))
            {
                log.Error("directory " + FileArchiveRootFolder + " doesn't exist.");
                log.Debug("ProcessHLSArchive end");
                return;
            }

            String[] fodlers = Directory.GetDirectories(FileArchiveRootFolder);
            foreach (String fodler in fodlers)
            {
                String ChannelId = Path.GetFileName(fodler);
                if (channelsToProces != null) {
                    // run defiend folders only
                    if (!channelsToProces.Contains(ChannelId)) {
                            continue; // skip this foler. not set for this task
                    }
                }

                String[] files = Directory.GetFiles(fodler);
                log.Debug(files.Length + " files to processs in " + fodler);

                if (files.Length == 0)
                    continue;


                XmlElement channelNode = (XmlElement)epgConfigDoc.SelectSingleNode("EPGChannelConfig/Channels/Channel[@id='" + ChannelId + "']");
                if (channelNode == null)
                {
                    log.Error("ChannelId " + ChannelId + "no found in EPGChannelConfigXML");
                    continue;
                }
                XmlElement hlsnode = (XmlElement)channelNode.SelectSingleNode("HLS");
                if (hlsnode == null)
                {
                    log.Error("HLS not defined in EPGChannelConfigXML for ChannelId " + ChannelId);
                    continue;
                }

                TimeZoneInfo hlstimeZone;
                try
                {
                    hlstimeZone = TimeZoneInfo.FindSystemTimeZoneById(hlsnode.GetAttribute("encodeInTimezone"));
                }
                catch (Exception ex)
                {
                    log.Error("Failed to find timeZone with id " + hlsnode.GetAttribute("encodeInTimezone"), ex);
                    continue;
                }

                foreach (String file in files)
                {
                    try
                    {
                        log.Debug("load ChannelId:" + ChannelId + " file:" + file + " hlstimeZone:" + hlstimeZone.Id);
                        StreamReader streamReader = new StreamReader(file);
                        String manifestFile = streamReader.ReadToEnd();
                        streamReader.Close();
                        if (IsIndexList(manifestFile))
                            LoadHLSIndexList(UInt64.Parse(ChannelId), manifestFile);
                        else
                            LoadHLSPlayList(UInt64.Parse(ChannelId), file, manifestFile, hlstimeZone);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failed to load " + file + " to DB", ex);
                        continue;
                    }
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Failed to delete " + file, ex);
                    }
                }
            }

            log.Debug("ProcessHLSArchive end");
        }


         private Boolean IsIndexList(String manifestFile)
        {
            return (manifestFile.IndexOf("#EXT-X-STREAM-INF") > 0);
        }

         private void LoadHLSIndexList(UInt64 channelId, String manifestFile)
        {

            String[] lines = manifestFile.Split(Environment.NewLine.ToCharArray(),
                                    StringSplitOptions.RemoveEmptyEntries);

            String EXTXSTREAMINF = "";
            foreach (String line in lines)
            {
                if (line.Contains("#EXT-X-STREAM-INF"))
                {
                    EXTXSTREAMINF = line.Split(':')[1];
                }
                if (line.Contains(".m3u8"))
                {
                    String bitrateFileName = GetBitrateFileName(line);

                    Int32 result = DBManager.Instance.UpdateHLSIndex(channelId, bitrateFileName, EXTXSTREAMINF);
                    if (result == 0)
                        DBManager.Instance.AddHLSIndex(channelId, bitrateFileName, EXTXSTREAMINF);

                }
            }
        }

        private void LoadHLSPlayList(UInt64 channelId, String hlsPlayList, String manifestFile, TimeZoneInfo timezone)
        {

            String[] lines = manifestFile.Split(Environment.NewLine.ToCharArray(),
                                    StringSplitOptions.RemoveEmptyEntries);

            // process data
            List<HLSChunk> hlsChunks = new List<HLSChunk>();
            String extXKey;
            Int32 EXTXVERSION = 0;
            Int32 EXTXMEDIASEQUENCE = 0;
            Int32 EXTXTARGETDURATION = 0;
            String METHOD = "";
            String URI = "";
            String IV = "";
            HLSChunk chunk = new HLSChunk();
            foreach (String line in lines)
            {

                if (line.Contains("#EXT-X-VERSION"))
                {
                    EXTXVERSION = Int32.Parse(line.Split(':')[1]);
                }
                else if (line.Contains("#EXT-X-TARGETDURATION"))
                {
                    EXTXTARGETDURATION = Int32.Parse(line.Split(':')[1]);
                }
                else if (line.Contains("#EXT-X-MEDIA-SEQUENCE"))
                {
                    EXTXMEDIASEQUENCE = Int32.Parse(line.Split(':')[1]);
                }
                else if (line.Contains("#EXT-X-KEY"))
                {
                    //#EXT-X-KEY:METHOD=AES-128-CX,IV=0x11121314151617180000000100000000,URI="conaxdrm:<drm ID>:<base64 encoded info>" 
                    String subLine = line.Substring(line.IndexOf(':') + 1);
                    foreach (String sl in subLine.Split(','))
                    {
                        if (sl.Contains("METHOD"))
                            METHOD = sl.Split('=')[1];
                        if (sl.Contains("IV"))
                            IV = sl.Split('=')[1];
                        if (sl.Contains("URI"))
                        {
                            URI = sl.Substring(sl.IndexOf('=') + 1);
                        }
                    }
                }
                else if (line.Contains("#EXTINF"))
                {
                    chunk.EXTINF = line.Split(':')[1];
                }
                else if (!line.StartsWith("#"))
                {
                    chunk.URI = line;

                    if (!String.IsNullOrEmpty(IV))
                    {
                        chunk.EXTXKEY = "METHOD=" + METHOD + ",IV=" + IV + ",URI=" + URI;
                        IV = InCreaseIV(IV);
                    }
                    chunk.EXTXTARGETDURATION = EXTXTARGETDURATION;
                    chunk.EXTXVERSION = EXTXVERSION;

                    chunk.EXTXMEDIASEQUENCE = EXTXMEDIASEQUENCE;
                    EXTXMEDIASEQUENCE++;

                    DateTime? utcTime = GetHLSStartTimeInUTC(line, timezone);
                    if (utcTime == null)
                    {
                        log.Error("failed to get start time for segement " + line);
                        chunk = new HLSChunk();
                        continue;
                    }
                    chunk.UTCStarttime = utcTime.Value;
                    chunk.UTCEndtime = chunk.UTCStarttime.AddSeconds(Double.Parse(chunk.EXTINF.Split(',')[0], CultureInfo.InvariantCulture));
                    chunk.ChannelId = channelId;
                    String bitrateName = GetBitrateFileName(hlsPlayList.Split('-')[1]);
                    chunk.PlayListName = bitrateName;

                    hlsChunks.Add(chunk);
                    // reset.
                    chunk = new HLSChunk();
                }
            }

            // find last new index pointer.
            Int32 pointer = 0;
            pointer = GetIndexPointer(hlsChunks);
            log.Debug("pointer " + pointer + " / " + (hlsChunks.Count - 1));
            // save to db
            //foreach (HLSChunk hlsChunk in hlsChunks)
            //{
            //    Boolean result = DBManager.Instance.IsHLSSegmentExist(hlsChunk);
            //    if (result)
            //        continue; // already registered int he DB

            //    DBManager.Instance.AddHLSSegment(hlsChunk);
            //}
            Boolean result = true;
            for (Int32 x = pointer; x < hlsChunks.Count; x++ )
            {
                HLSChunk hlsChunk = hlsChunks[x];
                if (result) // if pre exist
                    result = DBManager.Instance.IsHLSSegmentExist(hlsChunk);

                if (!result)
                { // if las check no exist
                    try
                    {
                        DBManager.Instance.AddHLSSegment(hlsChunk);
                    }
                    catch (Exception ex) {
                        if (!(ex.Message.IndexOf("are not unique") > 0))
                            throw;
                        //log.Warn(ex.Message + " " + hlsChunk.CubiChannelId + " " + hlsChunk.URI);
                        //else
                        //    throw;
                    }
                }
            }
        }

        private static Int32 GetIndexPointer(List<HLSChunk> hlsChunks)
        {
            /*
            if (hlsChunks.Count == 0)
                return 0;

            HLSChunk lastChunk = DBManager.Instance.GetLastHLSChunks(hlsChunks[0].CubiChannelId, hlsChunks[0].PlayListName);
            if (lastChunk == null)
                return 0;

            for(Int32 x = hlsChunks.Count - 1; x >= 0; x--) {
                if (hlsChunks[x].URI.Equals(lastChunk.URI))
                    return x;
            }

            // outside of the list
            if (hlsChunks[hlsChunks.Count - 1].UTCStarttime < lastChunk.UTCStarttime)
                return hlsChunks.Count - 1;
            else
                return 0;
            */
            
            Decimal min = 0;
            Decimal max = hlsChunks.Count - 1;
            Int32 pointer = 0;
            //Boolean result = false;
            while ((max - min) > 1)
            {

                pointer = (Int32)Math.Round((max - min) / 2) + (Int32)min;
                //log.Debug("MIN:" + min + " MAX:" + max + " pointer:" + pointer);
                HLSChunk hlsChunk = hlsChunks[pointer];
                Boolean result = DBManager.Instance.IsHLSSegmentExist(hlsChunk);
                //if (pointer >= 2998)
                //    result = false;
                //else
                //    result = true;
                if (result)                
                    min = pointer;
                else
                    max = pointer;               
            }
            //log.Debug("min: " + min);
            return (Int32)min;
             
        }

        private DateTime? GetHLSStartTimeInUTC(String fileName, TimeZoneInfo timezone)
        {

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == this.systemName).SingleOrDefault();
            String FileArchiveRootFolder = systemConfig.GetConfigParam("HLSCatchUpFileDateTimeRegExp");

            String tsfile = Path.GetFileName(fileName);

            Match match = Regex.Match(tsfile,
                                      FileArchiveRootFolder,
                                      RegexOptions.IgnoreCase);
            if (match.Success)
            {
                DateTime catchUpStart = DateTime.ParseExact(match.Value, systemConfig.GetConfigParam("HLSCatchUpFileDateTimeFormat"), null);
                DateTime utcTime = TimeZoneInfo.ConvertTime(catchUpStart,
                                                            timezone,
                                                            TimeZoneInfo.Utc);

                return utcTime;
            }
            return null;
        }

        private String InCreaseIV(String extXKey)
        {
            //extXKey
            if (!extXKey.StartsWith("0x"))
                throw new Exception("extXKey is not a Hex String: " + extXKey);

            if (extXKey.Length != 34)
                throw new Exception("extXKey is not 127 bit long Hex string: " + extXKey);

            String block1 = extXKey.Substring(0, 18);
            String block2 = extXKey.Substring(18, 8);
            String block3 = extXKey.Substring(26);

            Int32 iv = Convert.ToInt32(block2, 16);
            iv++;
            block2 = iv.ToString("X8");
            return block1 + block2 + block3;
        }

        protected virtual String GetBitrateFileName(String orginalStr) {
            return orginalStr;
        }
        #endregion

        #region Delete
        public override void DeleteCatchupSegments(EPGChannel epgChannel)
        {
            // TODO: we don't support generate of playlist righ tnow.
            throw new NotImplementedException();
            //List<HLSChunk> hlsChunks = DBManager.Instance.GetHLSChunks(epgChannel.MppContentId, epgChannel.DeleteTimeHLSBuffer);
            //log.Debug(hlsChunks.Count + " of hls segments to delete for ChannelId:" + epgChannel.MppContentId + " deletefrom:" + epgChannel.DeleteTimeHLSBuffer.ToString("yyyy-MM-dd HH:mm:ss"));
            ////EnvivioCatchUpFileHandler catchUpFileHandler = new EnvivioCatchUpFileHandler();
            //catchUpFileHandler.DeleteHLSCatchUpFiles(hlsChunks, epgChannel.HLSCatchUpFSRoot);
            //// remove from db
            //try
            //{
            //    DBManager.Instance.DeleteHLSChunks(epgChannel.MppContentId, epgChannel.DeleteTimeHLSBuffer);
            //}
            //catch (Exception ex)
            //{
            //    log.Error("Failed to delete hls segments data in DB", ex);
            //}
        }
        #endregion
    }

    class ThreadParameter {
        public String Folder { get; set; }
        public XmlDocument epgConfigDoc { get; set; }
        public String TaskName { get; set; }
    }
}
