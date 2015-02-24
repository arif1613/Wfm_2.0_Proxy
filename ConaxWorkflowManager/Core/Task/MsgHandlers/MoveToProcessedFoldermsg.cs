using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers
{
    public class MoveToProcessedFoldermsg
    {
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        private readonly ConaxWorkflowManagerConfig _systemConfig;
        public MoveToProcessedFoldermsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            MoveFile();
        }

        private void MoveFile()
        {
            string xmlFilePath;
            try
            {
                xmlFilePath = _brokeredMessage.Properties["FileName"].ToString();
            }
            catch (Exception e)
            {
                throw e.InnerException;
            }
            if (!string.IsNullOrEmpty(xmlFilePath))
            {
                var fi = new FileInfo(xmlFilePath);
                string newFolderPath = fi.Directory.Name;
                string targetPath = _systemConfig.FileIngestProcessedDirectory + @"\" + newFolderPath;
                string newfilepath = Path.Combine(targetPath, fi.Name);
                var newfileinfo = new FileInfo(newfilepath);
                var systemConfig =
                (ConaxWorkflowManagerConfig)
                    Config.GetConfig()
                        .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }
                else
                {
                    if (File.Exists(newfilepath))
                    {
                        File.Delete(newfilepath);
                    }
                }
                //move xml to work folder
                if (File.Exists(xmlFilePath))
                {
                    Console.WriteLine(string.Format("{0} ({1}) moving to work folder has started......", fi.Name, fi.Directory.Name));
                    Thread.Sleep(10000);
                    try
                    {
                        var fileMoveProcess = new FileMoveProcess(xmlFilePath, newfilepath);
                        File.SetAttributes(newfileinfo.FullName, FileAttributes.Normal);
                        Console.WriteLine("File moving is finished.");
                        Console.WriteLine();

                    }
                    catch (Exception)
                    {
                        var ms = new MessageSender(null, "Move to processed folder", fi, _brokeredMessage);
                    }
                }
            }
        }
    }
}
