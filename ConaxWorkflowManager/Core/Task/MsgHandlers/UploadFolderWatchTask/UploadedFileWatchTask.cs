using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers.UploadFolderWatchTask
{
    public class UploadedFileWatchTask
    {
    
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly XmlDocument UploadFolderConfigDoc = new XmlDocument();
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        private readonly ConaxWorkflowManagerConfig _systemConfig;

        public UploadedFileWatchTask(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            String fileIngestUploadDirectoryConfig = systemConfig.FileIngestUploadDirectoryConfig;
            UploadFolderConfigDoc.Load(fileIngestUploadDirectoryConfig);
            IEnumerable<string> validUploadFolders = GetUploadFolderWithFolderconfig();
            if (validUploadFolders.Any())
            {
                foreach (var f in validUploadFolders)
                {
                    _brokeredMessage.Properties.Add("UploadFolderPath",f);
                    new MessageSender(null, "Find Complete XML", null,_brokeredMessage);
                }
            }
        }

        public void FindCompleteCrudxml(IEnumerable<string> uploadFolderPaths)
        {
            log.Debug("Find Complete CRUD XML");
            String folderSettingsFileName = _systemConfig.FolderSettingsFileName;

            var fileInfosList = new List<FileInfo>();
            foreach (var uploadFolderPath in uploadFolderPaths)
            {
                if (Directory.Exists(uploadFolderPath))
                {
                    string[] fileInformations = Directory.GetFiles(uploadFolderPath);
                    fileInfosList = fileInformations.Select(v => new FileInfo(v)).ToList();
                    var fileInfo =
                        fileInfosList.SingleOrDefault(
                            f =>
                                Path.GetFileName(f.Name)
                                    .Equals(folderSettingsFileName, StringComparison.OrdinalIgnoreCase));
                    if (fileInfo == null)
                    {
                        Console.WriteLine("Upload folder " + uploadFolderPath + " missing config file " +
                                          folderSettingsFileName);
                    }
                }
            }
        }

        private IEnumerable<string> GetUploadFolderPaths()
        {
            var folderPaths = new List<string>();
            var folderPathsToIgnore = new List<string>();

            XmlNodeList croNodes = UploadFolderConfigDoc.SelectNodes("UploadFolderConfig/ContentRightsOwner");

            foreach (XmlNode croNode in croNodes)
            {
                XmlNodeList uploadFolderNodes = croNode.SelectNodes("UploadFolders/UploadFolder");

                if (uploadFolderNodes != null)
                    log.Debug("CRO " + ((XmlElement)croNode).GetAttribute("name") + " have " + uploadFolderNodes.Count +
                              " upload folders.");

                foreach (XmlNode uploadFolderNode in uploadFolderNodes)
                {

                    String folderPath = uploadFolderNode.InnerText;
                    if (folderPaths.Contains(folderPath))
                    {
                        log.Warn("The upload folder path '" + folderPath +
                                 "' occurs more than once in the FileIngestUploadDirectoryConfig. This upload folder will be ignored.");
                        if (!folderPathsToIgnore.Contains(folderPath))
                            folderPathsToIgnore.Add(folderPath);
                    }
                    else
                        //getting ingest folder path
                        folderPaths.Add(folderPath);
                }
            }

            foreach (var folderPathToIgnore in folderPathsToIgnore)
            {
                folderPaths.RemoveAll(x => x == folderPathToIgnore);
            }

            return folderPaths;
        }

        private IEnumerable<string> GetUploadFolderWithFolderconfig()
        {
            IEnumerable<string> PrimaryUploadFolders = GetUploadFolderPaths();
            String folderSettingsFileName = _systemConfig.FolderSettingsFileName;
            List<string> ValidUploadFolderList=new List<string>();
            foreach (var uploadFolderPath in PrimaryUploadFolders)
            {
                if (Directory.Exists(uploadFolderPath))
                {
                    string FolderConfigFilePath = Path.Combine(uploadFolderPath, folderSettingsFileName);
                    if (File.Exists(FolderConfigFilePath))
                    {
                        ValidUploadFolderList.Add(FolderConfigFilePath);
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("No folderSettings file found in {0}", uploadFolderPath));
                }
            }
            return ValidUploadFolderList;
        }
    }
}
