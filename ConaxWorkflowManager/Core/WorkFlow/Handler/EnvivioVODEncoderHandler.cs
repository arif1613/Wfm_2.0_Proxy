using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using System.Threading;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.Collections;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class EnvivioVodEncoderHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Envivio4BalancerServicesWrapper wrapper = new Envivio4BalancerServicesWrapper();

        EnvivioJobHandler encoderJob;
        EnvivioJobHandler trailerEncoderJob;

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");

            try
            {
                var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "EnvivioEncoder").SingleOrDefault();
                ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
                String existingJobID = ConaxIntegrationHelper.CheckForExistingJobID(parameters, false);
                String existingTrailerJobID = ConaxIntegrationHelper.CheckForExistingJobID(parameters, true);
                String stateObject = "";
                log.Debug("starting encoding for " + content.Name);
                encoderJob = new EnvivioJobHandler();
                encoderJob.TrailerJob = false;
                encoderJob.Content = content;
                if (String.IsNullOrEmpty(existingJobID))
                {
                    log.Debug("Starting new job");
                    String jobID = encoderJob.StartEncoding();
                    stateObject = "jobID=" + jobID;
                    parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket = stateObject;
                    log.Debug("setting basket to " + stateObject);
                    log.Debug("job started with ID = " + jobID);
                }
                else
                {
                    encoderJob.JobID = existingJobID;
                    encoderJob.SetupParameters();
                    log.Debug("Using existing jobID= " + existingJobID);
                }

                
                //String pause = systemConfig.GetConfigParam("Pause");
                //if (!String.IsNullOrEmpty(pause))
                //{
                //    log.Debug("<---------------------------- pausing -------------------------------->");
                //    Thread.Sleep(20000);
                //}

               
                trailerEncoderJob = new EnvivioJobHandler();
                trailerEncoderJob.TrailerJob = true;
                trailerEncoderJob.Content = content;
                if (String.IsNullOrEmpty(existingTrailerJobID))
                {
                    log.Debug("Starting new trailer job");
                    String trailerJobID = trailerEncoderJob.StartEncoding();

                    stateObject += ";trailerJobID=" + trailerJobID;
                    log.Debug("setting basket to " + stateObject);
                    parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket = stateObject;
                    log.Debug("trailer job started with ID = " + trailerJobID);
                }
                else
                {
                    trailerEncoderJob.JobID = existingTrailerJobID;
                    trailerEncoderJob.SetupParameters();
                    log.Debug("Using existing trailerJobID = " + existingTrailerJobID);
                }
                //if (!String.IsNullOrEmpty(pause))
                //{
                //    log.Debug("<---------------------------- pausing -------------------------------->");
                //    Thread.Sleep(20000);
                //}

                if (encoderJob.CheckJobStatus() && trailerEncoderJob.CheckJobStatus()) // check if both jobs was successful
                {

                    encoderJob.UpdateAsset();
                    trailerEncoderJob.UpdateAsset();
                    MPPIntegrationServicesWrapper wrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
                    wrapper.UpdateAssets(content);
                    try
                    {
                        encoderJob.DeleteCopiedFile();
                        trailerEncoderJob.DeleteCopiedFile();
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Error when deleting copied file");
                    }
                }
                else
                {
                    log.Error("Something went wrong encoding content with name = " + content.Name + " and contentID = " + content.ID);
                    throw new Exception("Something went wrong encoding content with name = " + content.Name + " and contentID = " + content.ID);
                }
            }
            catch (Exception e)
            {
                log.Error("Something went wrong when handling encoding", e);
                log.Debug("removing trailers from encoder folder");

                encoderJob.DeleteCopiedFile();
                trailerEncoderJob.DeleteCopiedFile();
                log.Debug("Removed copied file");

                return new RequestResult(RequestResultState.Failed, "Something went wrong when handling encoding");
            }

            return new RequestResult(RequestResultState.Successful);
        }

        public override void OnChainFailed(RequestParameters parameters)
        {
            try
            {
                encoderJob.DeletePlayoutFolder();
            }
            catch (Exception ex)
            {
                log.Error("Error deleting playout directory", ex);
            }
            
        }
        

        //private bool MoveFinishedFiles(ContentData content, string fileRoot, string fileName, string trailerFileName)
        //{
        //    List<FileInfo> allMovedFiles = null;
        //    try
        //    {
        //        allMovedFiles = MoveSmoothStreamFiles(content, fileRoot, fileName, trailerFileName);
        //    }
        //    catch (Exception e)
        //    {
        //        log.Error("Error moving encoded smooth stream files to file area", e);
        //        return false;
        //    }
        //    try
        //    {
        //        MoveHLSFiles(content, fileRoot, fileName, trailerFileName);
        //    }
        //    catch (Exception e)
        //    {
        //        log.Error("Error moving encoded HLS files to file area", e);
        //        log.Debug("Removing copied files");
        //        EncoderFileSystemHandler fileHandler = new EncoderFileSystemHandler();
        //        fileHandler.RemoveCopiedFiles(allMovedFiles);
        //        return false;
        //    }


        //    return true;
        //}

        //private void MoveHLSFiles(ContentData content, string fileRoot, string fileName, string trailerFileName)
        //{
        //    try
        //    {
        //        var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "Encoder").SingleOrDefault();

        //        String fileAreaRoot = systemConfig.GetConfigParam("FileAreaRoot");

        //        String hlsFileOutput = systemConfig.GetConfigParam("HLSOutputFolder");

        //        EncoderFileSystemHandler fileHandler = new EncoderFileSystemHandler();

        //        List<FileInfo> files = fileHandler.GetHLSFiles(fileRoot + "\\" + hlsFileOutput); // fetch all hls files to copy

        //        List<FileInfo> copiedFiles = new List<FileInfo>();
        //        foreach (FileInfo file in files)
        //        {
        //            FileInfo copiedFile = fileHandler.MoveFile(file, fileAreaRoot);
        //            if (copiedFile == null)
        //            {
        //                fileHandler.RemoveCopiedFiles(copiedFiles);
        //                throw new Exception("Error copying encoded HLS files");
        //            }
        //            else
        //            {
        //                copiedFiles.Add(copiedFile);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        log.Error("error fetching hls files to copy");
        //        throw;
        //    }
        //}


        //private List<FileInfo> MoveSmoothStreamFiles(ContentData content, string fileRoot, string fileName, string trailerFileName)
        //{
        //    try
        //    {
        //        var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "Encoder").SingleOrDefault();

        //        String fileAreaRoot = systemConfig.GetConfigParam("FileAreaRoot");

        //        String smoothStreamingFileOutput = systemConfig.GetConfigParam("SmoothStreamOutputFolder");

        //        DirectoryInfo dir = new DirectoryInfo(fileRoot);
        //        fileName = fileName.Remove(fileName.IndexOf(".")); // remove extension

        //        FileInfo[] files = dir.GetFiles(fileName + "*");

        //        EncoderFileSystemHandler fileMover = new EncoderFileSystemHandler();
        //        List<FileInfo> copiedFiles = new List<FileInfo>();
        //        foreach (FileInfo file in files)
        //        {
        //            FileInfo copiedFile = fileMover.MoveFile(file, fileAreaRoot);
        //            if (copiedFile == null)
        //            {
        //                fileMover.RemoveCopiedFiles(copiedFiles);
        //                throw new Exception("Error copying encoded smooth stream files");
        //            }
        //            else
        //            {
        //                copiedFiles.Add(copiedFile);
        //            }
        //        }
        //        fileName = trailerFileName.Remove(trailerFileName.IndexOf(".")); // remove extension
        //        files = dir.GetFiles(fileName + "*");

        //        List<FileInfo> copiedTrailerFiles = new List<FileInfo>();
        //        foreach (FileInfo file in files)
        //        {
        //            FileInfo copiedFile = fileMover.MoveFile(file, fileAreaRoot);
        //            if (copiedFile == null)
        //            {
        //                fileMover.RemoveCopiedFiles(copiedTrailerFiles);
        //                fileMover.RemoveCopiedFiles(copiedFiles);
        //                throw new Exception("Error copying encoded smooth stream trailers");
        //            }
        //            else
        //            {
        //                copiedTrailerFiles.Add(copiedFile);
        //            }
        //        }
        //        copiedFiles.AddRange(copiedTrailerFiles);
        //        return copiedFiles;
        //    }
        //    catch (Exception e)
        //    {
        //        throw;
        //    }
        //}

     
    }
}
