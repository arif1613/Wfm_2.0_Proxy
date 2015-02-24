using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class RegisterCatchUpContentInCubiTVHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");
            ICubiTVMWServiceWrapper wrapper = CubiTVMiddlewareManager.Instance(parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0].ObjectID.Value);
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            // POST XMLTV
            log.Debug("CreateCatchUpContent");
            //wrapper.CreateCatchUpContent(content);
            
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            Int32 pollTime = Int32.Parse(systemConfig.GetConfigParam("PollCubiCatchUpCreatedInSec")) * 1000;
            // Check if Catchup content is created. since it a async job in Cubi.
            System.Threading.Thread.Sleep(pollTime);
            log.Debug("GetCatchUpContent");
            wrapper.GetCatchUpContent(content.ExternalID);

            // update cubit content with assets
            log.Debug("UpdateCatchUpContent");
            //wrapper.UpdateCatchUpContent(content);

            return new RequestResult(RequestResultState.Successful);
        }
    }
}
