using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.CarbonEncoder;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Harmonic;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Carbon;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder;
using System.Threading;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.HarmonicOrigin;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class CarbonVodEncoderJob : EncoderJobHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private CarbonVodEncoderWrapper CarbonWrapper = new CarbonVodEncoderWrapper();

        private Job CarbonJob;

        public JobState jobState;

        public RequestParameters parameters;


        public Job StartJob()
        {
            try
            {

                var encoderConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "CarbonEncoder").SingleOrDefault();
                var xTendManagerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

                String uploadFolder = xTendManagerConfig.GetConfigParam("FileIngestWorkDirectory");

                String encoderRoot = encoderConfig.GetConfigParam("EncoderUploadFolder");
                String mezzanineName = ConaxIntegrationHelper.GetMezzanineName(content, trailerJob);
                String mezzanineToName = mezzanineName;
                bool pushToHarmonicOrigin = false;
                if (encoderConfig.ConfigParams.ContainsKey("UsingHarmonicOrigin"))
                    bool.TryParse(encoderConfig.GetConfigParam("UsingHarmonicOrigin"), out pushToHarmonicOrigin);
                if (pushToHarmonicOrigin)
                {
                    FileInfo file = new FileInfo(mezzanineName);
                    String extension = file.Extension;
                    String contentID = content.ID.Value.ToString();
                    if (trailerJob)
                        contentID += "_trailer";
                    String newFileName = contentID + extension;
                    mezzanineToName = newFileName;
                    log.Debug("using origin, changing name of inputFile name from " + mezzanineName + " to " + mezzanineToName);
                }

                String fullPathFrom = Path.Combine(uploadFolder, mezzanineName);
                String fullPathTo = Path.Combine(encoderRoot, mezzanineToName);

                if (jobState == null)
                    copiedFile = CopyFiles(fullPathFrom, fullPathTo);

                String templateGuid = CarbonEncoderHelper.GetProfileID("Carbon", content, trailerJob);

                //Fetch workflow order
                List<IncludedWorkFlow> workFlowListForEncoderJob = CarbonEncoderHelper.GetEncoderJobWorkFlowOrder(templateGuid);

                String inputFile = fullPathTo;

                String fileAreaPath = CarbonEncoderHelper.GetFileAreaPath(content, trailerJob);
                if (encoderConfig.ConfigParams.ContainsKey("StatusCheckInterval"))
                {
                    String checkInterval = encoderConfig.GetConfigParam("StatusCheckInterval");
                    statusCheckInterval = int.Parse(checkInterval) * 1000;
                }

                PlayoutFileDirectory = fileAreaPath;
                //  List<IncludedWorkFlow> workFlowListForEncoderJob = CarbonEncoderHelper.GetEncoderJobWorkFlowOrder(templateGuid);

                String jobConfigXml = "";

                String tempFolder = CarbonEncoderHelper.GetTempFolder(encoderRoot, content);

                String serverConfigName = "";

                JobState tempJobState = new JobState();

                int jobNo = 0;

                foreach (IncludedWorkFlow includedJob in workFlowListForEncoderJob)
                {
                    try
                    {
                        jobNo++;
                        log.Debug("using job with name " +includedJob.Name + ", Guid= " + includedJob.WorkFlowGuid + ", useParametersfromprevious= " + includedJob.UseParametersFromPreviousJob.ToString() + " useTempfolder= " + includedJob.UseTempFolderForOutput.ToString()); 
                        bool startJob = true;
                        if (jobState != null)
                        {
                            if (TemplateAlreadyUsed(includedJob)) // this job has already been finished, go to next;
                            {
                                log.Debug("Template with Guid " + includedJob.WorkFlowGuid + " have already been used, skipping this");
                                continue;
                            }
                            if (!String.IsNullOrEmpty(jobState.CurrentJobGuid))
                                startJob = false;

                        }
                        includedJob.TempFolder = tempFolder;

                        if (startJob)
                        {
                            log.Debug("No running job found, starting new!");
                            if (CarbonJob != null) // Fetch Name from previously job since that is the same name as the templateEx created
                                serverConfigName = CarbonJob.Name;
                            else
                            {
                                if (jobState != null && !String.IsNullOrEmpty(jobState.PreviousJobName))
                                {
                                    log.Debug("Using Name from previous job for configFile, name = " + jobState.PreviousJobName);
                                    serverConfigName = jobState.PreviousJobName;
                                }
                            }
                            jobConfigXml = CarbonEncoderHelper.FetchJobConfig(content, trailerJob, includedJob, inputFile, serverConfigName, mezzanineToName);

                            CarbonJob = CarbonWrapper.GenerateJob(includedJob, jobConfigXml, fileAreaPath, pushToHarmonicOrigin, jobNo == workFlowListForEncoderJob.Count);
                            log.Debug("Job is created");
                            tempJobState = new JobState();
                            tempJobState.CurrentJobGuid = CarbonJob.Guid.ToString();
                            tempJobState.CurrentTemplateGuidInWorkFlow = includedJob.WorkFlowGuid;
                            tempJobState.TemplateEx = jobConfigXml;
                            ConaxIntegrationHelper.SetCurrentJobState(tempJobState, parameters, trailerJob);
                            log.Debug("Job Started, guid= " + CarbonJob.Guid.ToString());
                        }
                        else
                        {
                            log.Debug("Found existing job with Guid= " + jobState.CurrentJobGuid + ", loading job from encoder!");
                            CarbonJob = CarbonWrapper.GetJob(jobState.CurrentJobGuid);
                            log.Debug("Job Loaded");
                        }
                        log.Debug("waiting for job to finish");
                        if (!CheckJobStatus())
                        {
                            log.Error("Something went wrong when running job for template with Guid " + includedJob.WorkFlowGuid);
                            throw new Exception("Something went wrong when running job for template with Guid " + includedJob.WorkFlowGuid + ", please check encoderlog for further information");
                        }
                        log.Debug("Job with guid " + CarbonJob.Guid.ToString() + " is done");

                        tempJobState.ListOfAlreadyUsedTemplates.Add(includedJob.WorkFlowGuid);
                        tempJobState.PreviousJobName = CarbonJob.Name;
                        tempJobState.CurrentTemplateGuidInWorkFlow = "";
                        tempJobState.CurrentJobGuid = "";
                        tempJobState.PreviousWorkflow = includedJob;
                        ConaxIntegrationHelper.SetCurrentJobState(tempJobState, parameters, trailerJob);
                    }
                    catch (Exception exc)
                    {
                        log.Error("Error running encoderJob", exc);
                        throw;
                    }
                }
                log.Debug("Encoding job workflow is done for content with objectID = " + content.ObjectID.ToString() + " : trailer = " + trailerJob);
                try
                {
                    copiedFile.Delete();
                    DirectoryInfo di = new DirectoryInfo(tempFolder);
                    di.Delete(true);
                }
                catch (Exception ex)
                {
                    log.Debug("Error deleting copied files", ex);
                }
                log.Debug("Updating assets");
                UpdateAssets();
                parameters.CurrentWorkFlowProcess.WorkFlowParameters.Basket = ""; // Clear basket for finished job
            }
            catch (Exception exc)
            {
                log.Error("Error Starting job", exc);
                throw;
            }
            return CarbonJob;
        }

        private bool TemplateAlreadyUsed(IncludedWorkFlow includedJob)
        {
            return jobState.ListOfAlreadyUsedTemplates.Contains(includedJob.WorkFlowGuid) && (String.IsNullOrEmpty(jobState.CurrentTemplateGuidInWorkFlow) || !jobState.CurrentTemplateGuidInWorkFlow.Equals(includedJob.WorkFlowGuid));
        }

       

        public bool CheckJobStatus()
        {
            if (statusCheckInterval == 0)
                statusCheckInterval = 15000;
            int tries = 0;
            CarbonEncoder.JobStatus status = CarbonEncoder.JobStatus.Queued;
            CarbonEncoder.Job job = null;
            while (true)
            {
                try
                {
                    job =  CarbonWrapper.CheckJobStatus(CarbonJob.Guid);
                    status = job.Status;
                }
                catch (Exception ex)
                {
                    tries++;
                    log.Warn("Error Checking jobStatus, number of tries= " + tries.ToString(), ex);
                    if (tries >= 3)
                    {
                        log.Error("Failed fetching jobStatus, exiting after three tries", ex);
                        status = CarbonEncoder.JobStatus.Fatal;
                    }
                }
                if (status == CarbonEncoder.JobStatus.Fatal || status == CarbonEncoder.JobStatus.Abort)
                {
                    break;
                }
                else if (status == CarbonEncoder.JobStatus.Completed)
                {
                    break;
                }
                else
                {
                    tries = 0; // call was successful, reset tries to 0
                    log.Info("In status check loop, current status = " + status.ToString() + ", " + job.Summary.AverageProgress.ToString() + "% done, sleeping for " + statusCheckInterval.ToString() + "ms");
                    Thread.Sleep(statusCheckInterval);
                }

            }
            if (status == CarbonEncoder.JobStatus.Completed)
            {
                log.Debug("Encoder job done for content with ID " + content.ID.ToString());
                return true;
            }
            else
            {
                log.Error("Encoder job failed, status= " + status.ToString());
                return false;
            }
        }

        public void SetJobGuid(String existingJobGuid)
        {
            if (CarbonJob != null)
                return;
            try
            {
                CarbonJob = CarbonWrapper.GetJob(existingJobGuid);
            }
            catch (Exception exc)
            {
                log.Error("Error Fetching job from encoder, jobGuid= " + existingJobGuid);
                throw;
            }
        }

        public void UpdateAssets()
        {
            log.Debug("in Update Asset");
            var encoderConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "CarbonEncoder").SingleOrDefault();
            var xTendManagerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

            IHarmonicOriginWrapper originWrapper = null;

          

            bool pushToHarmonicOrigin = false;
            if (encoderConfig.ConfigParams.ContainsKey("UsingHarmonicOrigin"))
                bool.TryParse(encoderConfig.GetConfigParam("UsingHarmonicOrigin"), out pushToHarmonicOrigin);
            if (pushToHarmonicOrigin)
            {
                log.Debug("Pushed to origin, setting status online");
                originWrapper = HarmonicOriginWrapperManager.Instance;
                log.Debug("Setting state on asset to online");
                originWrapper.SetAdminState(content, OriginState.Online, trailerJob);
                log.Debug("Status set");
            }

            Dictionary<AssetFormatType, String> urlsforType = new Dictionary<AssetFormatType, String>();

            String url = "";
           // String originRoot = xTendManagerConfig.GetConfigParam("FileAreaRoot");
            foreach (Util.ValueObjects.Asset asset in content.Assets.Where<Util.ValueObjects.Asset>(a => a.IsTrailer == trailerJob))
            {
                AssetFormatType assetFormatType = ConaxIntegrationHelper.GetAssetFormatTypeFromAsset(asset);
                if (pushToHarmonicOrigin)
                {
                    String originPath = "";
                    log.Debug("Assets pushed to origing, fetch url");
                    if (!urlsforType.ContainsKey(assetFormatType)) // not fetched for type yet
                    {
                        log.Debug("Fetching url from Harmonic Origin");
                        OriginVODAsset originVODAsset = originWrapper.GetAssetData(content, asset);
                        log.Debug("Fetched assetInformation from origin, data= " + JsonHelper.JsonSerializer<OriginVODAsset>(originVODAsset));
                        // asset.SetProperty("OriginAssetPath", originVODAsset.AssetPath);
                        urlsforType.Add(assetFormatType, originVODAsset.AssetPath);
                        originPath = originVODAsset.AssetPath;
                    }
                    else
                    {
                        log.Debug("fetching url from hash");
                        originPath = urlsforType[assetFormatType];
                    }
                    log.Debug("originpath= " + originPath);
                    url =  originPath;
                }
                else
                {
                    String mezzanineName = ConaxIntegrationHelper.GetMezzanineName(content, trailerJob);
                    String outPutPath = CarbonEncoderHelper.GetFileAreaPath(content, trailerJob);
                    log.Debug("Updating url on asset with name = " + asset.Name + " trailer = " + trailerJob.ToString());
                    //path = fileAreaPath;
                    log.Debug("path= " + outPutPath);
                    //path = path.Replace(encoderMappedFileArea, fileArea); // removed mapped path and set real path

                    String name = asset.Name.Remove(asset.Name.LastIndexOf("."));
                    if (assetFormatType == AssetFormatType.SmoothStreaming)
                    {
                        url = Path.Combine(outPutPath, name + ".ism/Manifest");
                    }
                    else if (assetFormatType == AssetFormatType.HTTPLiveStreaming)
                    {
                        url = Path.Combine(outPutPath, name + ".m3u8");
                    }
                }
                asset.Name = url;
                log.Debug("Setting name on asset to " + asset.Name);
            }
        }

        public void DeleteCopiedFile()
        {
            if (copiedFile != null)
                copiedFile.Delete();
        }

        public void DeletePlayoutFolder()
        {
            try
            {
                var conaxConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
                String fileArea = conaxConfig.GetConfigParam("FileAreaRoot");
                var encoderConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "CarbonEncoder").SingleOrDefault();
                String mappedFileArea = encoderConfig.GetConfigParam("EncoderUploadFolder");
                String tempFolder = CarbonEncoderHelper.GetTempFolder(mappedFileArea, content);
                try
                {
                    if (Directory.Exists(tempFolder))
                        Directory.Delete(tempFolder, true);
                }
                catch (Exception exc)
                {
                    log.Warn("Could'nt delete tempfolder", exc);
                }
                String fileLocation = CarbonEncoderHelper.GetFileAreaPath(content, trailerJob);
                try
                {
                    if (Directory.Exists(fileLocation))
                        Directory.Delete(fileLocation, true);
                }
                catch (Exception exc)
                {
                    log.Warn("Could'nt delete folder from filearea", exc);
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong deleting files from playoutDirectory", ex);
            }

        }
    }
}
