using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Dispatcher;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database.SQLite;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class ProcessEventTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static List<ContentData> ignorelist = new List<ContentData>();
        private MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");
            try
            {
                // fetch events from DB and processing them.
                DispatchEventsFromDB();
            }
            catch (Exception ex)
            {
                log.Error("Failed to process MPP Events:" + ex.Message, ex);
            }

            //ConaxWorkShopFix();
            log.Debug("DoExecute End");
        }

        private void DispatchEventsFromDB()
        {
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
            IDBWrapper dbwrapper = DBManager.Instance;
            BaseMPPEventDispatcher dispatcher = Activator.CreateInstance(System.Type.GetType(systemConfig.MPPEventDispatcher), new object[] { dbwrapper, this.TaskConfig }) as BaseMPPEventDispatcher;
            dispatcher.DispatchMPPEvent();
        }
    }
}
