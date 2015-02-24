using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using System.IO;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public class BaseFileIngestHelper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Boolean ReMoveFiles(List<String> files, String fromDir) {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            IFileHandler fileHandler = Activator.CreateInstance(System.Type.GetType(systemConfig.GetConfigParam("FileIngestHandlerType"))) as IFileHandler;

            // delete files from upload folder            
            log.Debug("start delete files from upload folder");
            foreach (String file in files) 
            {
                fileHandler.DeleteFile(Path.Combine(fromDir, file));
            }
            log.Debug("all files deleted successfully from folder " + fromDir);

            return true;
        }

        public Boolean CopyIngestFiles(List<String> files, String fromDir, String toDir) {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            IFileHandler fileHandler = Activator.CreateInstance(System.Type.GetType(systemConfig.GetConfigParam("FileIngestHandlerType"))) as IFileHandler;

            Dictionary<String, String> fileList = new Dictionary<String, String>();
            try
            {
                log.Debug("Start copy files from folder " + fromDir + " to folder " + toDir);
                // copy files to work folder
                foreach (String file in files) {
                    String fileFrom = Path.Combine(fromDir, file);
                    String fileTo = Path.Combine(toDir, file);
                    fileHandler.CopyTo(fileFrom, fileTo);
                    fileList.Add(fileFrom, fileTo);
                }
                log.Debug("all files copied successfully to folder " + toDir);
            }
            catch (Exception ex)
            {
                log.Warn("failed to copy files to folder " + toDir + ", will skip this ingest", ex);
                // remove already copied files from work folder
                foreach (KeyValuePair<String, String> kvp in fileList)
                {
                    fileHandler.DeleteFile(kvp.Value);
                }
                return false;
            }
            return true;
        }

        public Boolean MoveIngestFiles(List<String> files, String fromDir, String toDir)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            IFileHandler fileHandler = Activator.CreateInstance(System.Type.GetType(systemConfig.GetConfigParam("FileIngestHandlerType"))) as IFileHandler;

            Dictionary<String, String> fileList = new Dictionary<String, String>();
            try
            {
                log.Debug("Start copy files from folder " + fromDir + " to folder " + toDir);
                // copy files to work folder
                foreach (String file in files)
                {
                    String fileFrom = Path.Combine(fromDir, file);
                    String fileTo = Path.Combine(toDir, file);
                    log.Debug("Trying to move file " + file);
                    fileHandler.MoveTo(fileFrom, fileTo);
                    fileList.Add(fileFrom, fileTo);
                }
                log.Debug("all files copied successfully to folder " + toDir);
            }
            catch (Exception ex)
            {
                log.Warn("failed to move files to folder " + toDir + ", will skip this ingest", ex);
                // remove already copied files from work folder
                foreach (KeyValuePair<String, String> kvp in fileList)
                {
                    try
                    {
                        fileHandler.MoveTo(kvp.Value, kvp.Key);
                    }
                    catch (Exception exc)
                    {
                        log.Error("Error when moving back already copied files to original folder " + kvp.Key, exc);
                    }
                }
                return false;
            }

            return true;
        }

       
    }
}
