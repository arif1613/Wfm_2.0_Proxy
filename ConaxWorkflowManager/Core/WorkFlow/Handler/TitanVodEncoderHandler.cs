using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{

    public class TitanVodEncoderHandler : ResponsibilityHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        List<TitanJobHandler> jobs = new List<TitanJobHandler>();

       // List<TitanJobHandler> trailerJobs = new List<TitanJobHandler>();

        public override RequestResult OnProcess(RequestParameters parameters)
        {

            //try
            //{
            log.Debug("<------------------------------------ Starting encoding -------------------------------------------------->");
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            List<AssetFormatType> assetTypes = ConaxIntegrationHelper.GetEncodingTypes(content, false);
            foreach (AssetFormatType assetFormat in assetTypes)
            {
                TitanJobHandler job = new TitanJobHandler();
                job.Content = content;
                job.TrailerJob = false;
                job.OutFormatType = assetFormat;
                job.StartEncoding();
                jobs.Add(job);
            }
            assetTypes = ConaxIntegrationHelper.GetEncodingTypes(content, true);
            foreach (AssetFormatType assetFormat in assetTypes)
            {
                TitanJobHandler job = new TitanJobHandler();
                job.Content = content;
                job.TrailerJob = true;
                job.OutFormatType = assetFormat;
                job.StartEncoding();
                jobs.Add(job);
            }

            //String existingJobID = ConaxIntegrationHelper.CheckForExistingJobID(parameters, false);
            //String existingTrailerJobID = ConaxIntegrationHelper.CheckForExistingJobID(parameters, true);

            //log.Debug("Existing jobID= " + existingJobID + " and Existing trailerJobID= " + existingTrailerJobID);

            // encoderJob.TrailerJob = false;
            // encoderJob.Content = content;

            //    String stateObject = "";
            //    if (String.IsNullOrEmpty(existingJobID))
            //    {
            //        log.Debug("Starting new job");
            //        String jobID = encoderJob.StartEncoding();
            //        stateObject = "jobID=" + jobID;
            //        parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket = stateObject;
            //        log.Debug("setting basket to " + stateObject);
            //        SaveCurrentWorkFlowProcessToDB();
            //        log.Debug("job started with ID = " + jobID);
            //    }
            //    else
            //    {
            //        encoderJob.JobID = existingJobID;
            //        log.Debug("Using existing jobID= " + existingJobID);
            //    }
            //    //String pause = systemConfig.GetConfigParam("Pause");
            //    //if (!String.IsNullOrEmpty(pause))
            //    //{
            //    //    log.Debug("<---------------------------- pausing -------------------------------->");
            //    //    Thread.Sleep(30000);
            //    //}

            //    bool encodeForTrailers = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == true) != null;
            //    if (encodeForTrailers)
            //    {
            //        trailerEncoderJob.TrailerJob = true;
            //        trailerEncoderJob.Content = content;
            //        if (String.IsNullOrEmpty(existingTrailerJobID))
            //        {
            //            log.Debug("Starting new trailer job");
            //            String trailerJobID = trailerEncoderJob.StartEncoding();

            //            stateObject += ";trailerJobID=" + trailerJobID;
            //            log.Debug("setting basket to " + stateObject);
            //            parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket = trailerJobID;
            //            SaveCurrentWorkFlowProcessToDB();
            //            log.Debug("trailer job started with ID = " + trailerJobID);
            //        }
            //        else
            //        {
            //            trailerEncoderJob.JobID = existingTrailerJobID;
            //            log.Debug("Using existing trailerJobID = " + existingTrailerJobID);
            //        }
            //    }
            //    if (encoderJob.CheckJobStatus())
            //    {
            //        log.Debug("encoder job finished!");
            //        encoderJob.UpdateAssets();
            //    }
            //    else
            //    {
            //        log.Error("Something went wrong encoding content with name = " + content.Name + " and contentID = " + content.ID);
            //        throw new Exception("Something went wrong encoding content with name = " + content.Name + " and contentID = " + content.ID);
            //    }
            //    if (encodeForTrailers)
            //    {
            //        if (trailerEncoderJob.CheckJobStatus())
            //        {
            //            log.Debug("Trailer encoder job finished!");
            //            trailerEncoderJob.UpdateAssets();
            //        }
            //        else
            //        {
            //            log.Error("Something went wrong encoding trailer for content with name = " + content.Name + " and contentID = " + content.ID);
            //            throw new Exception("Something went wrong encoding trailer for content with name = " + content.Name + " and contentID = " + content.ID);
            //        }
            //    }
            //     catch (Exception ex)
            //{
            //    log.Error("Something went wrong when handeling encoding", ex);

            //    encoderJob.DeleteCopiedFile();
            //    trailerEncoderJob.DeleteCopiedFile();
            //    log.Debug("Removed copied file");
            //    return new RequestResult(RequestResultState.Exception, ex);
            //}
            log.Debug("<---------------------- encoding done ------------------------->");
            return new RequestResult(RequestResultState.Successful);
        }
    }

    class JobContainer
    {
        List<TitanJobHandler> jobs = new List<TitanJobHandler>();
    }
}
