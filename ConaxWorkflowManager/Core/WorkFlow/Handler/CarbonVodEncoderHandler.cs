using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Carbon;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class CarbonVodEncoderHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private String PlayoutFileDirectory = "";

        CarbonVodEncoderJob encoderJob = new CarbonVodEncoderJob();
        CarbonVodEncoderJob trailerEncoderJob = new CarbonVodEncoderJob();

        public override RequestResult OnProcess(RequestParameters parameters)
        {

            try
            {
                log.Info("<------------------------------------ Starting encoding -------------------------------------------------->");
                var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
                ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;


                JobState jobState = ConaxIntegrationHelper.GetCurrentJobState(parameters, false);
                JobState trailerJobState = ConaxIntegrationHelper.GetCurrentJobState(parameters, true);

                log.Info("Starting new job");

                encoderJob.Content = content;
                encoderJob.TrailerJob = false;
                encoderJob.jobState = jobState;
                encoderJob.parameters = parameters;
                MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.CarbonEncoder.Job job = encoderJob.StartJob();
                String jobID = job.Guid.ToString();
                log.Info("job done for " + jobID);
                
                bool encodeForTrailers = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == true) != null;
                if (encodeForTrailers)
                {
                    trailerEncoderJob.TrailerJob = true;
                    trailerEncoderJob.Content = content;
                    trailerEncoderJob.jobState = trailerJobState;
                    trailerEncoderJob.parameters = parameters;
                    log.Debug("Starting new trailer job");
                    MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.CarbonEncoder.Job trailerJob = trailerEncoderJob.StartJob();
                    String trailerJobID = trailerJob.Guid.ToString();
                    log.Debug("trailer job done with ID = " + trailerJobID);
                }
              
                log.Debug("Updating in mpp");
                MPPIntegrationServicesWrapper wrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
                wrapper.UpdateAssets(content);
                log.Debug("Update done!");
                try
                {
                    encoderJob.DeleteCopiedFile();
                    if (encodeForTrailers)
                        trailerEncoderJob.DeleteCopiedFile();
                }
                catch (Exception ex)
                {
                    log.Warn("Error when deleting copied file");
                }


            }
            catch (Exception ex)
            {
                log.Error("Something went wrong when handeling encoding", ex);

                encoderJob.DeleteCopiedFile();
                trailerEncoderJob.DeleteCopiedFile();
                log.Debug("Removed copied file");
                return new RequestResult(RequestResultState.Exception, ex);
            }
            log.Debug("<---------------------- encoding done ------------------------->");

            return new RequestResult(RequestResultState.Successful);
        }


        public override void OnChainFailed(RequestParameters parameters)
        {
            log.Debug("OnChainFailed");
            try
            {
                encoderJob.DeletePlayoutFolder();
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong deleting files from playoutDirectory", ex);
            }

        }


    }
}
