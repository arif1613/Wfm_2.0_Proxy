using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.FileOperations;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers
{
    public class MoveToWorkFoldermsg
    {
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        private readonly ConaxWorkflowManagerConfig _systemConfig;
        public MoveToWorkFoldermsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            MoveFile();
        }

        private void MoveFile()
        {
            var xmlFilePath = _brokeredMessage.Properties["FileName"].ToString();
            var fi=new FileInfo(xmlFilePath);
            Console.WriteLine(string.Format("{0} ({1}) moving to work folder has started......", fi.Name, fi.Directory.Name));
            try
            {
                FileMover fileMover = new FileMover(xmlFilePath, "work");
                Console.WriteLine("File moving is finished.");
                FileInfo newfileinfo = fileMover.getNewFileInfo();
                Thread.Sleep(5000);
               new MessageSender(null, "Create ContegoVODcontent And Encode", newfileinfo, _brokeredMessage);
            }
            catch (Exception)
            {
                if (fi.Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    new MessageSender(null, "Move To Work Folder", fi, _brokeredMessage);
                }
            }
        }
    }
}
