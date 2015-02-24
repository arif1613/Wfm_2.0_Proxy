using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.FileOperations
{
    class FileCopier
    {
        private static FileInfo _sourceFileInfo;
        private static string _destinationConfigFolder;

        public FileCopier(string DestinationconfigFolder)
        {
            _destinationConfigFolder = DestinationconfigFolder;
        }
        public FileCopier(string sourceFilePath, string DestinationconfigFolder)
        {
            if (!string.IsNullOrEmpty(sourceFilePath))
            {
                var sourcefileinfo = new FileInfo(sourceFilePath);
                _destinationConfigFolder = DestinationconfigFolder;
                _sourceFileInfo = sourcefileinfo;
            }

            try
            {
                CreateDir();
            }
            catch (Exception e)
            {
            }

        }

        public void CreateDir()
        {
            string dirname = null;

            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            if (_destinationConfigFolder == "work")
            {
                dirname = Path.Combine(systemConfig.FileIngestWorkDirectory, _sourceFileInfo.Directory.Name);
            }
            if (_destinationConfigFolder == "reject")
            {
                dirname = Path.Combine(systemConfig.FileIngestRejectDirectory, _sourceFileInfo.Directory.Name);
            }


            if (!Directory.Exists(dirname))
            {
                Directory.CreateDirectory(dirname);
            }
            CreateFile(Path.Combine(dirname, _sourceFileInfo.Name));

        }

        public void CreateFile(string filename)
        {
            if (!File.Exists(filename))
            {
                _sourceFileInfo.CopyTo(filename);
            }
        }
    }
}
