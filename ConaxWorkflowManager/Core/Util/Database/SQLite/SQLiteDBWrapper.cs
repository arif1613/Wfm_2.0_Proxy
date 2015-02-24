using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Data;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.System;
using log4net;
using System.Reflection;
using System.Threading;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database.SQLite
{
    public class SQLiteDBWrapper : IDBWrapper
    {
        private static volatile SQLiteDatabase db = null;// new SQLiteDatabase(Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager").GetConfigParam("DBSource"));
        private static volatile Dictionary<UInt64, SQLiteDatabase> catchupdbs = new Dictionary<UInt64, SQLiteDatabase>(); //new SQLiteDatabase(Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager").GetConfigParam("CatchUpDBSource"));
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SQLiteDBWrapper() {
            try
            {
                var config = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager");
                db = new SQLiteDatabase(config.GetConfigParam("DBSource"));
            }
            catch (Exception ex) {
                log.Error("Failed to initiate SQLiteDBWrapper.", ex);
                throw;
            }
        }

        public DateTime LastOccurredDateForTask(String taskName, ulong serviceObjectId)
        {
            //try
            //{
            //    String query = "SELECT MAX(Occurred) AS Occurred FROM CatchupAndNPVRTaskDates WHERE TaskName = '" + taskName + "' AND ServiceObjectId = " + serviceObjectId.ToString();

            //    DataTable events = db.GetDataTable(query);
            //    DateTime lastExecuteDate = DateTime.MinValue;
            //    if (events.Rows.Count > 0)
            //    {
            //        DataRow r = events.Rows[0];
            //        if (!String.IsNullOrWhiteSpace(r["Occurred"].ToString()))
            //            lastExecuteDate = DateTime.Parse(r["Occurred"].ToString());
            //    }
            //    return lastExecuteDate;
            //}
            //catch (Exception ex)
            //{
            //    throw;
            //}
            return DateTime.UtcNow;
        }

        public void AddOccuredDateForTask(String taskName, ulong serviceObjectId, DateTime executeDate)
        {
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("TaskName", taskName);
            String dateString = executeDate.ToString("yyyy-MM-dd HH:mm:ss");
            data.Add("Occurred", dateString);
            data.Add("ServiceObjectId", serviceObjectId.ToString());
            db.Insert("CatchupAndNPVRTaskDates", data);
        }

        public Int32 UpdateOccuredDateForTask(String taskName, ulong serviceObjectId, DateTime executeDate) {
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("Occurred", executeDate.ToString("yyyy-MM-dd HH:mm:ss"));

            return db.Update("CatchupAndNPVRTaskDates", data, "TaskName = '" + taskName + "' and ServiceObjectId = " + serviceObjectId.ToString());
        }

      

        #region WorkFlowProcess

        public void UpdateWorkFlowProcess(WorkFlowProcess workFlowProcess) {

            Stopwatch sw = Stopwatch.StartNew();
            // Serialize workFlowProcess to XML for updaet
            String xmlData = CommonUtil.SerializeObject(workFlowProcess.WorkFlowParameters);

            // add to DB
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("WorkFlowJobId", workFlowProcess.WorkFlowJobId.ToString());
            data.Add("MethodName", workFlowProcess.MethodName);
            data.Add("Occurred", workFlowProcess.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add("WorkFlowProcessState", workFlowProcess.State.ToString("G"));
            data.Add("WorkFlowParameters", xmlData);
            data.Add("Message", workFlowProcess.Message);

            db.Update("WorkFlowProcess", data, "Id = " + workFlowProcess.Id.ToString());
            sw.Stop();
            if (sw.ElapsedMilliseconds > 3000)
            {
                log.Warn("Updating UpdateWorkFlowProcess for workflowid " + workFlowProcess.WorkFlowJobId + " took " + sw.ElapsedMilliseconds + " ms");
            }
        }

        public void AddWorkFlowProcess(WorkFlowProcess workFlowProcess)
        {
            String xmlData = "";
            for (int x = 0; x < 5; x++) {
                xmlData = InsertWorkFlowProcess(workFlowProcess);
                // update ID
                List<WorkFlowProcess> WorkFlowProcesses = GetWorkFlowProcessesByWorkFlowJobId(workFlowProcess.WorkFlowJobId);
                var lastWP = WorkFlowProcesses.OrderByDescending(wp => wp.Id).FirstOrDefault();
                if (lastWP != null) {
                    // for some reason the insert sometime never get done, have a 3 reties loop and throw exceptino is still can't be done.
                    workFlowProcess.Id = lastWP.Id;
                    return;
                }
                Thread.Sleep(20);
            }
            throw new Exception("Failed to Add workflow process to SQLITE DB for workflow id " + workFlowProcess.WorkFlowJobId + " " + workFlowProcess.MethodName + " " + workFlowProcess.State.ToString("G") + " " + xmlData);
        }

        private String InsertWorkFlowProcess(WorkFlowProcess workFlowProcess)
        {
            Stopwatch sw = Stopwatch.StartNew();
            // Serialize workFlowProcess to XML for insert
            String xmlData = CommonUtil.SerializeObject(workFlowProcess.WorkFlowParameters);

            // add to DB
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("WorkFlowJobId", workFlowProcess.WorkFlowJobId.ToString());
            data.Add("MethodName", workFlowProcess.MethodName);
            data.Add("Occurred", workFlowProcess.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add("WorkFlowProcessState", workFlowProcess.State.ToString("G"));
            data.Add("WorkFlowParameters", xmlData);
            data.Add("Message", workFlowProcess.Message);

            db.Insert("WorkFlowProcess", data);
            sw.Stop();
            if (sw.ElapsedMilliseconds > 3000)
            {
                log.Warn("Adding AddWorkFlowProcess for workflowid " + workFlowProcess.WorkFlowJobId + " took " + sw.ElapsedMilliseconds + " ms");
            }
            return xmlData;
        }

        public List<WorkFlowProcess> GetWorkFlowProcessesByWorkFlowJobId(UInt64 workFlowJobId)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                List<WorkFlowProcess> workFlowProcesses = new List<WorkFlowProcess>();
                String query = "SELECT * FROM WorkFlowProcess WHERE WorkFlowJobId = " + workFlowJobId.ToString() + " ORDER BY Id DESC;";
                //DataTable processes = db.GetDataTable(query);

                //foreach (DataRow r in processes.Rows)
                //{
                //    WorkFlowProcess workFlowProcess = new WorkFlowProcess();

                //    workFlowProcess.Id = UInt64.Parse(r["Id"].ToString());
                //    workFlowProcess.WorkFlowJobId = UInt64.Parse(r["WorkFlowJobId"].ToString());
                //    workFlowProcess.TimeStamp = (DateTime)r["Occurred"];
                //    workFlowProcess.State = (WorkFlowProcessState)Enum.Parse(typeof(WorkFlowProcessState), r["WorkFlowProcessState"].ToString(), true);
                //    workFlowProcess.MethodName = r["MethodName"].ToString();
                //    workFlowProcess.Message = r["Message"].ToString();

                //    if (!String.IsNullOrEmpty(r["WorkFlowParameters"].ToString()))
                //    {
                //        WorkFlowParameters workFlowParameters = null;
                //        XmlSerializer serializer = new XmlSerializer(typeof(WorkFlowParameters));
                //        XmlReader reader = XmlReader.Create(new StringReader(r["WorkFlowParameters"].ToString()));
                //        workFlowParameters = (WorkFlowParameters)serializer.Deserialize(reader);
                //        reader.Close();
                //        workFlowProcess.WorkFlowParameters = workFlowParameters;
                //    }

                //    workFlowProcesses.Add(workFlowProcess);
                //}
                sw.Stop();
                if (sw.ElapsedMilliseconds > 3000)
                {
                    log.Warn("Fetching GetWorkFlowProcessesByWorkFlowJobId from db took " + sw.ElapsedMilliseconds + " ms");
                }
                return workFlowProcesses;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Int32 CountWorkFlowTries(UInt64 eventObjectId)
        {

            Stopwatch sw = Stopwatch.StartNew();
            String sqlStr = "SELECT COUNT(MethodName) FROM WorkFlowProcess WHERE EventObjectId = " + eventObjectId + " GROUP BY MethodName;";
            //String result =  db.ExecuteScalar(sqlStr);

            //if (String.IsNullOrEmpty(result))
            //    return 0;
            sw.Stop();
            if (sw.ElapsedMilliseconds > 3000)
            {
                log.Warn("fetching tries for objectId " + eventObjectId +  " took " + sw.ElapsedMilliseconds + " ms");
            }
            //return Int32.Parse(result);
            return 1;
        }
        /*
        private String SerializeObject(Object obj) {

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.OmitXmlDeclaration = true;
            XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
            xmlSerializerNamespaces.Add("", "");
            XmlSerializer ser = new XmlSerializer(obj.GetType());
            StringWriter stringWriter = new StringWriter();
            XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);
            ser.Serialize(xmlWriter, obj, xmlSerializerNamespaces);
            String xmlData = stringWriter.ToString();
            return xmlData;
        }
        */
        public void DeleteWorkFlowProcessesByWorkFlowJobId(UInt64 workFlowJobId)
        {
            Stopwatch sw = Stopwatch.StartNew();
            db.Delete("WorkFlowProcess", "WorkFlowJobId = " + workFlowJobId.ToString());
            sw.Stop();
            if (sw.ElapsedMilliseconds > 3000)
            {
                log.Warn("DeleteWorkFlowProcessesByWorkFlowJobId for WorkFlowId " + workFlowJobId + " took " + sw.ElapsedMilliseconds + " ms");
            }
        }


        public int GetNumberOfTries(ulong workFlowJobId, String handlerName)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            //String query = "select count(*) as tries from WorkFlowProcess where Id = " + workFlowJobId.ToString() + " and MethodName='" + handlerName + "'";

            //String count = db.ExecuteScalar(query);
            //sw.Stop();
            //if (sw.ElapsedMilliseconds > 3000)
            //{
            //    log.Warn("GetNumberOfTries for WorkFlowId " + workFlowJobId + "and handler " + handlerName + " took " + sw.ElapsedMilliseconds + " ms");
            //}
            //return int.Parse(count);
            return 1;
        }

        #endregion

        #region WorkFlowJob

        public WorkFlowJob GetLastMPPStationServerEvent()
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                WorkFlowJob workFlowJob = new WorkFlowJob();
                String query = "SELECT * FROM WorkFlowJob " +
                                "WHERE SourceId = (SELECT Max(SourceId) FROM WorkFlowJob);";
                //DataTable events = db.GetDataTable(query);

                //foreach (DataRow r in events.Rows)
                //{
                //    workFlowJob.Id = UInt64.Parse(r["Id"].ToString());
                //    workFlowJob.SourceId = UInt64.Parse(r["SourceId"].ToString());
                //    workFlowJob.Created = (DateTime)r["Created"];
                //    workFlowJob.NotUntil = (DateTime)r["NotUntil"];
                //    workFlowJob.Type = (EventType)Enum.Parse(typeof(EventType), r["Type"].ToString(), true);
                //    workFlowJob.State = (WorkFlowJobState)Enum.Parse(typeof(WorkFlowJobState), r["WorkFlowJobState"].ToString(), true);

                //    return workFlowJob;
                //}
                sw.Stop();
                if (sw.ElapsedMilliseconds > 3000)
                {
                    log.Warn("GetLastMPPStationServerEvent took " + sw.ElapsedMilliseconds + " ms");
                }
                return workFlowJob;
            }
            catch (Exception cex)
            {
                throw;
            }
        }

        public void AddWorkFlowJob(WorkFlowJob workFlowJob)
        {

            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                String message = CommonUtil.SerializeObject(workFlowJob.Message);

                Dictionary<String, String> data = new Dictionary<String, String>();
                data.Add("SourceId", workFlowJob.SourceId.ToString());
                data.Add("Type", workFlowJob.Type.ToString("G"));
                data.Add("Message", message);
                data.Add("MessageType", workFlowJob.MessageType);
                data.Add("Created", workFlowJob.Created.ToString("yyyy-MM-dd HH:mm:ss"));
                data.Add("NotUntil", workFlowJob.NotUntil.ToString("yyyy-MM-dd HH:mm:ss"));
                data.Add("workFlowJobState", workFlowJob.State.ToString("G")); 

                db.Insert("WorkFlowJob", data);
                sw.Stop();
                if (sw.ElapsedMilliseconds > 3000)
                {
                    log.Warn("AddWorkFlowJob took " + sw.ElapsedMilliseconds + " ms");
                }
            }
            catch (Exception cex)
            {
                throw;
            }
        }

        public List<WorkFlowJob> GetWorkFlowJobs(WorkFlowJobState state, List<EventType> eventTypes) {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                String typecon = "";
                if (eventTypes.Count > 0)
                {
                    foreach (EventType eventtype in eventTypes)
                    {
                        if (typecon.Length == 0)
                            typecon += "'" + eventtype.ToString("G") + "'";
                        else
                            typecon += ",'" + eventtype.ToString("G") + "'";
                    }
                    typecon = " Type IN (" + typecon + ") ";
                }

                List<WorkFlowJob> workFlowJobs = new List<WorkFlowJob>();
                String query = "SELECT * FROM WorkFlowJob WHERE workFlowJobState = '" + state.ToString("G") + "' ";
                       query += " AND NotUntil < '" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                if (!String.IsNullOrEmpty(typecon))
                    query += " AND " + typecon;
                query += " ORDER BY Id ASC;";

                //DataTable events = db.GetDataTable(query);

                //foreach (DataRow r in events.Rows)
                //{
                //    WorkFlowJob workFlowJob = new WorkFlowJob();

                //    workFlowJob.Id = UInt64.Parse(r["Id"].ToString());
                //    workFlowJob.SourceId = UInt64.Parse(r["SourceId"].ToString());
                //    workFlowJob.Type = (EventType)Enum.Parse(typeof(EventType), r["Type"].ToString(), true);
                //    workFlowJob.Created = (DateTime)r["Created"];
                //    workFlowJob.NotUntil = (DateTime)r["NotUntil"];
                //    workFlowJob.State = (WorkFlowJobState)Enum.Parse(typeof(WorkFlowJobState), r["WorkFlowJobState"].ToString(), true);
                //    workFlowJob.MessageType = r["MessageType"].ToString();

                //    XmlSerializer serializer = new XmlSerializer(Type.GetType(workFlowJob.MessageType));
                //    XmlReader reader = XmlReader.Create(new StringReader(r["Message"].ToString()));
                //    workFlowJob.Message = serializer.Deserialize(reader);
                //    reader.Close();

                //    workFlowJobs.Add(workFlowJob);
                //}
                sw.Stop();
                if (sw.ElapsedMilliseconds > 3000)
                {
                    log.Warn("GetWorkFlowJobs for WorkFlowJobState " + state + " took " + sw.ElapsedMilliseconds + " ms");
                }
                return workFlowJobs;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void UpdateWorkFlowJob(WorkFlowJob workFlowJob)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Dictionary<String, String> data = new Dictionary<String, String>();
            //data.Add("Occurred", mppEvent.Occurred.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add("WorkFlowJobState", workFlowJob.State.ToString("G"));

            db.Update("WorkFlowJob", data, "Id == " + workFlowJob.Id.Value.ToString());
            sw.Stop();
            if (sw.ElapsedMilliseconds > 3000)
            {
                log.Warn("UpdateWorkFlowJob for workFlowJob with id " + workFlowJob.Id + " took " + sw.ElapsedMilliseconds + " ms");
            }
        }

        public void UpdateWorkFlowJobNotUntil(ulong workFlowJobID, DateTime newNotUntil)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Dictionary<String, String> data = new Dictionary<String, String>();
            //data.Add("Occurred", mppEvent.Occurred.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add("NotUntil", newNotUntil.ToString("yyyy-MM-dd HH:mm:ss"));

            db.Update("WorkFlowJob", data, "Id == " + workFlowJobID.ToString());
            sw.Stop();
            if (sw.ElapsedMilliseconds > 3000)
            {
                log.Warn("UpdateWorkFlowJobNotUntil  took " + sw.ElapsedMilliseconds + " ms");
            }
        }

        #endregion

        #region MPPStationServerEvent
        /*
        public MPPStationServerEvent GetLastMPPStationServerEvent()
        {
            try
            {
                MPPStationServerEvent mppEvent = new MPPStationServerEvent();
                String query = "SELECT * FROM MPPStationServerEvent " +                                
                                "WHERE ObjectId = (SELECT Max(ObjectId) FROM MPPStationServerEvent);";
                DataTable events = db.GetDataTable(query);

                foreach (DataRow r in events.Rows)
                {
                    mppEvent.ObjectId = UInt64.Parse(r["ObjectId"].ToString());
                    mppEvent.Occurred = (DateTime)r["Occurred"];
                    mppEvent.Type = (EventType)Enum.Parse(typeof(EventType), r["Type"].ToString(), true);
                    mppEvent.RelatedPersistentObjectId = UInt64.Parse(r["RelatedPersistentObjectId"].ToString());
                    mppEvent.RelatedPersistentClassName = r["RelatedPersistentClassName"].ToString();
                    mppEvent.UserId = UInt64.Parse(r["UserId"].ToString());
                    mppEvent.State = (MPPEventProcessState)Enum.Parse(typeof(MPPEventProcessState), r["MPPEventProcessState"].ToString(), true);

                    return mppEvent;
                }
                return mppEvent;
            }
            catch (Exception cex)
            {
                throw;
            }
        }
        */
        public void AddMPPStationServerEvent(MPPStationServerEvent mppEvent)
        {
            try
            {
                Dictionary<String, String> data = new Dictionary<String, String>();
                data.Add("ObjectId", mppEvent.ObjectId.ToString());
                data.Add("Occurred", mppEvent.Occurred.ToString("yyyy-MM-dd HH:mm:ss"));
                data.Add("Type", mppEvent.Type.ToString("G"));
                data.Add("RelatedPersistentObjectId", mppEvent.RelatedPersistentObjectId.ToString());
                data.Add("RelatedPersistentClassName", mppEvent.RelatedPersistentClassName);
                data.Add("UserId", mppEvent.UserId.ToString());
                data.Add("MPPEventProcessState", mppEvent.State.ToString("G"));

                db.Insert("MPPStationServerEvent", data);
            }
            catch (Exception cex)
            {
                throw;
            }
        }

        public List<MPPStationServerEvent> GetMPPStationServerEvents(MPPEventProcessState state, List<EventType> eventTypes)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                String typecon = "";
                if (eventTypes.Count > 0)
                {
                    foreach (EventType eventtype in eventTypes)
                    {
                        if (typecon.Length == 0)
                            typecon += "'" + eventtype.ToString("G") + "'";
                        else
                            typecon += ",'" + eventtype.ToString("G") + "'";
                    }
                    typecon = " Type IN (" + typecon + ") ";
                }

                List<MPPStationServerEvent> MPPStationServerEvents = new List<MPPStationServerEvent>();
                String query = "SELECT * FROM MPPStationServerEvent WHERE MPPEventProcessState = '" + state.ToString("G") + "' ";
                if (!String.IsNullOrEmpty(typecon))
                    query += " AND " + typecon;
                query += " ORDER BY ObjectId DESC;";

                //DataTable events = db.GetDataTable(query);

                //foreach (DataRow r in events.Rows)
                //{
                //    MPPStationServerEvent mppEvent = new MPPStationServerEvent();

                //    mppEvent.ObjectId = UInt64.Parse(r["ObjectId"].ToString());
                //    mppEvent.Occurred = (DateTime)r["Occurred"];
                //    mppEvent.Type = (EventType)Enum.Parse(typeof(EventType), r["Type"].ToString(), true);
                //    mppEvent.RelatedPersistentObjectId = UInt64.Parse(r["RelatedPersistentObjectId"].ToString());
                //    mppEvent.RelatedPersistentClassName = r["RelatedPersistentClassName"].ToString();
                //    mppEvent.UserId = UInt64.Parse(r["UserId"].ToString());
                //    mppEvent.State = (WorkFlowJobState)Enum.Parse(typeof(WorkFlowJobState), r["MPPEventProcessState"].ToString(), true);

                //    MPPStationServerEvents.Add(mppEvent);
                //}
                sw.Stop();
                if (sw.ElapsedMilliseconds > 3000)
                {
                    log.Warn("GetMPPStationServerEvents  took " + sw.ElapsedMilliseconds + " ms");
                }
                return MPPStationServerEvents;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public List<MPPStationServerEvent> GetMPPStationServerEvents(MPPEventProcessState state)
        {
            return GetMPPStationServerEvents(state, new List<EventType>());
            /*
            try
            {
                List<MPPStationServerEvent> MPPStationServerEvents = new List<MPPStationServerEvent>();
                String query = "SELECT * FROM MPPStationServerEvent WHERE MPPEventProcessState = '" + state.ToString("G") + "' ORDER BY ObjectId DESC;";
                DataTable events = db.GetDataTable(query);


                foreach (DataRow r in events.Rows)
                {
                    MPPStationServerEvent mppEvent = new MPPStationServerEvent();

                    mppEvent.ObjectId = UInt64.Parse(r["ObjectId"].ToString());
                    mppEvent.Occurred = (DateTime)r["Occurred"];
                    mppEvent.Type = (EventType)Enum.Parse(typeof(EventType), r["Type"].ToString(), true);
                    mppEvent.RelatedPersistentObjectId = UInt64.Parse(r["RelatedPersistentObjectId"].ToString());
                    mppEvent.RelatedPersistentClassName = r["RelatedPersistentClassName"].ToString();
                    mppEvent.UserId = UInt64.Parse(r["UserId"].ToString());
                    mppEvent.State = (MPPEventProcessState)Enum.Parse(typeof(MPPEventProcessState), r["MPPEventProcessState"].ToString(), true);

                    MPPStationServerEvents.Add(mppEvent);
                }

                return MPPStationServerEvents;
            }
            catch (Exception ex)
            {
                throw;
            }
            */
        }

        public void UpdateMPPStationServerEvent(MPPStationServerEvent mppEvent)
        {

            Stopwatch sw = Stopwatch.StartNew();
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("Occurred", mppEvent.Occurred.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add("Type", mppEvent.Type.ToString("G"));
            data.Add("RelatedPersistentObjectId", mppEvent.RelatedPersistentObjectId.ToString());
            data.Add("RelatedPersistentClassName", mppEvent.RelatedPersistentClassName);
            data.Add("UserId", mppEvent.UserId.ToString());
            data.Add("MPPEventProcessState", mppEvent.State.ToString("G"));

            db.Update("MPPStationServerEvent", data, "ObjectId == " + mppEvent.ObjectId.ToString());
            sw.Stop();
            if (sw.ElapsedMilliseconds > 3000)
            {
                log.Warn("UpdateMPPStationServerEvent  took " + sw.ElapsedMilliseconds + " ms");
            }
        }

        #endregion

        #region CatchUp segments

        public SQLiteDatabase GetCatchupDB(UInt64 channelId)
        {
            SQLiteDatabase catchupdb = null;
            if (catchupdbs.TryGetValue(channelId, out catchupdb))
                return catchupdb;
            else
                throw new Exception("Catchup DB was not defined for Channel " + channelId);
        }

        public List<String> GetHLSManifestNames(ContentData content) {

            return GetHLSManifestNames(content, content.EventPeriodFrom.Value, content.EventPeriodTo.Value);
        }

        public List<String> GetHLSManifestNames(ContentData content, DateTime UTCStarttime, DateTime UTCEndtime) {
            String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;

            return GetHLSManifestNames(UInt64.Parse(channelId), UTCStarttime, UTCEndtime);
        }

        public List<String> GetHLSManifestNames(UInt64 channelId, DateTime UTCStarttime, DateTime UTCEndtime)
        {
            //String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);
            
            List<String> manifestNames = new List<String>();
            String query = "SELECT PlayListName FROM HLSSegments " +
                           "WHERE ChannelId = '" + channelId + "' " +
                           "  AND UTCStarttime <= '" + UTCEndtime.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                           "  AND UTCEndtime >= '" + UTCStarttime.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                           "GROUP BY PlayListName;";

            //DataTable events = catchupdb.GetDataTable(query);

            //foreach (DataRow r in events.Rows)
            //    manifestNames.Add(r["PlayListName"].ToString());
            
            return manifestNames;
        }

        public List<HLSChunk> GetHLSChunks(ContentData content, String manifestName) {
            return GetHLSChunks(content, manifestName, content.EventPeriodFrom.Value, content.EventPeriodTo.Value);
        }

        public List<HLSChunk> GetHLSChunks(ContentData content, String manifestName, DateTime UTCStarttime, DateTime UTCEndtime) {

            String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals("ChannelId", StringComparison.OrdinalIgnoreCase)).Value;
            return GetHLSChunks(UInt64.Parse(channelId), manifestName, UTCStarttime, UTCEndtime);
        }

        public List<HLSChunk> GetHLSChunks(UInt64 channelId, String manifestName, DateTime UTCStarttime, DateTime UTCEndtime)
        {
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            List<HLSChunk> HLSChunks = new List<HLSChunk>();
            String query = "SELECT * FROM HLSSegments " +
                           "WHERE ChannelId = '" + channelId + "' " +
                           "  AND PlayListName = '" + manifestName + "' " +
                           "  AND UTCStarttime <= '" + UTCEndtime.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                           "  AND UTCEndtime >= '" + UTCStarttime.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                           "ORDER BY UTCStarttime;";

            //DataTable events = catchupdb.GetDataTable(query);

            //foreach (DataRow r in events.Rows)
            //{
            //    HLSChunk hlsChunk = new HLSChunk();

            //    hlsChunk.ChannelId = UInt64.Parse(r["ChannelId"].ToString());
            //    hlsChunk.PlayListName = r["PlayListName"].ToString();
            //    hlsChunk.URI = r["URI"].ToString();
            //    hlsChunk.UTCStarttime = (DateTime)r["UTCStarttime"];
            //    hlsChunk.UTCEndtime = (DateTime)r["UTCEndtime"];
            //    hlsChunk.EXTXVERSION = Int32.Parse(r["EXTXVERSION"].ToString());
            //    hlsChunk.EXTXTARGETDURATION = Int32.Parse(r["EXTXTARGETDURATION"].ToString());
            //    hlsChunk.EXTXMEDIASEQUENCE = Int32.Parse(r["EXTXMEDIASEQUENCE"].ToString());
            //    hlsChunk.EXTXKEY = r["EXTXKEY"].ToString();
            //    hlsChunk.EXTINF = r["EXTINF"].ToString();

            //    HLSChunks.Add(hlsChunk); ;
            //}
            return HLSChunks;
        }

        public List<HLSChunk> GetHLSChunks(UInt64 channelId, DateTime fromDate)
        {
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            List<HLSChunk> HLSChunks = new List<HLSChunk>();
            String query = "SELECT * FROM HLSSegments " +
                           "WHERE UTCEndtime < '" + fromDate.ToString("yyyy-MM-dd HH:mm:ss") + "'" +
                           "  AND ChannelId = '" + channelId + "';";

            //DataTable events = catchupdb.GetDataTable(query);

            //foreach (DataRow r in events.Rows)
            //{
            //    HLSChunk hlsChunk = new HLSChunk();

            //    hlsChunk.ChannelId = UInt64.Parse(r["ChannelId"].ToString());
            //    hlsChunk.PlayListName = r["PlayListName"].ToString();
            //    hlsChunk.URI = r["URI"].ToString();
            //    hlsChunk.UTCStarttime = (DateTime)r["UTCStarttime"];
            //    hlsChunk.UTCEndtime = (DateTime)r["UTCEndtime"];
            //    hlsChunk.EXTXVERSION = Int32.Parse(r["EXTXVERSION"].ToString());
            //    hlsChunk.EXTXTARGETDURATION = Int32.Parse(r["EXTXTARGETDURATION"].ToString());
            //    hlsChunk.EXTXMEDIASEQUENCE = Int32.Parse(r["EXTXMEDIASEQUENCE"].ToString());
            //    hlsChunk.EXTXKEY = r["EXTXKEY"].ToString();
            //    hlsChunk.EXTINF = r["EXTINF"].ToString();

            //    HLSChunks.Add(hlsChunk); ;
            //}
            return HLSChunks;
        }

        public HLSChunk GetLastHLSChunks(UInt64 channelId, String PlayListName)
        {
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            HLSChunk HLSChunk = new HLSChunk();
            String query = "SELECT * FROM HLSSegments " +
                           "WHERE ChannelId = '" + channelId + "' " +
                           "  AND PlayListName = '" + PlayListName + "' " + 
                           "ORDER BY UTCStarttime " + 
                           "LIMIT 1;";

            //DataTable events = catchupdb.GetDataTable(query);

            //foreach (DataRow r in events.Rows)
            //{
            //    HLSChunk hlsChunk = new HLSChunk();

            //    hlsChunk.ChannelId = UInt64.Parse(r["ChannelId"].ToString());
            //    hlsChunk.PlayListName = r["PlayListName"].ToString();
            //    hlsChunk.URI = r["URI"].ToString();
            //    hlsChunk.UTCStarttime = (DateTime)r["UTCStarttime"];
            //    hlsChunk.UTCEndtime = (DateTime)r["UTCEndtime"];
            //    hlsChunk.EXTXVERSION = Int32.Parse(r["EXTXVERSION"].ToString());
            //    hlsChunk.EXTXTARGETDURATION = Int32.Parse(r["EXTXTARGETDURATION"].ToString());
            //    hlsChunk.EXTXMEDIASEQUENCE = Int32.Parse(r["EXTXMEDIASEQUENCE"].ToString());
            //    hlsChunk.EXTXKEY = r["EXTXKEY"].ToString();
            //    hlsChunk.EXTINF = r["EXTINF"].ToString();

            //    return hlsChunk;
            //}
            return null;
        }

        public void DeleteHLSChunks(UInt64 channelId, DateTime fromDate)
        {

            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            Boolean result = catchupdb.Delete("HLSSegments", "UTCEndtime < '" + fromDate.ToString("yyyy-MM-dd HH:mm:ss") + "'" +
                                                              "  AND ChannelId = '" + channelId + "'");
        }

        public List<AvailableDateTime> GetHLSAvailableDateTime(List<String> channelsToProces)
        {
            List<AvailableDateTime> availableDateTimes = new List<AvailableDateTime>();
            String tempQuery = "SELECT ChannelId, MAX(UTCEndtime) AS UTCEndtime FROM HLSSegments " +
                           "WHERE ChannelId = {0} " +
                           "GROUP BY ChannelId;";

            foreach (KeyValuePair<UInt64, SQLiteDatabase> kvp in catchupdbs)
            {
                if (channelsToProces != null) {
                    // run defiend channels only
                    if (!channelsToProces.Contains(kvp.Key.ToString()))
                        continue; // skip this foler. not set for this task
                }

                SQLiteDatabase catchupdb = kvp.Value;
                String query = String.Format(tempQuery, kvp.Key.ToString());
                //DataTable events = catchupdb.GetDataTable(query);

                //foreach (DataRow r in events.Rows)
                //{
                //    AvailableDateTime availableDateTime = new AvailableDateTime();
                //    availableDateTime.ChannelId = UInt64.Parse(r["ChannelId"].ToString());
                //    availableDateTime.UTCMaxEndtime = DateTime.Parse(r["UTCEndtime"].ToString());

                //    availableDateTimes.Add(availableDateTime); ;
                //}
            }
            return availableDateTimes;
        }

        public Boolean IsHLSSegmentExist(HLSChunk hlsChunk)
        {
            UInt64 channelId = hlsChunk.ChannelId;
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            String query = "SELECT COUNT(*) FROM HLSSegments WHERE ChannelId='" + channelId + "' AND URI='" + hlsChunk.URI + "';";
            //String count = catchupdb.ExecuteScalar(query);
            //if (UInt32.Parse(count) == 0)
            //    return false;
            //else
                return true;
        }

        public void AddHLSSegment(HLSChunk hlsChunk)
        {
            Thread.Sleep(25);
            UInt64 ChannelId = hlsChunk.ChannelId;
            SQLiteDatabase catchupdb = GetCatchupDB(ChannelId);

            // add to DB
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("ChannelId", ChannelId.ToString());
            data.Add("PlayListName", hlsChunk.PlayListName);
            data.Add("URI", hlsChunk.URI);
            data.Add("UTCStarttime", hlsChunk.UTCStarttime.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add("UTCEndtime", hlsChunk.UTCEndtime.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add("EXTXVERSION", hlsChunk.EXTXVERSION.ToString());
            data.Add("EXTXTARGETDURATION", hlsChunk.EXTXTARGETDURATION.ToString());
            data.Add("EXTXMEDIASEQUENCE", hlsChunk.EXTXMEDIASEQUENCE.ToString());
            data.Add("EXTXKEY", hlsChunk.EXTXKEY);
            data.Add("EXTINF", hlsChunk.EXTINF);

            catchupdb.Insert("HLSSegments", data);
        }

        public void AddHLSIndex(UInt64 channelId, String playListName, String EXTXSTREAMINF)
        {
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);
            // add to DB
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("ChannelId", channelId.ToString());
            data.Add("PlayListName", playListName);
            data.Add("EXTXSTREAMINF", EXTXSTREAMINF);
            
            catchupdb.Insert("HLSIndex", data);
        }

        public Int32 UpdateHLSIndex(UInt64 channelId, String playListName, String EXTXSTREAMINF)
        {
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);
            // add to DB
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("EXTXSTREAMINF", EXTXSTREAMINF);

            return catchupdb.Update("HLSIndex", data, " ChannelId='" + channelId + "' AND PlayListName='" + playListName + "'");
        }

        public String GetHLSIndexStream(UInt64 channelId, String manifestName)
        {
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            List<HLSChunk> HLSChunks = new List<HLSChunk>();
            String query = "SELECT * FROM HLSIndex " +
                           "WHERE ChannelId='" + channelId + "'" +
                           "  AND PlayListName='" + manifestName + "';";

            //DataTable events = catchupdb.GetDataTable(query);
            //String result = "";

            //foreach (DataRow r in events.Rows)
            //    result = r["EXTXSTREAMINF"].ToString();
    
            return null;
        }

        public void AddSSManifest(SSManifest ssManifest)
        {
            UInt64 channelId = ssManifest.ChannelId;
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            // add to DB
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("ChannelId", channelId.ToString());
            data.Add("ManifestFileName", ssManifest.ManifestFileName);
            data.Add("ManifestData", ssManifest.ManifestData);
            data.Add("UTCManifestStartTime", ssManifest.UTCManifestStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add("UTCStreamStartTime", ssManifest.UTCStreamStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Add("UTCStreamEndTime", ssManifest.UTCStreamEndTime.ToString("yyyy-MM-dd HH:mm:ss"));
            
            catchupdb.Insert("SSManifest", data);
        }

        public Boolean IsSSManifestExist(UInt64 channelId, String manifestFileName)
        {

            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            String query = "SELECT COUNT(*) FROM SSManifest WHERE ChannelId='" + channelId + "' AND ManifestFileName='" + manifestFileName + "';";
            //String count = catchupdb.ExecuteScalar(query);
            //if (UInt32.Parse(count) == 0)
            //    return false;
            //else
                return true;
        }

        public List<SSManifest> GetSSManifests(ContentData content)
        {
            String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals("ChannelId", StringComparison.OrdinalIgnoreCase)).Value;
            SQLiteDatabase catchupdb = GetCatchupDB(UInt64.Parse(channelId));

            List<SSManifest> ssManifests = new List<SSManifest>();
            String query = "SELECT * FROM SSManifest " +
                           "WHERE ChannelId = '" + channelId + "' " +
                           "  AND UTCStreamStartTime <= '" + content.EventPeriodTo.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                           "  AND UTCStreamEndTime >= '" + content.EventPeriodFrom.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                           "ORDER BY UTCStreamStartTime;";

            //DataTable events = catchupdb.GetDataTable(query);

            //foreach (DataRow r in events.Rows)
            //{
            //    SSManifest ssManifest = new SSManifest();

            //    ssManifest.ChannelId = UInt64.Parse(r["ChannelId"].ToString());
            //    ssManifest.ManifestFileName = r["ManifestFileName"].ToString();
            //    ssManifest.ManifestData = r["ManifestData"].ToString();
            //    ssManifest.UTCManifestStartTime = (DateTime)r["UTCManifestStartTime"];
            //    ssManifest.UTCStreamStartTime = (DateTime)r["UTCStreamStartTime"];
            //    ssManifest.UTCStreamEndTime = (DateTime)r["UTCStreamEndTime"];

            //    ssManifests.Add(ssManifest); ;
            //}
            return ssManifests;
        }

        public List<AvailableDateTime> GetSSAvailableDateTime()
        {
            List<AvailableDateTime> availableDateTimes = new List<AvailableDateTime>();
            String query = "SELECT CubiChannelId, MAX(UTCStreamEndTime) AS UTCEndtime FROM SSManifest " +
                           "GROUP BY CubiChannelId;";

            foreach (KeyValuePair<UInt64, SQLiteDatabase> kvp in catchupdbs)
            {
                SQLiteDatabase catchupdb = kvp.Value;
                //DataTable events = catchupdb.GetDataTable(query);

                //foreach (DataRow r in events.Rows)
                //{
                //    AvailableDateTime availableDateTime = new AvailableDateTime();
                //    availableDateTime.ChannelId = UInt64.Parse(r["ChannelId"].ToString());
                //    availableDateTime.UTCMaxEndtime = DateTime.Parse(r["UTCEndtime"].ToString());

                //    availableDateTimes.Add(availableDateTime); ;
                //}
            }
            return availableDateTimes;
        }

        public List<SSManifest> GetSSManifests(UInt64 channelId, DateTime fromDate)
        {
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            List<SSManifest> ssManifests = new List<SSManifest>();
            String query = "SELECT * FROM SSManifest " +
                           "WHERE UTCStreamEndTime < '" + fromDate.ToString("yyyy-MM-dd HH:mm:ss") + "'" +
                           "  AND ChannelId = '" + channelId + "';";
            
            //DataTable events = catchupdb.GetDataTable(query);

            //foreach (DataRow r in events.Rows)
            //{
            //    SSManifest ssManifest = new SSManifest();

            //    ssManifest.ChannelId = UInt64.Parse(r["ChannelId"].ToString());
            //    ssManifest.ManifestFileName = r["ManifestFileName"].ToString();
            //    ssManifest.ManifestData = r["ManifestData"].ToString();
            //    ssManifest.UTCManifestStartTime = (DateTime)r["UTCManifestStartTime"];
            //    ssManifest.UTCStreamStartTime = (DateTime)r["UTCStreamStartTime"];
            //    ssManifest.UTCStreamEndTime = (DateTime)r["UTCStreamEndTime"];

            //    ssManifests.Add(ssManifest); ;
            //}
            return ssManifests;
        }

        public void DeleteSSManifest(UInt64 channelId, DateTime fromDate)
        {
            SQLiteDatabase catchupdb = GetCatchupDB(channelId);

            Boolean result = catchupdb.Delete("SSManifest", "UTCStreamEndTime < '" + fromDate.ToString("yyyy-MM-dd HH:mm:ss") + "'" +
                                                              "  AND ChannelId = '" + channelId + "'");
        }

        #endregion

        public void CleanDB(DateTime toDate) {

            db.Delete("WorkFlowProcess", "Occurred < '" + toDate.ToString("yyyy-MM-dd HH:mm:ss") + "'");
            db.Delete("WorkFlowJob", "Created < '" + toDate.ToString("yyyy-MM-dd HH:mm:ss") + "'");
            String sqlStr = "DELETE FROM MPPStationServerEvent WHERE ObjectId < (SELECT MAX(ObjectId) FROM MPPStationServerEvent) " +
                                                                "AND Occurred < '" + toDate.ToString("yyyy-MM-dd HH:mm:ss") + "';";
           // db.ExecuteNonQuery(sqlStr);

            //db.ExecuteNonQuery("vacuum;");
            
        }

        public void ClearAndDefragDB()
        {
            throw new NotImplementedException();
        }

        //public void ClearAndDefragDB()
        //{
        //    db.ClearAndDefragDB();
        //}
    }
}
