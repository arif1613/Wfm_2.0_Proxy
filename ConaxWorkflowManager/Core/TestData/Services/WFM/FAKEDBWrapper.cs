using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.WFM
{
    public class FAKEDBWrapper : IDBWrapper
    {
        public void AddWorkFlowProcess(ConaxWorkflowManager.Core.WorkFlow.WorkFlowProcess workFlowProcess)
        {
            throw new NotImplementedException();
        }

        public void UpdateWorkFlowProcess(ConaxWorkflowManager.Core.WorkFlow.WorkFlowProcess workFlowProcess)
        {
            throw new NotImplementedException();
        }

        public void DeleteWorkFlowProcessesByWorkFlowJobId(ulong workFlowJobId)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.WorkFlow.WorkFlowProcess> GetWorkFlowProcessesByWorkFlowJobId(ulong workFlowJobId)
        {
            throw new NotImplementedException();
        }

        public int CountWorkFlowTries(ulong eventObjectId)
        {
            throw new NotImplementedException();
        }

        public void UpdateWorkFlowJobNotUntil(ulong workFlowJobID, DateTime newNotUntil)
        {
            throw new NotImplementedException();
        }

        public DateTime LastOccurredDateForTask(string taskName, ulong serviceObjectId)
        {
            return DateTime.MinValue;
        }

        public void AddOccuredDateForTask(string taskName, ulong serviceObjectId, DateTime executeDate)
        {
            
        }

        public int UpdateOccuredDateForTask(string taskName, ulong serviceObjectId, DateTime executeDate)
        {
            return 0;
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.System.WorkFlowJob GetLastMPPStationServerEvent()
        {
            throw new NotImplementedException();
        }

        public void AddWorkFlowJob(ConaxWorkflowManager.Core.Util.ValueObjects.System.WorkFlowJob workFlowJob)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.System.WorkFlowJob> GetWorkFlowJobs(ConaxWorkflowManager.Core.Util.Enums.WorkFlowJobState state, List<ConaxWorkflowManager.Core.Util.Enums.EventType> eventTypes)
        {
            throw new NotImplementedException();
        }

        public void UpdateWorkFlowJob(ConaxWorkflowManager.Core.Util.ValueObjects.System.WorkFlowJob workFlowJob)
        {
            throw new NotImplementedException();
        }

        public int GetNumberOfTries(ulong workFlowJobId, string handlerName)
        {
            throw new NotImplementedException();
        }

        public void AddMPPStationServerEvent(ConaxWorkflowManager.Core.Util.ValueObjects.MPPStationServerEvent mppEvent)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.MPPStationServerEvent> GetMPPStationServerEvents(ConaxWorkflowManager.Core.Util.Enums.MPPEventProcessState state, List<ConaxWorkflowManager.Core.Util.Enums.EventType> eventTypes)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.MPPStationServerEvent> GetMPPStationServerEvents(ConaxWorkflowManager.Core.Util.Enums.MPPEventProcessState state)
        {
            throw new NotImplementedException();
        }

        public void UpdateMPPStationServerEvent(ConaxWorkflowManager.Core.Util.ValueObjects.MPPStationServerEvent mppEvent)
        {
            throw new NotImplementedException();
        }

        public List<string> GetHLSManifestNames(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public List<string> GetHLSManifestNames(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content, DateTime UTCStarttime, DateTime UTCEndtime)
        {
            throw new NotImplementedException();
        }

        public List<string> GetHLSManifestNames(ulong channelId, DateTime UTCStarttime, DateTime UTCEndtime)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.HLSChunk> GetHLSChunks(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content, string manifestName)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.HLSChunk> GetHLSChunks(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content, string manifestName, DateTime UTCStarttime, DateTime UTCEndtime)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.HLSChunk> GetHLSChunks(ulong channelId, string manifestName, DateTime UTCStarttime, DateTime UTCEndtime)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.HLSChunk> GetHLSChunks(ulong channelId, DateTime fromDate)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.HLSChunk GetLastHLSChunks(ulong channelId, string PlayListName)
        {
            throw new NotImplementedException();
        }

        public void DeleteHLSChunks(ulong channelId, DateTime fromDate)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.AvailableDateTime> GetHLSAvailableDateTime(List<string> channelsToProces)
        {
            throw new NotImplementedException();
        }

        public bool IsHLSSegmentExist(ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.HLSChunk hlsChunk)
        {
            throw new NotImplementedException();
        }

        public void AddHLSSegment(ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.HLSChunk hlsChunk)
        {
            throw new NotImplementedException();
        }

        public void AddHLSIndex(ulong channelId, string playListName, string EXTXSTREAMINF)
        {
            throw new NotImplementedException();
        }

        public int UpdateHLSIndex(ulong channelId, string playListName, string EXTXSTREAMINF)
        {
            throw new NotImplementedException();
        }

        public string GetHLSIndexStream(ulong channelId, string manifestName)
        {
            throw new NotImplementedException();
        }

        public void AddSSManifest(ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.SSManifest ssManifest)
        {
            throw new NotImplementedException();
        }

        public bool IsSSManifestExist(ulong channelId, string manifestFileName)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.SSManifest> GetSSManifests(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.AvailableDateTime> GetSSAvailableDateTime()
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.SSManifest> GetSSManifests(ulong channelId, DateTime fromDate)
        {
            throw new NotImplementedException();
        }

        public void DeleteSSManifest(ulong channelId, DateTime fromDate)
        {
            throw new NotImplementedException();
        }

        public void CleanDB(DateTime toDate)
        {
            throw new NotImplementedException();
        }

        #region IDBWrapper Members


        public void ClearAndDefragDB()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
