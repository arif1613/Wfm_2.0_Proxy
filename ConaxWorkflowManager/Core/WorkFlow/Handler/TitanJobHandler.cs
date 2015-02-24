using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Titan;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class TitanJobHandler : EncoderJobHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public AssetFormatType OutFormatType { get; set; }

        public String JobUrl { get; set; }

        public String JobName { get; set; }

        private TitanVODEncoderWrapper wrapper = new TitanVODEncoderWrapper();

        /// <summary>
        /// Starts the encoding job
        /// </summary>
        /// <returns>The ID of the encoding job, used to check status</returns>
        public String StartEncoding()
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "TitanEncoder").SingleOrDefault();
            var conaxConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String uploadFolder = conaxConfig.GetConfigParam("FileIngestWorkDirectory");
            String encoderRoot = systemConfig.GetConfigParam("EncoderUploadFolder");
            String mezzanineName = ConaxIntegrationHelper.GetMezzanineName(content, trailerJob);

            String fullPathFrom = Path.Combine(uploadFolder, mezzanineName);
            String fullPathTo = Path.Combine(encoderRoot, mezzanineName);

            if (!Directory.Exists(Path.GetDirectoryName(fullPathTo)))
            {
                log.Debug("Directory " + Path.GetDirectoryName(fullPathTo) + " doesn't exist");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPathTo));
                }
                catch (Exception ex)
                {
                    log.Error("Folder couldn't be created", ex);
                }
            }
           
            log.Debug("moving file from " + fullPathFrom + " to " + fullPathTo);
            EncoderFileSystemHandler fileMover = new EncoderFileSystemHandler();
            copiedFile = fileMover.MoveFile(fullPathFrom, fullPathTo);
            if (copiedFile == null)
            {
                throw new Exception("Error moving trailer file to encoding folder for content with objectID " + content.ObjectID.ToString() + " and name " + content.Name);
            }
            log.Debug("files copied to Encoder folder");

            String profileName = TitanEncoderHelper.GetProfileID("Titan", content, trailerJob);
            log.Debug("profileID= " + profileName);

            String encoderUrlPath = systemConfig.GetConfigParam("EncoderMappedFilePath");
            String fileArea = conaxConfig.GetConfigParam("FileAreaRoot");
           // String customerID = conaxConfig.GetConfigParam("CustomerID");

            Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == trailerJob);
            String outPutPath = fileArea + "/" + content.ObjectID.ToString() + "/" + Path.GetDirectoryName(asset.Name).Replace(@"\", "/") + "/";
            outPutPath = outPutPath.Replace(@"\", "/");
            PlayoutFileDirectory = outPutPath;
            log.Debug("outPutPath= " + outPutPath);

            String encoderPath = encoderUrlPath + "/" + mezzanineName;
            encoderPath = encoderPath.Replace(@"\", "/");
            String conaxContentID = ConaxIntegrationHelper.GetConaxContegoContentID(content);
            //List<JobParameter> param = wrapper.CreateDefaultParameters(encoderPath, outPutPath, conaxContentID);

           // JobID = wrapper.LaunchJob(profileName, param, trailerJob, content);

            return jobID;
        }

        public bool CheckJobStatus()
        {
            TitanJobInfo jobInfo = null;
            if (statusCheckInterval == 0)
                statusCheckInterval = 15000;
            log.Debug("In check status for jobs");
            //while (true)
            //{
            //    jobInfo =  wrapper.GetJobStatus("", jobName);
            //    if (jobInfo.status == ElementalJobStatus.error)
            //    {
            //        break;
            //    }
            //    else if (jobInfo.status == ElementalJobStatus.complete)
            //    {
            //        log.Debug("Encoding job with ID + " + jobID + " finished successfully");
            //        break;
            //    }
            //    else
            //    {
            //        log.Debug("In status check loop, current status = " + jobInfo.status.ToString() + ", sleeping for " + statusCheckInterval.ToString() + "ms");
            //        Thread.Sleep(statusCheckInterval);
            //    }
            //}
            //if (jobInfo.status != ElementalJobStatus.complete)
            //{
            //    log.Error("Something went wrong encoding file for job with ID " + jobID);
            //    return false;
            //}
            return true;
        }
    }
}
