using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using MessageSender = MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers.MessageSender;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.FolderCleanUpTask
{
    public class RejectFolderCleanUp
    {
        private static BrokeredMessage _br;
        private static DateTime _dt;
        private static string _rejectFolderPath;

        public RejectFolderCleanUp()
        {
        }

        public RejectFolderCleanUp(BrokeredMessage br, DateTime dt)
        {
            _br = br;
            _dt = dt;
            var systemConfig =
         (ConaxWorkflowManagerConfig)
             Config.GetConfig()
                 .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _rejectFolderPath = systemConfig.FileIngestRejectDirectory;
            getRejectFolderSubDirectories();
        }

        public void getRejectFolderSubDirectories()
        {
            var rejectFolderSubDirectories = Directory.GetDirectories(_rejectFolderPath).ToList();
            string filetype = string.Format("*.xml", StringComparison.OrdinalIgnoreCase);
            foreach (var subd in rejectFolderSubDirectories)
            {
                List<string> xmlFileInformations = Directory.GetFiles(subd, filetype).ToList();
                if (xmlFileInformations.Any())
                {
                    new MessageSender(null, "Delete Files from RejectFolder sub directory", new FileInfo(xmlFileInformations.FirstOrDefault()), _br);
                }
            }
        }
    }
}
