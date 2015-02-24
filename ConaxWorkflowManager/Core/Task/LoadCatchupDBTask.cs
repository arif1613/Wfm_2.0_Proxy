using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup;
using System.Threading;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class LoadCatchupDBTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        BaseEncoderCatchupHandler smoothHandler = null;
        BaseEncoderCatchupHandler hlshHandler = null;

        public LoadCatchupDBTask() {
            var managerConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager");
            smoothHandler = Activator.CreateInstance(System.Type.GetType(managerConfig.GetConfigParam("SmoothCatchUpHandler"))) as BaseEncoderCatchupHandler;
            hlshHandler = Activator.CreateInstance(System.Type.GetType(managerConfig.GetConfigParam("HLSCatchUpHandler"))) as BaseEncoderCatchupHandler;
        }

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");
            
            DateTime dtStart = DateTime.Now;
            List<String> channelsToProces = null;

            if (this.TaskConfig.ConfigParams.ContainsKey("ChannelsToProcess") &&
                !String.IsNullOrEmpty(this.TaskConfig.GetConfigParam("ChannelsToProcess")))
            {
                channelsToProces = new List<String>();
                channelsToProces.AddRange(this.TaskConfig.GetConfigParam("ChannelsToProcess").Split(",".ToArray(), StringSplitOptions.RemoveEmptyEntries));
                log.Debug("Channels to process for this task are " + this.TaskConfig.GetConfigParam("ChannelsToProcess"));
            }
            else {
                log.Debug("All Channels will be processed for this task.");
            }

            try {
                smoothHandler.ProcessArchive(channelsToProces);
            } catch (Exception ex) {
                log.Error("Failed to execute load SS manifest.", ex);
            }

            try {
                hlshHandler.ProcessArchive(channelsToProces);
            } catch(Exception ex) {
                log.Error("Failed to execute load HLS manfiest.", ex);
            }

            log.Debug("DoExecute End " + (DateTime.Now - dtStart).TotalMilliseconds);
        }
    }
}
