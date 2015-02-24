using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.System;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database
{
    public interface IDBWrapper
    {

        #region WorkFlowProcess

        void AddWorkFlowProcess(WorkFlowProcess workFlowProcess);

        void UpdateWorkFlowProcess(WorkFlowProcess workFlowProcess);

        void DeleteWorkFlowProcessesByWorkFlowJobId(UInt64 workFlowJobId);

        List<WorkFlowProcess> GetWorkFlowProcessesByWorkFlowJobId(UInt64 workFlowJobId);

        Int32 CountWorkFlowTries(UInt64 eventObjectId);

        void UpdateWorkFlowJobNotUntil(ulong workFlowJobID, DateTime newNotUntil);

        DateTime LastOccurredDateForTask(String taskName, ulong serviceObjectId);

        void AddOccuredDateForTask(String taskName, ulong serviceObjectId, DateTime executeDate);

        Int32 UpdateOccuredDateForTask(String taskName, ulong serviceObjectId, DateTime executeDate);

        #endregion

        #region WorkFlowJob

        WorkFlowJob GetLastMPPStationServerEvent();

        void AddWorkFlowJob(WorkFlowJob workFlowJob);

        List<WorkFlowJob> GetWorkFlowJobs(WorkFlowJobState state, List<EventType> eventTypes);

        void UpdateWorkFlowJob(WorkFlowJob workFlowJob);

        int GetNumberOfTries(ulong workFlowJobId, String handlerName);

        #endregion

        #region MPPStationServerEvent

        //MPPStationServerEvent GetLastMPPStationServerEvent();

        void AddMPPStationServerEvent(MPPStationServerEvent mppEvent);

        List<MPPStationServerEvent> GetMPPStationServerEvents(MPPEventProcessState state, List<EventType> eventTypes);

        List<MPPStationServerEvent> GetMPPStationServerEvents(MPPEventProcessState state);

        void UpdateMPPStationServerEvent(MPPStationServerEvent mppEvent);

        #endregion

        #region CatchUp segments

        List<String> GetHLSManifestNames(ContentData content);

        List<String> GetHLSManifestNames(ContentData content, DateTime UTCStarttime, DateTime UTCEndtime);

        List<String> GetHLSManifestNames(UInt64 channelId, DateTime UTCStarttime, DateTime UTCEndtime);

        List<HLSChunk> GetHLSChunks(ContentData content, String manifestName);

        List<HLSChunk> GetHLSChunks(ContentData content, String manifestName, DateTime UTCStarttime, DateTime UTCEndtime);

        List<HLSChunk> GetHLSChunks(UInt64 channelId, String manifestName, DateTime UTCStarttime, DateTime UTCEndtime);

        List<HLSChunk> GetHLSChunks(UInt64 channelId, DateTime fromDate);

        HLSChunk GetLastHLSChunks(UInt64 channelId, String PlayListName);

        void DeleteHLSChunks(UInt64 channelId, DateTime fromDate);

        List<AvailableDateTime> GetHLSAvailableDateTime(List<String> channelsToProces);

        Boolean IsHLSSegmentExist(HLSChunk hlsChunk);

        void AddHLSSegment(HLSChunk hlsChunk);

        void AddHLSIndex(UInt64 channelId, String playListName, String EXTXSTREAMINF);

        Int32 UpdateHLSIndex(UInt64 channelId, String playListName, String EXTXSTREAMINF);

        String GetHLSIndexStream(UInt64 channelId, String manifestName);
        
        void AddSSManifest(SSManifest ssManifest);

        Boolean IsSSManifestExist(UInt64 channelId, String manifestFileName);

        List<SSManifest> GetSSManifests(ContentData content);

        List<AvailableDateTime> GetSSAvailableDateTime();

        List<SSManifest> GetSSManifests(UInt64 channelId, DateTime fromDate);

        void DeleteSSManifest(UInt64 channelId, DateTime fromDate);

        #endregion

        void CleanDB(DateTime toDate);

        void ClearAndDefragDB();
    }
}
