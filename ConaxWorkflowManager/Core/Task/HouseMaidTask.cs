using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class HouseMaidTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            // clear DB
            DateTime toDatetime = DateTime.Now;
            try
            {
                toDatetime = toDatetime.AddDays(-1 * Double.Parse(systemConfig.GetConfigParam("CleanOldDBDataInDay")));
            }
            catch (Exception ex)
            {
                toDatetime = toDatetime.AddDays(-30);
            }
            log.Debug("Clean DB to " + toDatetime.ToString("yyyy-MM-dd HH:mm:ss"));
            DBManager.Instance.CleanDB(toDatetime);
            DBManager.Instance.ClearAndDefragDB();
            // delete old files in processed and reject folders
            DateTime deleteFrom = DateTime.Now;
            try
            {
                deleteFrom = deleteFrom.AddDays(-1 * Double.Parse(systemConfig.GetConfigParam("CleanUploadsOlderThanDays")));
            }
            catch (Exception e)
            {
                deleteFrom = deleteFrom.AddDays(-30);
                log.Debug("Unable to use CleanUploadsOlderThanDays from config, will use 30. " + e.Message);
            }

            log.Debug("Delete files that were uploaded before " + deleteFrom.ToString("yyyy-MM-dd HH:mm:ss"));

            IFileHandler fileHandler = Activator.CreateInstance(System.Type.GetType(systemConfig.GetConfigParam("FileIngestHandlerType"))) as IFileHandler;

            string processedFolder = systemConfig.GetConfigParam("FileIngestProcessedDirectory");
            string rejectFolder = systemConfig.GetConfigParam("FileIngestRejectDirectory");

            DeleteOldFiles(fileHandler, processedFolder, deleteFrom);
            DeleteOldFiles(fileHandler, rejectFolder, deleteFrom);

            log.Debug("DoExecute End");
        }

        private void DeleteOldFiles(IFileHandler fh, string folder, DateTime olderThan)
        {
           
            var filesAndFolders = fh.ListDirectory(folder, true);
            var folders = filesAndFolders.Where<FileInformation>(f => f.IsDirectory);
            var files = filesAndFolders.Where<FileInformation>(f => !f.IsDirectory);

            foreach (FileInformation fi in folders)
            {
                log.Debug("Checking folder" + fi.Path);
                DeleteOldFiles(fh, fi.Path, olderThan);
            }

            foreach (var item in files.Where(f => f.lastAccess < olderThan))
            {
                log.Debug("Deleting file " + item.Path);
                fh.DeleteFile(item.Path);
            }
        }
    }
}
