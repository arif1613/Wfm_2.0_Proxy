using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Threading;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Envivio;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class EnvivioJobHandler : EncoderJobHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Envivio4BalancerServicesWrapper wrapper = new Envivio4BalancerServicesWrapper();

        private String SmoothStreamOutput;

        private String HLSOutPut;

        /// <summary>
        /// Starts the encoding job
        /// </summary>
        /// <returns>The ID of the encoding job, used to check status</returns>
        public String StartEncoding()
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "EnvivioEncoder").SingleOrDefault();
            var conaxConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            
            inputFolder = conaxConfig.GetConfigParam("FileIngestWorkDirectory");
            encoderFolder = systemConfig.GetConfigParam("EncoderUploadFolder");

            String checkInterval = systemConfig.GetConfigParam("StatusCheckInterval");
            statusCheckInterval = int.Parse(checkInterval) * 1000;

            String mezzanineName = ConaxIntegrationHelper.GetMezzanineName(content, trailerJob);

            String fullPathFrom = Path.Combine(inputFolder, mezzanineName);
            String fullPathTo = Path.Combine(encoderFolder, mezzanineName);

            String presetID = EnvivioEncoderHelper.GetProfileID("Envivio", content, trailerJob);

            log.Debug("moving file from " + fullPathFrom + " to " + fullPathTo);
            EncoderFileSystemHandler fileMover = new EncoderFileSystemHandler();
            copiedFile = fileMover.MoveFile(fullPathFrom, fullPathTo);
            if (copiedFile == null)
            {
                throw new Exception("Error moving trailer file to encoding folder for content with objectID " + content.ObjectID.ToString() + " and name " + content.Name);
            }
            log.Debug("files copied to Encoder folder");

            List<JobParameter> jobParameters = SetupJobParameters(content, trailerJob);
            log.Debug("Jobparameters created");
            String jobName = content.ObjectID.ToString() + "_" + content.Name;
            if (trailerJob)
                jobName += "_trailer";
            log.Debug("Started job for content " + content.Name + " with jobName = " + jobName + " with presetID= " + presetID);
            JobID = wrapper.LaunchEncodingJob(presetID, jobParameters, jobName);
            log.Debug("Encoder job started with ID " + jobID);
            if (String.IsNullOrEmpty(jobID))
            {
                throw new Exception("Error Starting encoding job for content with objectID " + content.ID.ToString() + " and name " + content.Name);
            }

            return jobID;
        }

        public bool CheckJobStatus()
        {
            log.Debug("In checkjobstatus");
            EncodingJobStatus status = null;
            if (statusCheckInterval == 0)
                statusCheckInterval = 15000;
            bool retrying = false;

            while (true)
            {
                status = wrapper.GetJobStatus(jobID);
                if (status.JobStatus == JobStatus.error || status.JobStatus == JobStatus.canceled || status.JobStatus == JobStatus.canceling)
                {
                    break;
                }
                else if (status.JobStatus == JobStatus.retrying && !retrying)
                {
                    log.Debug("Encoding with ID " + jobID + " failed, retrying");
                    retrying = true;
                    Thread.Sleep(statusCheckInterval);
                }
                else if (status.JobStatus == JobStatus.success)
                {
                    log.Debug("Encoding job with ID + " + jobID + " finished successfully");
                    break;
                }
                else
                {
                    Thread.Sleep(statusCheckInterval);
                    log.Debug("In status check loop, current status = " + status.JobStatus.ToString() + ", sleeping for " + statusCheckInterval.ToString() + "ms");
                }
            }
            if (status.JobStatus != JobStatus.success)
            {
                log.Error("Something went wrong encoding file for job with ID " + jobID);
                return false;

            }
            Thread.Sleep(statusCheckInterval);
            return true;
        }

        public void DeleteCopiedFile()
        {
            if (copiedFile != null)
                copiedFile.Delete();
        }

        public void DeletePlayoutFolder()
        {
            if (!String.IsNullOrEmpty(PlayoutFileDirectory))
            {
                try
                {
                    Directory.Delete(PlayoutFileDirectory, true);
                }
                catch (Exception ex)
                {
                    log.Error("Something went wrong deleting files from playoutDirectory", ex);
                }
            }
        }

        public void UpdateAsset()
        {
            //var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "EnvivioEncoder").SingleOrDefault();
            List<PlayType> encodingTypes = EnvivioJobHandler.GetEncodingTypes(content);
            if (encodingTypes.Contains(PlayType.SmoothStream))
            {
                SetSmoothAssets(content, trailerJob, SmoothStreamOutput);
            }
            if (encodingTypes.Contains(PlayType.HLS))
            {
                SetHLSAssets(content, trailerJob, HLSOutPut);
            }
           
        }

        private void SetHLSAssets(ContentData content, bool trailer, String url)
        {
            bool assetFound = false;
            String hlsURL = url + "\\index-eng.m3u8";
            log.Debug("Setting hls url to " + hlsURL);
            foreach (Asset asset in content.Assets.Where<Asset>(a => a.IsTrailer == trailer))
            {
                if (IsHLS(asset))
                {
                    assetFound = true;
                    asset.Name = hlsURL;
                }
            }

            if (!assetFound)
            {
                log.Debug("Didnt find a hls asset, looking to see if there is a ss asset and an undefined one");
                if (EnvivioJobHandler.GetEncodingTypes(content).Contains(PlayType.SmoothStream)) // SS exist on other assets
                {
                    log.Debug("There is a ss asset");
                    if (EnvivioJobHandler.GetEncodingTypes(content).Contains(PlayType.Unspecified)) // there are at least one unspecified asset, use this one
                    {
                        log.Debug("There is an undefined one");
                        foreach (Asset asset in content.Assets.Where<Asset>(a => a.IsTrailer == trailer))
                        {
                            if (IsUndefinedAsset(asset))
                            {
                                asset.Name = hlsURL;
                            }
                        }
                    }
                }
            }
        }

        private bool IsUndefinedAsset(Asset asset)
        {
            if (asset.Properties.Where<Property>(p => p.Type.Equals("DeviceType")).Count<Property>() == 0)
            {
                return true;
            }
            return false;
        }


        private bool IsHLS(Asset asset)
        {
            foreach (Property property in asset.Properties.Where<Property>(p => p.Type.Equals("DeviceType")))
            {
                if (property.Value.ToLower().Equals("iphone") || property.Value.ToLower().Equals("ipad"))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsSmooth(Asset asset)
        {
            foreach (Property property in asset.Properties.Where<Property>(p => p.Type.Equals("DeviceType")))
            {
                if (property.Value.ToLower().Equals("pc") || property.Value.ToLower().Equals("stb") || property.Value.ToLower().Equals("mac"))
                {
                    return true;
                }
            }
            return false;
        }

        private void SetSmoothAssets(ContentData content, bool trailer, String url)
        {
            log.Debug("SetSmoothAssets, url = " + url);
            String smoothURL = url + ".ism/Manifest";
            bool assetFound = false;
            log.Debug("Setting SmoothStream URL to " + smoothURL);
            try
            {
                foreach (Asset asset in content.Assets.Where<Asset>(a => a.IsTrailer == trailer))
                {
                    if (IsSmooth(asset))
                    {
                        assetFound = true;
                        asset.Name = smoothURL;
                    }
                }

                if (!assetFound)
                {
                    if (EnvivioJobHandler.GetEncodingTypes(content).Contains(PlayType.HLS)) // SS exist on other assets
                    {
                        if (EnvivioJobHandler.GetEncodingTypes(content).Contains(PlayType.Unspecified)) // there are at least one unspecified asset, use this one
                        {
                            foreach (Asset asset in content.Assets.Where<Asset>(a => a.IsTrailer == trailer))
                            {
                                if (IsUndefinedAsset(asset))
                                {
                                    asset.Name = smoothURL;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Error setting smoothURL");
            }
        }

        /// <summary>
        /// This method initializes parameters that are needed when updating contents etc when manager is restarted from a failure;
        /// </summary>
        public void SetupParameters()
        {
            SetupJobParameters(content, trailerJob);
        }

        /// <summary>
        /// Setups the parameters that should be sent to the envivio encoder.
        /// </summary>
        /// <param name="content">The content to setup the parameters for.</param>
        /// <param name="trailerJob">States if these is the parameters for the trailers</param>
        /// <returns></returns>
        private List<JobParameter> SetupJobParameters(ContentData content, bool trailerJob)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "EnvivioEncoder").SingleOrDefault();
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            List<JobParameter> parameters = new List<JobParameter>();
            List<AssetFormatType> encodingTypes = ConaxIntegrationHelper.GetEncodingTypes(content, trailerJob);
            String fileAreaRoot = managerConfig.GetConfigParam("FileAreaRoot");
            String smoothStreamingFileOutput = systemConfig.GetConfigParam("SmoothStreamOutputFolder");
            String hlsFileOutput = systemConfig.GetConfigParam("HLSOutputFolder");
            String customerID = managerConfig.GetConfigParam("CustomerID");

            String hlsParameterOutputName = systemConfig.GetConfigParam("HLSParameterOutputName");
            String smoothStreamParameterOutputName = systemConfig.GetConfigParam("SmoothStreamParameterOutputName");
            
            String mezzanineName = ConaxIntegrationHelper.GetMezzanineName(content, trailerJob);
            String encoderRoot = systemConfig.GetConfigParam("EncoderParameterPath");
            String inputFilePath = Path.Combine(encoderRoot, mezzanineName);
            JobParameter inputFileParameter = new JobParameter() { Name = "inputfilename", Value = inputFilePath };
            parameters.Add(inputFileParameter);
            String contentID = ConaxIntegrationHelper.GetConaxContegoContentID(content);

            PlayoutFileDirectory = fileAreaRoot + content.ObjectID.ToString() + "_" + customerID;
            String fileName = "";
            int fileExtensionPos = mezzanineName.IndexOf(".");
            if (fileExtensionPos != -1)
            {
                fileName = mezzanineName.Substring(0, fileExtensionPos);
            }
            if (encodingTypes.Contains(AssetFormatType.SmoothStreaming))
            {
                // Smooth
                log.Debug("adding parameter for smooth");
                String outputFolder = fileAreaRoot + content.ObjectID.ToString() + "_" + customerID + "\\" + smoothStreamingFileOutput;
                //smoothStreamingFileOutput = outputFolder + "\\" + mezzanineName;
               
                SmoothStreamOutput = outputFolder + "\\" + fileName;
                JobParameter outputFileParameter = new JobParameter() { Name = smoothStreamParameterOutputName, Value = outputFolder + "\\" + fileName };
                log.Debug("adding parameter name= " + smoothStreamParameterOutputName + " outputfolder= " + outputFolder);
                parameters.Add(outputFileParameter);

                JobParameter contentIDParameter = new JobParameter() { Name = "contentid", Value = contentID };
                parameters.Add(contentIDParameter);
                contentIDParameter = new JobParameter() { Name = "contentid_2", Value = contentID };
                parameters.Add(contentIDParameter);
            }
            if (encodingTypes.Contains(AssetFormatType.HTTPLiveStreaming))
            {
                // HLS
                String hlsOutputFolder = fileAreaRoot + content.ObjectID.ToString() + "_" + customerID + "\\" + hlsFileOutput;
                HLSOutPut = hlsOutputFolder + "\\" + fileName;
                JobParameter hlsOutputFileParameter = new JobParameter() { Name = hlsParameterOutputName, Value = hlsOutputFolder + "\\" + fileName };
                parameters.Add(hlsOutputFileParameter);

                JobParameter contentIDParameter = new JobParameter() { Name = "contentid", Value = contentID };
                parameters.Add(contentIDParameter);
                contentIDParameter = new JobParameter() { Name = "contentid_2", Value = contentID };
                parameters.Add(contentIDParameter);

            }
            return parameters;
        }

        /// <summary>
        /// Fetches all encoding types that should be encoded for this content, TODO! add dynamic configuration of DeviceTypes connected to playtype
        /// ie. iphone = hls and PC = smooth.
        /// </summary>
        /// <param name="content">The content to check what encoding types should be encoded for.</param>
        /// <returns></returns>
        public static List<PlayType> GetEncodingTypes(ContentData content)
        {
            List<PlayType> playTypes = new List<PlayType>();
            foreach (Asset asset in content.Assets)
            {
                if (asset.Properties.Exists(p => p.Type.Equals("DeviceType") && (p.Value.ToLower().Equals("mac") || p.Value.ToLower().Equals("pc") || p.Value.ToLower().Equals("stb"))))
                {
                    if (!playTypes.Contains(PlayType.SmoothStream))
                    {
                        playTypes.Add(PlayType.SmoothStream);
                    }
                }
                else if (asset.Properties.Exists(p => p.Type.Equals("DeviceType") && (p.Value.ToLower().Equals("ipad") || p.Value.ToLower().Equals("iphone"))))
                {
                    if (!playTypes.Contains(PlayType.HLS))
                    {
                        playTypes.Add(PlayType.HLS);
                    }
                }
                else
                {
                    if (!playTypes.Contains(PlayType.Unspecified))
                    {
                        playTypes.Add(PlayType.Unspecified);
                    }
                }
            }
            return playTypes;
        }

        private string GetPresetID(SystemConfig systemConfig, ContentData content, bool getPresetIDForTrailer)
        {
            String configPresetField = "";

            String encodeAllTypes = systemConfig.GetConfigParam("EncodeAllTypes");
            ResolutionType resolution = ConaxIntegrationHelper.GetResolutionType(content.Assets.FirstOrDefault<Asset>(a=>a.IsTrailer == getPresetIDForTrailer));
            if (resolution == ResolutionType.NotSpecified)
                throw new Exception("No resolution found for content " + content.Name);
            log.Debug("ResolutionType= " + resolution.ToString());
            if (resolution == ResolutionType.HD)
                configPresetField += "HD";

            List<PlayType> encodingTypes = GetEncodingTypes(content);
            bool encodeAll = false;
            bool.TryParse(encodeAllTypes, out encodeAll);

            if (!(encodeAll || (encodingTypes.Contains(PlayType.HLS) && encodingTypes.Contains(PlayType.SmoothStream))))
            {
                if (encodingTypes.Contains(PlayType.HLS))
                    configPresetField += "HLS";
                else if (encodingTypes.Contains(PlayType.SmoothStream))
                    configPresetField += "SS";
            }
            
            if (getPresetIDForTrailer)
                configPresetField += "Trailer";
            configPresetField += "PresetID";
            
            return systemConfig.GetConfigParam(configPresetField);
           
           
            
        }

    }

    public enum PlayType
    {
        Unspecified,
        HLS,
        SmoothStream
    }
}
