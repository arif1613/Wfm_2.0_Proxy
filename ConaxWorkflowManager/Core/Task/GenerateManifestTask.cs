using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.IO;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database.SQLite;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class GenerateManifestTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        //BaseEncoderCatchupHandler smoothHandler = null;
        //BaseEncoderCatchupHandler hlshHandler = null;
        BaseEncoderCatchupHandler handler = null;

        public GenerateManifestTask() {
            //var managerConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager");
            //smoothHandler = Activator.CreateInstance(System.Type.GetType(managerConfig.GetConfigParam("SmoothCatchUpHandler"))) as BaseEncoderCatchupHandler;
            //hlshHandler = Activator.CreateInstance(System.Type.GetType(managerConfig.GetConfigParam("HLSCatchUpHandler"))) as BaseEncoderCatchupHandler;            
        }

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");
            handler = Activator.CreateInstance(System.Type.GetType(this.TaskConfig.GetConfigParam("CatchUpHandler"))) as BaseEncoderCatchupHandler;
            List<String> channelsToProces = null;

            //CubiTVMiddlewareManager.Instance(513697793).CreateNPVRRecording("6875");

            if (this.TaskConfig.ConfigParams.ContainsKey("ChannelsToProcess") &&
                !String.IsNullOrEmpty(this.TaskConfig.GetConfigParam("ChannelsToProcess")))
            {
                channelsToProces = new List<String>();
                channelsToProces.AddRange(this.TaskConfig.GetConfigParam("ChannelsToProcess").Split(",".ToArray(), StringSplitOptions.RemoveEmptyEntries));
                log.Debug("Channels to process for this task are " + this.TaskConfig.GetConfigParam("ChannelsToProcess") + " with handler " + this.TaskConfig.GetConfigParam("CatchUpHandler"));
            }
            else
            {
                log.Debug("All Channels will be processed for this task. with handler " + this.TaskConfig.GetConfigParam("CatchUpHandler"));
            }

            try
            {
                handler.GenerateManifest(channelsToProces);
            }
            catch (Exception ex)
            {
                log.Error("Failed to execute manifest generation using handler " + this.TaskConfig.GetConfigParam("CatchUpHandler"), ex);
            }
            //try {
            //    smoothHandler.GenerateManifest(channelsToProces);
            //}
            //catch (Exception ex) {
            //    log.Error("Failed to execute SS manifest generation.", ex);
            //}

            //try {
            //    hlshHandler.GenerateManifest(channelsToProces);
            //}
            //catch (Exception ex) {
            //    log.Error("Failed to execute HLS manifest generation.", ex);
            //}

            log.Debug("DoExecute End");
        }
    }
}
