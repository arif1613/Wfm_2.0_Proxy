using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.IO;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow
{

    public class AddLiveContentFlow : BaseFlow
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /*
        public AddLiveContentFlow(TaskConfig contextConfig)
            : base(contextConfig) {}
        */
        public override RequestResult Process(RequestParameters requestParameters)
        {

            try
            {
                //RequestResult result = GCFMPPHandler.HandleRequest(requestParameters);
                RequestResult result = HandleRequest(requestParameters);
                if (result.State == RequestResultState.Failed ||
                    result.State == RequestResultState.Exception)
                {
                    try
                    {
                        RejectIngest(requestParameters);
                    }
                    catch (Exception moveEx)
                    {
                        log.Warn("Failed to move files to reject folder.", moveEx);
                    }
                    try
                    {
                        if (result.State == RequestResultState.Exception)
                        {
                            CommonUtil.SendFailedVODIngestNotification(requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, result.Ex);
                        }
                        else // state = failed
                        {
                            CommonUtil.SendFailedVODIngestNotification(requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, result.Message);
                        }
                    }
                    catch (Exception mailex)
                    {
                        log.Warn("Failed to send Notification.", mailex);
                    }
                }
                else if (result.State == RequestResultState.Successful)
                {
                    try
                    {
                        ProcessedIngest(requestParameters);
                    }
                    catch (Exception moveEx)
                    {
                        log.Warn("Failed to move files to proccessed folder.", moveEx);
                    }
                    try
                    {
                        CommonUtil.SendSuccessfulVODIngestNotification(requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content);
                    }
                    catch (Exception mailex)
                    {
                        log.Warn("Failed to send Notification.", mailex);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                try
                {
                    RejectIngest(requestParameters);
                }
                catch (Exception moveEx)
                {
                    log.Warn("Failed to move files to reject folder.", moveEx);
                }
                try
                {
                    CommonUtil.SendFailedVODIngestNotification(requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, ex);
                }
                catch (Exception mailex)
                {
                    log.Warn("Failed to send Notification.", mailex);
                }
                throw;
            }
            // Send notification (mail)
        }

        private void ProcessedIngest(RequestParameters requestParameters)
        {

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String processedDir = systemConfig.GetConfigParam("FileIngestProcessedDirectory");
            MoveIngestFromWorkTo(requestParameters, processedDir);
        }

        private void RejectIngest(RequestParameters requestParameters)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String rejectDir = systemConfig.GetConfigParam("FileIngestRejectDirectory");
            MoveIngestFromWorkTo(requestParameters, rejectDir);
        }

        private void MoveIngestFromWorkTo(RequestParameters requestParameters, String destDir)
        {
            throw new NotImplementedException();
        }
    }
}
