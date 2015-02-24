using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database.SQLite;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.System;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class MPPSyncTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
        private IDBWrapper dbwrapper = DBManager.Instance;

        public MPPSyncTask() {
            //var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            //dbwrapper = Activator.CreateInstance(System.Type.GetType(systemConfig.GetConfigParam("DBWrapper"))) as IDBWrapper;
        }

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");
            UInt64 userID = MPPIntegrationServiceManager.InstanceWithPassiveEvent.User.Id;
            try
            {
                // get the last event object
                WorkFlowJob lastMPPEvent = dbwrapper.GetLastMPPStationServerEvent();
                log.Debug("Get last Event from DB, last objectID: " + lastMPPEvent.SourceId);

                // get new MPP events  // 8209490945
                //lastMPPEvent.SourceId = 6209490945;
                List<MPPStationServerEvent> mppEvents = mppWrapper.GetEventsFromSink(lastMPPEvent.SourceId, userID);
                log.Debug("Get new MPP Event from MPP, last objectID: " + lastMPPEvent.SourceId + " new event count: " + mppEvents.Count);


                if (lastMPPEvent.SourceId == 0 && mppEvents.Count > 0)
                {
                    // the DB was empty, probably this is the first run.
                    // save the last event as ignored just for reference.
                    var MPPStationServerEvent = mppEvents.OrderByDescending(e => e.ObjectId).FirstOrDefault();
                    if (MPPStationServerEvent != null)
                    {
                        MPPStationServerEvent.State = WorkFlowJobState.Ignored;
                        mppEvents = new List<MPPStationServerEvent>();
                        mppEvents.Add(MPPStationServerEvent);
                        log.Debug("DB was empty, this might be the first run. only save the last MPP event as ignored for referencing later.");
                    }
                }

                ulong ignored = 0;
                foreach (MPPStationServerEvent mppEvent in mppEvents) {
                    // create workflowjobs
                    if (mppEvent.Type == EventType.ContentAgreementUpdated || mppEvent.Type == EventType.ContentDeleted || mppEvent.Type == EventType.ContentAgreementCreated)
                        continue;
                    WorkFlowJob wfj = new WorkFlowJob();
                    wfj.SourceId = mppEvent.ObjectId;
                    wfj.Type = mppEvent.Type;
                    wfj.Message = mppEvent;
                    wfj.MessageType = mppEvent.GetType().FullName;
                    wfj.Created = DateTime.UtcNow;
                    wfj.LastModified = DateTime.UtcNow;
                    wfj.NotUntil = DateTime.UtcNow;
                    wfj.State = mppEvent.State;
                    
                    // save jobs
                    dbwrapper.AddWorkFlowJob(wfj);

                    // save the mpp events in DB
                    //dbwrapper.AddMPPStationServerEvent(mppEvent);
                }
                log.Debug(mppEvents.Count + " new mpp events saved to DB and " + ignored + " ignored");
            }
            catch (Exception ex) {
                log.Error("Failed to sync MPP Events:" + ex.Message, ex);
            }
            log.Debug("DoExecute End");
        }
    }
}
