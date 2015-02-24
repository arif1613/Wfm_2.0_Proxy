using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration
{
    public class ConaxWorkflowManagerConfig : SystemConfig
    {
        public ConaxWorkflowManagerConfig(XmlNode systemConfigNode) : base(systemConfigNode) { }

        /// <summary>
        /// Defines how many minutes to wait before asking for recordings for a certain content again.
        /// This parameter is optional, default value is 5 minutes.
        /// Used by:
        ///     NPVR
        /// </summary>
        private Int32? _RequestForContentRecordingsTimeout;
        public Int32 RequestForContentRecordingsTimeout
        {
            get
            {
                if (!_RequestForContentRecordingsTimeout.HasValue)
                {
                    var value = 5;
                    if (this.ConfigParams.ContainsKey("RequestForContentRecordingsTimeout"))
                        value = Int32.Parse(this.GetConfigParam("RequestForContentRecordingsTimeout"));
                    _RequestForContentRecordingsTimeout = value;
                }
                return _RequestForContentRecordingsTimeout.Value;
            }
            set { _RequestForContentRecordingsTimeout = value; }
        }

        public Int32 MaxExternalIdsToSendPerCall
        {
            get
            {
                Int32 result = 100;

                if (this.ConfigParams.ContainsKey("NumberOfExternalIdsToSendPerCall"))
                    result = Int32.Parse(this.GetConfigParam("NumberOfExternalIdsToSendPerCall"));

                return result;
            }
        }

        /// <summary>
        /// Defines how many retries should be made to get all recordings for a content.
        /// This parameter is optional, default value is 3.
        /// Used by:
        ///     NPVR
        /// </summary>
        public Int32 RequestForContentRecordingsRetries
        {
            get
            {
                var value = 3;
                if (this.ConfigParams.ContainsKey("RequestForContentRecordingsRetries"))
                    value = Int32.Parse(this.GetConfigParam("RequestForContentRecordingsRetries"));
                return value;
            }
        }

        /// <summary>
        /// Defines how many seconds pre guard time to reserve in the live strem buffer.
        /// This parameter is optional, default value is 1800 seconds.
        /// Used by:
        ///     NPVR
        /// </summary>
        public UInt32 NPVRBufferPreGuardInSec {
            get {
                UInt32 value = 30 * 60;
                if (this.ConfigParams.ContainsKey("NPVRBufferPreGuardInSec"))
                    value = UInt32.Parse(this.GetConfigParam("NPVRBufferPreGuardInSec"));
                return value;
            }
        }

        /// <summary>
        /// Defines pre guard time to append for the NPVR asset, apart from the user defiend pre guard that is.
        /// This is a optional parameter, default value is 0.
        /// Used by:
        ///     NPVR
        /// </summary>
        public UInt32 NPVRRecordingPreGuardInSec
        {
            get
            {
                UInt32 value = 0;
                if (this.ConfigParams.ContainsKey("NPVRRecordingPreGuardInSec"))
                    value = UInt32.Parse(this.GetConfigParam("NPVRRecordingPreGuardInSec"));
                return value;
            }
        }

        /// <summary>
        /// Defines post guard time to append for the NPVR asset, apart from the user defiend post guard that is.
        /// This is a optional parameter, default value is 0.
        /// Used by:
        ///     NPVR
        /// </summary>
        public UInt32 NPVRRecordingPostGuardInSec
        {
            get
            {
                UInt32 value = 0;
                if (this.ConfigParams.ContainsKey("NPVRRecordingPostGuardInSec"))
                    value = UInt32.Parse(this.GetConfigParam("NPVRRecordingPostGuardInSec"));
                return value;
            }
        }

        /// <summary>
        /// Defiens which server implementation to use for handling Smooth stream.
        /// Used by:
        ///     NPVR, CatchUp
        /// </summary>
        private BaseEncoderCatchupHandler _SmoothCatchUpHandler = null;
        public BaseEncoderCatchupHandler SmoothCatchUpHandler
        { 
            get {
                if (_SmoothCatchUpHandler == null)
                    _SmoothCatchUpHandler = Activator.CreateInstance(System.Type.GetType(this.GetConfigParam("SmoothCatchUpHandler"))) as BaseEncoderCatchupHandler;

                return _SmoothCatchUpHandler;
            }            
           set { _SmoothCatchUpHandler = value; }
        }

        /// <summary>
        /// Defiens which server implementation to use for handling HLS stream.
        /// Used by:
        ///     NPVR, CatchUp
        /// </summary>
        private BaseEncoderCatchupHandler _HLSCatchUpHandler = null;
        public BaseEncoderCatchupHandler HLSCatchUpHandler
        {
            get
            {
                if (_HLSCatchUpHandler == null)
                    _HLSCatchUpHandler =  Activator.CreateInstance(System.Type.GetType(this.GetConfigParam("HLSCatchUpHandler"))) as BaseEncoderCatchupHandler;
                
                return _HLSCatchUpHandler;
            }
            set { _HLSCatchUpHandler = value; }
        }

        /// <summary>
        /// Defiens the path to the Upload folder configuration xml.
        /// Used by:
        ///     VOD and Channel ingest.
        /// </summary>
        public String NeedQAPublishDir
        {
            get
            {
                return this.GetConfigParam("NeedQAPublishDir");
            }
        }

        public String DirectPublishDir
        {
            get
            {
                return this.GetConfigParam("DirectPublishDir");
            }
        }
        public String FileIngestUploadDirectoryConfig {
            get {
                return this.GetConfigParam("FileIngestUploadDirectoryConfig");
            }
        }

        public String DefaultVodCoverImageFileName
        {
            get
            {
                return this.GetConfigParam("DefaultVodCoverImageFileName");
            }
        }

        /// <summary>
        /// Defiens name of the foldersetting config file.
        /// Used by:
        ///     VOD and Channel ingest.
        /// </summary>
        public String FolderSettingsFileName {
            get {
                return this.GetConfigParam("FolderSettingsFileName");
            }
        }

        /// <summary>
        /// Defiens what file handler implementation will be used for handling ingest files.
        /// Used by:
        ///     VOD and Channel ingest.
        /// </summary>
        public IFileHandler FileIngestHandlerType
        {
            get
            {
                return Activator.CreateInstance(System.Type.GetType(this.GetConfigParam("FileIngestHandlerType"))) as IFileHandler;
            }
        }

        /// <summary>
        /// Defiens which event dispatcher implementation to use.
        /// Used by:
        ///     VOD and Channel ingest.
        /// </summary>
        public String MPPEventDispatcher {
            get { 
                return this.GetConfigParam("MPPEventDispatcher");
            }
        }

        /// <summary>
        /// Defines the processed dir path for the ingested items.
        /// Userd by:
        ///     VOD and Channel ingest.
        /// </summary>
        public String FileIngestUploadDirectory
        {
            get
            {
                return this.GetConfigParam("FileIngestUploadDirectory");
            }
        }
        public String FileIngestProcessedDirectory {
            get {
                return this.GetConfigParam("FileIngestProcessedDirectory");
            }
        }

        /// <summary>
        /// Defines the rejected dir path for the ingest items.
        /// Used by:
        ///     VOD and Channel ingest.
        /// </summary>
        public String FileIngestRejectDirectory {
            get {
                return this.GetConfigParam("FileIngestRejectDirectory");
            }
        }

        /// <summary>
        /// Defiens the work dir path for the ingest items.
        /// Used by:
        ///     VOD and Channel ingest.
        /// </summary>
        public String FileIngestWorkDirectory {
            get {
                return this.GetConfigParam("FileIngestWorkDirectory");
            }
        }

        /// <summary>
        /// Defines the storeage dir for ingest items.
        /// Used by:
        ///     VOD and Channel ingest.
        /// </summary>
        public String SourceStorageDirectory {
            get {
                return this.GetConfigParam("SourceStorageDirectory");
            }
        }

        /// <summary>
        /// Defines the location of the EPG channel onfig file.
        /// Used by:
        ///     Catchup and NPVR.
        /// </summary>
        private String _EPGChannelConfigXML;
        public String EPGChannelConfigXML { 
            get {
                if (String.IsNullOrWhiteSpace(_EPGChannelConfigXML))
                    return this.GetConfigParam("EPGChannelConfigXML");
                else
                    return _EPGChannelConfigXML;
            }
            set { _EPGChannelConfigXML = value; }
        }

        /// <summary>
        /// Defines the location of the CatchUpFilterConfigXML onfig file.
        /// Used by:
        ///     Catchup.
        /// </summary>
        private String _CatchUpFilterConfigXML;
        public String CatchUpFilterConfigXML
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_CatchUpFilterConfigXML))
                    return this.GetConfigParam("CatchUpFilterConfigXML");
                else
                    return _CatchUpFilterConfigXML;
            }
            set { _CatchUpFilterConfigXML = value; }
        }

        /// <summary>
        /// Defiens how much extra pre guard time in seconds to add for each Catchup item.
        /// Default value is 0, if htis value is not set.
        /// Used by:
        ///     catch-up
        /// </summary>
        public Int32 EPGStartTimePendingSec {
            get {
                Int32 result = 0;

                if (this.ConfigParams.ContainsKey("EPGStartTimePendingSec"))
                    result = Int32.Parse(this.GetConfigParam("EPGStartTimePendingSec"));

                return result;
            }
        }

        /// <summary>
        /// Defiens how much extra post guard time in seconds to add for each Catchup item.
        /// Default value is 0, if htis value is not set.
        /// Used by:
        ///     catch-up
        /// </summary>
        public Int32 EPGEndTimePendingSec {
            get {
                Int32 result = 0;

                if (this.ConfigParams.ContainsKey("EPGEndTimePendingSec"))
                    result = Int32.Parse(this.GetConfigParam("EPGEndTimePendingSec"));

                return result;
            }
        }

        /// <summary>
        /// Defines how old EPG data from Feeds to use.
        /// Default is 0 hours from now.
        /// Used by:
        ///     catchup and NPVR.
        /// </summary>
        public Int32 EPGHistoryInHours
        {
            get { 
                Int32 res = 0;
                if (this.ConfigParams.ContainsKey("EPGHistoryInHours"))
                    int.TryParse(this.GetConfigParam("EPGHistoryInHours"), out res);
                return res;
            }
        }
        

        /// <summary>
        /// Defiens the encoders offset time.        
        /// Used by:
        ///     catch-up and NPVR.
        /// </summary>
        public String CatchUpEncoderOffset
        {
            get {                
                return this.GetConfigParam("CatchUpEncoderOffset");;
            }
        }

        private String _DBWrapper;
        public String DBWrapper
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_DBWrapper))
                    _DBWrapper = this.GetConfigParam("DBWrapper");
                return _DBWrapper;
            }
            set { _DBWrapper = value; }
        }

        private String _DBWrapperAssembly;
        public String DBWrapperAssembly
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_DBWrapperAssembly) &&
                    this.ConfigParams.ContainsKey("DBWrapperAssembly"))
                    _DBWrapperAssembly = this.GetConfigParam("DBWrapperAssembly");
                return _DBWrapperAssembly;
            }
            set { _DBWrapperAssembly = value; }
        }

        private Int32 _MAXArchiveThreads;
        public Int32 MAXArchiveThreads
        {
            get
            {
                if (this.ConfigParams.ContainsKey("MAXArchiveThreads"))
                {
                    try
                    {
                        _MAXArchiveThreads = Int32.Parse(this.GetConfigParam("MAXArchiveThreads"));

                    }
                    catch (Exception)
                    {
                        _MAXArchiveThreads = 50;
                    }
                }
                else
                {
                    _MAXArchiveThreads = 50;
                }
                return _MAXArchiveThreads;
            }
            set { _MAXArchiveThreads = value; }
        }

     

        private Int32? _RecordingRetries;
        public Int32 RecordingRetries
        {
            get
            {
                if (_RecordingRetries.HasValue)
                    return _RecordingRetries.Value;
                
                _RecordingRetries = 3;
                if (this.ConfigParams.ContainsKey("RecordingRetries"))
                    _RecordingRetries = Int32.Parse(this.GetConfigParam("RecordingRetries"));

                return _RecordingRetries.Value;
            }
            set { _RecordingRetries = value; }
        }

        public Int32 NPVRArchiveTaskFetchContentIntervalInSec
        {
            get
            {
                Int32 result = 60;

                if (this.ConfigParams.ContainsKey("NPVRArchiveTaskFetchContentIntervalInSec"))
                    result = Int32.Parse(this.GetConfigParam("NPVRArchiveTaskFetchContentIntervalInSec"));

                return result;
            }
        }

        public Int32 FetchNPVRRecordingsForNumberOfEPGPerCall
        {
            get
            {
                Int32 result = 10;

                if (this.ConfigParams.ContainsKey("FetchNPVRRecordingsForNumberOfEPGPerCall"))
                    result = Int32.Parse(this.GetConfigParam("FetchNPVRRecordingsForNumberOfEPGPerCall"));

                return result;
            }
        }

        public Int32 MaxGetNPVRRecordingsPerPage
        {
            get
            {
                Int32 result = 250;

                if (this.ConfigParams.ContainsKey("MaxGetNPVRRecordingsPerPage"))
                    result = Int32.Parse(this.GetConfigParam("MaxGetNPVRRecordingsPerPage"));

                return result;
            }
        }

        public Int32 MaxNumberNPVRRecordingsForUpdate
        {
            get
            {
                Int32 result = 1000;

                if (this.ConfigParams.ContainsKey("MaxNumberNPVRRecordingsForUpdate"))
                    result = Int32.Parse(this.GetConfigParam("MaxNumberNPVRRecordingsForUpdate"));

                return result;
            }
        }

        public Int32 SleepBetweenNPVRRecordingBulkUpdateInSec
        {
            get
            {
                Int32 result = 2;

                if (this.ConfigParams.ContainsKey("SleepBetweenNPVRRecordingsUpdateInSec"))
                    result = Int32.Parse(this.GetConfigParam("SleepBetweenNPVRRecordingsUpdateInSec"));

                return result;
            }
        }


        public Int32 FetchNumberOfReadyToPurgeEPG
        {
            get
            {
                Int32 result = 500;

                if (this.ConfigParams.ContainsKey("FetchNumberOfReadyToPurgeEPG"))
                    result = Int32.Parse(this.GetConfigParam("FetchNumberOfReadyToPurgeEPG"));

                return result;
            }
        }

        public Int32 WarningThresholdForNumberOfEPGInWaitList
        {
            get
            {
                Int32 result = 1000;

                if (this.ConfigParams.ContainsKey("WarningThresholdForNumberOfEPGInWaitList"))
                    result = Int32.Parse(this.GetConfigParam("WarningThresholdForNumberOfEPGInWaitList"));

                return result;
            }
        }

        private string _ExtraNPVRAssetLog;
        public String ExtraNPVRAssetLog
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(_ExtraNPVRAssetLog))
                    return _ExtraNPVRAssetLog;

                String result = String.Empty;
                if (this.ConfigParams.ContainsKey("ExtraNPVRAssetLog")) {
                    _ExtraNPVRAssetLog = this.GetConfigParam("ExtraNPVRAssetLog");
                    //if (_ExtraNPVRAssetLog.EndsWith("\"") || _ExtraNPVRAssetLog.EndsWith("/"))
                    //    _ExtraNPVRAssetLog = _ExtraNPVRAssetLog.Remove(_ExtraNPVRAssetLog.Length - 1);

                }
                return _ExtraNPVRAssetLog;
            }
        }

        public Int32 PagesToFetchNPVRRecordingsInParallel
        {
            get
            {
                Int32 result = 4;

                if (this.ConfigParams.ContainsKey("PagesToFetchNPVRRecordingsInParallel"))
                    result = Int32.Parse(this.GetConfigParam("PagesToFetchNPVRRecordingsInParallel"));

                return result;
            }
        }
    }
}
