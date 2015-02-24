using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class DeleteContentFromFileAreaHandler : ResponsibilityHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            log.Debug("Initializing deletion from filearea for content with name= " + content.Name + " and objectID= " + content.ObjectID.Value);

            try
            {
                //var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
                //String fileAreaRoot = systemConfig.GetConfigParam("FileAreaRoot");
                //String trailerFileAreaRoot = fileAreaRoot;

                //if (systemConfig.ConfigParams.ContainsKey("FileAreaTrailerRoot") && !String.IsNullOrEmpty(systemConfig.GetConfigParam("FileAreaTrailerRoot")))
                //    trailerFileAreaRoot = systemConfig.GetConfigParam("FileAreaTrailerRoot");
                
                //String customerID = systemConfig.GetConfigParam("CustomerID");
                //String PlayoutFileDirectory = Path.Combine(fileAreaRoot, content.ObjectID.ToString() + "_" + customerID);//fileAreaRoot + content.ObjectID.ToString() + "_" + customerID;
                //String TrailerPlayoutFileDirectory = Path.Combine(trailerFileAreaRoot, content.ObjectID.ToString() + "_" + customerID);//fileAreaRoot + content.ObjectID.ToString() + "_" + customerID;

                //CheckDirectory(PlayoutFileDirectory);
                //Directory.Delete(PlayoutFileDirectory, true);

                //CheckDirectory(TrailerPlayoutFileDirectory);
                //Directory.Delete(TrailerPlayoutFileDirectory, true);

                
                List<String> assetFileRootFolders = new List<String>();
                foreach(Asset asset in content.Assets) {
                    String fileRoot = GetFileRootFromAssetName(content.ObjectID.Value, asset.Name);
                    if (!String.IsNullOrEmpty(fileRoot) &&
                        !assetFileRootFolders.Contains(fileRoot))
                        assetFileRootFolders.Add(fileRoot);
                }

                foreach(String assetFileRootFolder in assetFileRootFolders) {
                    if (!Directory.Exists(assetFileRootFolder))
                        continue;
                    CheckDirectory(assetFileRootFolder);
                    Directory.Delete(assetFileRootFolder, true);
                }

            }
            catch (Exception e)
            {
                log.Warn("Error when deleting from filearea for content with name " + content.Name, e);
                //return false;
            }

            return new RequestResult(RequestResultState.Successful);
        }
        public String GetFileRootFromAssetName(UInt64 contentObjectId, String assetName) {
            
            if (String.IsNullOrEmpty(assetName))
                return String.Empty;

            String dirName = Path.GetDirectoryName(assetName);            
            String fileName = Path.GetFileName(assetName);
            if (!String.IsNullOrEmpty(dirName))
                 fileName = Path.GetFileName(dirName);

            if (!fileName.StartsWith(contentObjectId + "_"))
                return GetFileRootFromAssetName(contentObjectId, dirName);

            return dirName;
        }

        private void CheckDirectory(string directory)
        {
            log.Debug("Checking subdirectory " + directory);
            String[] directories = Directory.GetDirectories(directory);
            foreach (String dir in directories)
            {
                CheckDirectory(dir);
            }
            CheckAndDeleteFiles(directory);
        }

        private void CheckAndDeleteFiles(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            foreach (string file in files)
            {
                FileAttributes attributes = File.GetAttributes(file);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    log.Debug("File " + file + " is readonly, removing readonly");
                    File.SetAttributes(file, ~FileAttributes.ReadOnly);
                    
                }
                try
                {
                    File.Delete(file);
                }
                catch (Exception exc)
                {
                    log.Warn("Error deleting " + file + " continuing deleting rest", exc);
                }
            }
        }

        //private string GetBaseDirectory(ContentData content)
        //{
        //    var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
        //    String fileAreaRoot = systemConfig.GetConfigParam("FileAreaRoot");
        //    Asset asset = content.Assets.FirstOrDefault<Asset>();

        //    String path = "";
        //    if (asset != null)
        //    {
        //        String directory = asset.Name.Replace(fileAreaRoot, "");
        //        int i = directory.IndexOf(@"\", 1);
        //        directory = directory.Remove(i);
        //        directory = fileAreaRoot + directory;
        //        path = directory;
        //    }
        //    else
        //    {
        //        log.Error("No asset on content that are being deleted, name= " + content.Name);
        //        throw new Exception("No asset on content that are being deleted, name= " + content.Name);
        //    }
        //    return path;
        //}

    }
}
