using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Management;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler;
using System.IO;
using MPS.MPP.Auxiliary.CompositeManifestGenerator.Generator.Util;
using MPS.MPP.Auxiliary.ImportServer.Server.Util;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    
    public class SeaChangeServicesWrapper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool UploadFilesToServer(ContentData content, String seaChangeArea, MultipleServicePrice price)
        {
            // mpg, png, xml 
            //upload sourcefiles
            //upload images
            //uload metadata
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            IFileHandler fileHandler = Activator.CreateInstance(System.Type.GetType(systemConfig.GetConfigParam("SeaChangeFileIngestHandlerType"))) as IFileHandler;
            log.Debug("FileHandler to use is " + systemConfig.GetConfigParam("SeaChangeFileIngestHandlerType"));
            var seaChangeConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SeaChange").SingleOrDefault();
            bool useStorage = false;
            bool.TryParse(seaChangeConfig.GetConfigParam("UseSourceStorage"), out useStorage);
            String srckDir = ""; 
            if (useStorage)
                srckDir = seaChangeConfig.GetConfigParam("SourceStorageDirectory");
            else
                srckDir = systemConfig.GetConfigParam("FileIngestProcessedDirectory");
               
            Dictionary<String, String> fileList = new Dictionary<String, String>();
            try
            {
                log.Debug("Start copy files from folder " + srckDir + " to folder " + seaChangeArea);
               
                // copy asset
                Asset va = content.Assets.FirstOrDefault(v => v.IsTrailer == false);
                Property assetNameProperty = va.Properties.FirstOrDefault(a=>a.Type.Equals("SourceFileName"));
                if (assetNameProperty == null || String.IsNullOrEmpty(assetNameProperty.Value))
                    throw new Exception("No assetName property found on asset");

                String assetName = assetNameProperty.Value;
                FileInfo fi = new FileInfo(assetName);
                String assetNameWithoutFolders = fi.Name;
                String vaFrom = Path.Combine(srckDir, assetName);
                String vaTo = Path.Combine(seaChangeArea, assetNameWithoutFolders);
                log.Debug("Copying asset from " + vaFrom + " to " + vaTo);
                fileHandler.CopyTo(vaFrom, vaTo);
                log.Debug("Done copying asset"); 
                fileList.Add(vaFrom, vaTo);

                // copy trailer
                Asset trailer = content.Assets.FirstOrDefault(t => t.IsTrailer == true);
                if (trailer != null)
                {
                    assetNameProperty = trailer.Properties.FirstOrDefault(a => a.Type.Equals("SourceFileName"));
                    if (assetNameProperty == null || String.IsNullOrEmpty(assetNameProperty.Value))
                        throw new Exception("No assetName property found on trailer asset");
                    fi = new FileInfo(assetNameProperty.Value);
                    assetName = assetNameProperty.Value;
                    String trailerFrom = Path.Combine(srckDir, assetName);
                    String trailerTo = Path.Combine(seaChangeArea, fi.Name);
                    log.Debug("Copying trailer from " + trailerFrom + " to " + trailerTo);
                    fileHandler.CopyTo(trailerFrom, trailerTo);
                    log.Debug("Done copying trailer");
                    fileList.Add(trailerFrom, trailerTo);
                }

                // copy images
                foreach (LanguageInfo lang in content.LanguageInfos)
                {
                    foreach (Image image in lang.Images)
                    {
                        FileInfo fileInfo = new FileInfo(image.URI);
                        String imageFrom = Path.Combine(srckDir, image.URI);
                        String imageTo = Path.Combine(seaChangeArea, fileInfo.Name);
                        log.Debug("Copying image from " + imageFrom + " to " + imageTo);
                        fileHandler.CopyTo(imageFrom, imageTo);
                        log.Debug("Done copying image"); 
                        fileList.Add(imageFrom, imageTo);
                    }
                }

                var ingestXMLFileNameProperty = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName", StringComparison.OrdinalIgnoreCase));
                // copy xml
                FileInfo xmlFile = new FileInfo(ingestXMLFileNameProperty.Value);
                ExternalXMLHelper xmlHelper = new ExternalXMLHelper();
                XmlDocument doc = xmlHelper.AsColumbusCableLabs1_1(content, price);
                //String xmlFrom = Path.Combine(srckDir, ingestXMLFileNameProperty.Value);
                String xmlTo = Path.Combine(seaChangeArea, xmlFile.Name);
                log.Debug("Copying xml to " + xmlTo);
                try
                {
                    doc.Save(xmlTo);
                }
                catch (Exception exc)
                {
                    log.Error("Error saving xml to seachange", exc);
                    throw;
                }
                
                log.Debug("Done Copying xml");
                fileList.Add(xmlTo, xmlTo);

                log.Debug("all files copied successfully to folder " + seaChangeArea);
            }
            catch (Exception ex)
            {
                log.Warn("failed to copy files to folder " + seaChangeArea + ", will skip this ingest", ex);
                // remove already copied files from work folder
                //foreach (KeyValuePair<String, String> kvp in fileList)
                //{
                //    fileHandler.DeleteFile(kvp.Value);
                //}

                //return false;
                throw;
            }
            return true;
        }

        public bool DeleteFilesFromServer(ContentData content)
        {

            return true;
        }

        /// <summary>
        /// Get space left on server
        /// </summary>
        /// <param name="driveArea"<>The path to the folder, ie \\storage.movies.com\movies\ /param>
        /// <param name="inPercentage">Not handled now.</param>
        /// <returns></returns>
        public int GetSpaceLeftOnServer(String UNCPath, bool inPercentage)
        {
            double totalStorage = 0;
            if (!UNCPath.EndsWith(@"\"))
                UNCPath += @"\";
            double freeSpace = Validator.CheckFreeSpace(UNCPath);
            
            if (inPercentage)
            {
                //double totalStorage = double.Parse(totalStorageString);
                //double freeSpace = double.Parse(freeSpaceString);
               // double spaceLeftAsPercentage = freeSpace / totalStorage;
              //  int spaceLeft = (int)(spaceLeftAsPercentage * 100);
                throw new NotImplementedException("InPercentage true have not been implemented");
              //  return spaceLeft;
            }
            else
            {
                return (int)(freeSpace / 1024000000);
            }
        }

        //public int GetSpaceLeftOnServer(String drive, bool inPercentage)
        //{
        //    ConnectionOptions options = new ConnectionOptions();
        //    ManagementScope scope = new ManagementScope("\\\\localhost\\root\\cimv2",
        //    options);
        //    scope.Connect();
        //    SelectQuery query = new SelectQuery("Select * from Win32_LogicalDisk");

        //    ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
        //    ManagementObjectCollection queryCollection = searcher.Get();

        //    String freeSpaceString = "";

        //    bool foundDrive = false;
        //    foreach (ManagementObject mo in queryCollection)
        //    {
        //        if (drive.ToLower().StartsWith(mo["Name"].ToString().ToLower()))
        //        {
        //            freeSpaceString = mo["FreeSpace"].ToString();
        //            foundDrive = true;
        //            break;
        //        }
        //    }
        //    if (!foundDrive)
        //        throw new Exception("Error when fetching diskarea left on drive, " + drive + " was not found");
           
        //    double freeSpace = double.Parse(freeSpaceString);

        //    return (int)(freeSpace / 1024000000);
        //}
    }
}
