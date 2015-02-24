using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Policy;
using log4net;
using System.Reflection;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using System.Threading;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class ElementalVodEncoderHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

       

        private String PlayoutFileDirectory = "";

        ElementalJobHandler encoderJob = new ElementalJobHandler();
        ElementalJobHandler trailerEncoderJob = new ElementalJobHandler();

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Info("OnProcess");
            try
            {
                log.Info("<------------------------------------ Starting encoding -------------------------------------------------->");
                var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
                ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

                String existingJobID = ConaxIntegrationHelper.CheckForExistingJobID(parameters, false);
                String existingTrailerJobID = ConaxIntegrationHelper.CheckForExistingJobID(parameters, true);

                log.Debug("Existing jobID= " + existingJobID + " and Existing trailerJobID= " + existingTrailerJobID);

                encoderJob.TrailerJob = false;
                encoderJob.Content = content;

                String stateObject = "";
                //if (String.IsNullOrEmpty(existingJobID))
                //{
                //    log.Debug("Starting new job");
                //    String jobID = encoderJob.StartEncoding();
                //    stateObject = "jobID=" + jobID;
                //    parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket = stateObject;
                //    log.Debug("setting basket to " + stateObject);
                //    log.Debug("job started with ID = " + jobID);
                //}
                //else
                //{
                //    encoderJob.JobID = existingJobID;
                //    log.Debug("Using existing jobID= " + existingJobID);
                //}

                bool encodeForTrailers = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == true) != null;
                if (encodeForTrailers)
                {
                    trailerEncoderJob.TrailerJob = true;
                    trailerEncoderJob.Content = content;
                    //if (String.IsNullOrEmpty(existingTrailerJobID))
                    //{
                    //    log.Debug("Starting new trailer job");
                    //    String trailerJobID = trailerEncoderJob.StartEncoding();

                    //    stateObject += ";trailerJobID=" + trailerJobID;
                    //    log.Debug("setting basket to " + stateObject);
                    //    parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket = trailerJobID;
                    //    log.Debug("trailer job started with ID = " + trailerJobID);
                    //}
                    //else
                    //{
                    //    trailerEncoderJob.JobID = existingTrailerJobID;
                    //    log.Debug("Using existing trailerJobID = " + existingTrailerJobID);
                    //}
                }
                if (encoderJob.CheckJobStatus())
                {
                    log.Info("encoder job finished!");
                    encoderJob.UpdateAssets();
                }
                else
                {
                    log.Error("Something went wrong encoding content with name = " + content.Name + " and contentID = " + content.ID);
                    throw new Exception("Something went wrong encoding content with name = " + content.Name + " and contentID = " + content.ID);
                }
                if (encodeForTrailers)
                {
                    if (trailerEncoderJob.CheckJobStatus())
                    {
                        log.Info("Trailer encoder job finished!");
                        trailerEncoderJob.UpdateAssets();
                    }
                    else
                    {
                        log.Error("Something went wrong encoding trailer for content with name = " + content.Name + " and contentID = " + content.ID);
                        throw new Exception("Something went wrong encoding trailer for content with name = " + content.Name + " and contentID = " + content.ID);
                    }
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
            log.Info("<---------------------- encoding fully done ------------------------->");

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
