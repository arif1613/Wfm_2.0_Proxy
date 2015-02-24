using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Policy;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using System.Threading;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Elemental;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class ElementalJobHandler : EncoderJobHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ElementalVODServicesWrapper wrapper = new ElementalVODServicesWrapper();

        /// <summary>
        /// Starts the encoding job
        /// </summary>
        /// <returns>The ID of the encoding job, used to check status</returns>
        public String StartEncoding(Asset asset,string profilename,bool trailorjob)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
            var conaxConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String uploadFolder = conaxConfig.GetConfigParam("FileIngestWorkDirectory");
            String encoderRoot = systemConfig.GetConfigParam("EncoderUploadFolder");
            //String mezzanineName = ConaxIntegrationHelper.GetMezzanineName(content, trailerJob);
            String mezzanineName = asset.Name;
            String fullPathFrom = Path.Combine(uploadFolder, mezzanineName);
            String fullPathTo = Path.Combine(encoderRoot, mezzanineName);

            //if (!Directory.Exists(Path.GetDirectoryName(fullPathTo)))
            //{
            //    log.Debug("Directory " + Path.GetDirectoryName(fullPathTo) + " doesn't exist");
            //    try
            //    {
            //        Directory.CreateDirectory(Path.GetDirectoryName(fullPathTo));
            //    }
            //    catch (Exception ex)
            //    {
            //        log.Error("Folder couldn't be created", ex);
            //    }
            //}
            //if (!fullPathFrom.Equals(fullPathTo, StringComparison.OrdinalIgnoreCase))
            //{
            //    log.Debug("moving file from " + fullPathFrom + " to " + fullPathTo);
            //    EncoderFileSystemHandler fileMover = new EncoderFileSystemHandler();
            //    copiedFile = fileMover.MoveFile(fullPathFrom, fullPathTo);
            //    if (copiedFile == null)
            //    {
            //        throw new Exception("Error moving trailer file to encoding folder for content with objectID " + content.ObjectID.ToString() + " and name " + content.Name);
            //    }
            //    log.Debug("files copied to Encoder folder");
            //}
            //else
            //{
            //    log.Debug("Using XTendWorkfolder as encoder workFolder, no copy needed");
            //}
            //String profileName = ElementalEncoderHelper.GetProfileID("Elemental", content, trailerJob);
            //log.Debug("profileID= " + profileName);
            //ConaxIntegrationHelper.SetProfileUsedProperty(content, profilename, trailorjob);
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            mppWrapper.UpdateContent(content, false);

            String encoderUrlPath = systemConfig.GetConfigParam("EncoderMappedFilePath");

            String fileArea = systemConfig.GetConfigParam("EncoderMappedFileAreaRoot");
            //String trailerFileArea = systemConfig.GetConfigParam("EncoderMappedFileAreaRoot");
            if (trailorjob)
            {
                if (systemConfig.ConfigParams.ContainsKey("EncoderMappedTrailerFileAreaRoot") &&
                    !String.IsNullOrEmpty(systemConfig.GetConfigParam("EncoderMappedTrailerFileAreaRoot")))
                    fileArea = systemConfig.GetConfigParam("EncoderMappedTrailerFileAreaRoot");

                //if (systemConfig.ConfigParams.ContainsKey("TrailerEncoderMappedFilePath") &&
                //    !String.IsNullOrEmpty(systemConfig.GetConfigParam("TrailerEncoderMappedFilePath")))
                //    encoderUrlPath = systemConfig.GetConfigParam("TrailerEncoderMappedFilePath");
            }
            String customerID = conaxConfig.GetConfigParam("CustomerID");

            //Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == trailerJob);
            //if (trailerJob)
            //    fileArea = trailerFileArea;
            String outPutPath = fileArea + "/" + content.ObjectID.ToString() + "_" + customerID + "/" +  Path.GetDirectoryName( asset.Name).Replace(@"\", "/") + "/";
            outPutPath = outPutPath.Replace(@"\", "/");
            PlayoutFileDirectory = outPutPath;
            log.Debug("outPutPath= " + outPutPath);
           
            String encoderPath = Path.Combine(encoderUrlPath, mezzanineName);
            encoderPath = encoderPath.Replace(@"\", "/");
            String conaxContentID = ConaxIntegrationHelper.GetConaxContegoContentID(content);
            List<JobParameter> param = wrapper.CreateDefaultParameters(encoderPath, outPutPath, conaxContentID );

            JobID = wrapper.LaunchJob(profilename, param, trailorjob, content);

            return jobID;
        }

        public bool CheckJobStatus()
        {
            ElementalJobInfo jobInfo = null;
            if (statusCheckInterval == 0)
                statusCheckInterval = 15000;
            log.Debug("In check status for jobs");
            while (true)
            {
                jobInfo = wrapper.GetJobStatus(jobID);
                if (jobInfo.status == ElementalJobStatus.error)
                {
                    break;
                }
                else if (jobInfo.status == ElementalJobStatus.complete)
                {
                    log.Debug("Encoding job with ID + " + jobID + " finished successfully");
                    break;
                }
                else
                {
                    log.Info("In status check loop, current status = " + jobInfo.status.ToString() + ", progress = " + jobInfo.Progress + ", sleeping for " + statusCheckInterval.ToString() + "ms");
                    Thread.Sleep(statusCheckInterval);
                }
            }
            if (jobInfo.status != ElementalJobStatus.complete)
            {
                log.Error("Something went wrong encoding file for job with ID " + jobID);
                return false;
            }
            return true;
        }

        public void UpdateAssets()
        {
            log.Debug("in Update Asset");
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
          //  String encoderMappedFileArea = systemConfig.GetConfigParam("EncoderMappedFileAreaRoot");
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String fileArea = managerConfig.GetConfigParam("FileAreaRoot");
            if (!fileArea.EndsWith(@"\"))
                fileArea += @"\";
            String trailerFileAreaRoot = fileArea;

            if (managerConfig.ConfigParams.ContainsKey("FileAreaTrailerRoot") && !String.IsNullOrEmpty(managerConfig.GetConfigParam("FileAreaTrailerRoot")))
                trailerFileAreaRoot = managerConfig.GetConfigParam("FileAreaTrailerRoot");
            if (!trailerFileAreaRoot.EndsWith(@"\"))
                trailerFileAreaRoot += @"\";
            String customerID = managerConfig.GetConfigParam("CustomerID");
            String outPutPath = fileArea + content.ObjectID.ToString() + "_" + customerID + @"\";

            String trailerOutPutPath = trailerFileAreaRoot + content.ObjectID.ToString() + "_" + customerID + @"\";

            String path = "";
            String url = "";
            foreach (Asset asset in content.Assets.Where<Asset>(a=>a.IsTrailer == trailerJob))
            {
                log.Debug("Updating url on asset with name = " + asset.Name + " trailer = " + trailerJob.ToString());
                path = outPutPath;
                if (trailerJob)
                    path = trailerOutPutPath;
                log.Debug("path= " + path);
                //path = path.Replace(encoderMappedFileArea, fileArea); // removed mapped path and set real path

                Property deviceTypeProperty = asset.Properties.FirstOrDefault<Property>(p => p.Type.Equals("DeviceType"));
                if (deviceTypeProperty != null)
                {
                    String deviceType = deviceTypeProperty.Value.ToLower();
                    String name = asset.Name.Remove(asset.Name.LastIndexOf("."));
                    if (deviceType.Equals("ipad", StringComparison.OrdinalIgnoreCase))
                    {
                        String ipadFilePrefix = systemConfig.GetConfigParam("iPadPrefix");
                        if (!String.IsNullOrEmpty(ipadFilePrefix))
                        {
                            FileInfo fi = new FileInfo(name);
                            name = Path.Combine(fi.Directory.Parent.Name, fi.Directory.Name, ipadFilePrefix + fi.Name);
                        }
                        //String ipadFileName = systemConfig.GetConfigParam("IpadFileName");
                        url = path + name + ".m3u8";
                        //url = url.Replace("//", "");
                        asset.Name = url;
                        log.Debug("Setting name on asset to " + asset.Name);
                    }
                    else if (deviceType.Equals("iphone", StringComparison.OrdinalIgnoreCase))
                    {
                        String iPhonePrefix = systemConfig.GetConfigParam("iPhonePrefix");
                        if (!String.IsNullOrEmpty(iPhonePrefix))
                        {
                            FileInfo fi = new FileInfo(name);
                            name = Path.Combine(fi.Directory.Parent.Name, fi.Directory.Name, iPhonePrefix + fi.Name);
                        }
                        url = path + name + ".m3u8";
                        // url = url.Replace("//", "");
                        asset.Name = url;
                        log.Debug("Setting name on asset to " + asset.Name);
                    }
                    else if (deviceType.Equals("pc", StringComparison.OrdinalIgnoreCase)) // deviceType.Equals("mac") || deviceType.Equals("stb"))
                    {
                        String pcPrefix = systemConfig.GetConfigParam("PCPrefix");
                        if (!String.IsNullOrEmpty(pcPrefix))
                        {
                            FileInfo fi = new FileInfo(name);
                            name = Path.Combine(fi.Directory.Parent.Name, fi.Directory.Name, pcPrefix + fi.Name);
                        }
                        url = path + name + ".ism/Manifest";
                        // url = url.Replace("//", "");
                        asset.Name = url;
                        log.Debug("Setting name on asset to " + asset.Name);
                    }
                    else if (deviceType.Equals("mac", StringComparison.OrdinalIgnoreCase)) // deviceType.Equals("mac") || deviceType.Equals("stb"))
                    {
                        String macPrefix = systemConfig.GetConfigParam("MacPrefix");
                        if (!String.IsNullOrEmpty(macPrefix))
                        {
                            FileInfo fi = new FileInfo(name);
                            name = Path.Combine(fi.Directory.Parent.Name, fi.Directory.Name, macPrefix + fi.Name);
                        }
                        url = path + name + ".ism/Manifest";
                        // url = url.Replace("//", "");
                        asset.Name = url;
                        log.Debug("Setting name on asset to " + asset.Name);
                    }
                    else if (deviceType.Equals("stb", StringComparison.OrdinalIgnoreCase)) // deviceType.Equals("mac") || deviceType.Equals("stb"))
                    {
                        String stbPrefix = systemConfig.GetConfigParam("STBPrefix");
                        if (!String.IsNullOrEmpty(stbPrefix))
                        {
                            FileInfo fi = new FileInfo(name);
                            name = Path.Combine(fi.Directory.Parent.Name, fi.Directory.Name, stbPrefix + fi.Name);
                        }
                        url = path + name + ".ism/Manifest";
                        // url = url.Replace("//", "");
                        asset.Name = url;
                        log.Debug("Setting name on asset to " + asset.Name);
                    }
                    else if (deviceType.Equals("AndroidMobile", StringComparison.OrdinalIgnoreCase))
                    {
                        String androidMobilePrefix = systemConfig.GetConfigParam("AndroidMobilePrefix");
                        if (!String.IsNullOrEmpty(androidMobilePrefix))
                        {
                            FileInfo fi = new FileInfo(name);
                            name = Path.Combine(fi.Directory.Parent.Name, fi.Directory.Name, androidMobilePrefix + fi.Name);
                        }
                        url = path + name + ".ism/Manifest";
                        // url = url.Replace("//", "");
                        asset.Name = url;
                        log.Debug("Setting name on asset to " + asset.Name);
                    }
                    else if (deviceType.Equals("AndroidTablet", StringComparison.OrdinalIgnoreCase))
                    {
                        String androidTabletPrefix = systemConfig.GetConfigParam("AndroidTabletPrefix");
                        if (!String.IsNullOrEmpty(androidTabletPrefix))
                        {
                            FileInfo fi = new FileInfo(name);
                            name = Path.Combine(fi.Directory.Parent.Name, fi.Directory.Name, androidTabletPrefix + fi.Name);
                        }
                        url = path + name + ".ism/Manifest";
                        // url = url.Replace("//", "");
                        asset.Name = url;
                        log.Debug("Setting name on asset to " + asset.Name);
                    }
                    else if (deviceType.Equals("Android", StringComparison.OrdinalIgnoreCase))
                    {
                        String androidPrefix = systemConfig.GetConfigParam("AndroidPrefix");
                        if (!String.IsNullOrEmpty(androidPrefix))
                        {
                            FileInfo fi = new FileInfo(name);
                            name = Path.Combine(fi.Directory.Parent.Name, fi.Directory.Name, androidPrefix + fi.Name);
                        }
                        url = path + name + ".ism/Manifest";
                        // url = url.Replace("//", "");
                        asset.Name = url;
                        log.Debug("Setting name on asset to " + asset.Name);
                    }
                }
            }
           
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
                    var conaxConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
                    String fileArea = conaxConfig.GetConfigParam("FileAreaRoot");

                    var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
                    String mappedFileArea = systemConfig.GetConfigParam("EncoderMappedFileAreaRoot");
                    if (trailerJob)
                    {
                        if (conaxConfig.ConfigParams.ContainsKey("FileAreaTrailerRoot") &&
                            !String.IsNullOrEmpty(conaxConfig.GetConfigParam("FileAreaTrailerRoot")))
                            fileArea = conaxConfig.GetConfigParam("FileAreaTrailerRoot");
                        if (systemConfig.ConfigParams.ContainsKey("EncoderMappedTrailerFileAreaRoot") &&
                            !String.IsNullOrEmpty(systemConfig.GetConfigParam("EncoderMappedTrailerFileAreaRoot")))
                            mappedFileArea = systemConfig.GetConfigParam("EncoderMappedTrailerFileAreaRoot");
                    }

                    String deleteUNC = PlayoutFileDirectory.Replace(mappedFileArea, fileArea);
                    Directory.Delete(deleteUNC, true);
                }
                catch (Exception ex)
                {
                    log.Error("Something went wrong deleting files from playoutDirectory", ex);
                }
            }
        }
    }
}
