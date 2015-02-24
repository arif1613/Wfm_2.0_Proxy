using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.FileOperations;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers
{
    public class ChannelFileMovingmsg
    {
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        private readonly ConaxWorkflowManagerConfig _systemConfig;

        public ChannelFileMovingmsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            var systemConfig =
                (ConaxWorkflowManagerConfig)
                    Config.GetConfig()
                        .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            MoveFile();
        }
        private void MoveFile()
        {
            string xmlFilePath = _brokeredMessage.Properties["FileName"].ToString();
            if (!string.IsNullOrEmpty(xmlFilePath))
            {
                var fi = new FileInfo(xmlFilePath);
                Console.WriteLine("Channel File Moving started....");
                try
                {
                    new FileMover(xmlFilePath, "work");
                    Console.WriteLine("File Moving finished");


                }
                catch (Exception e)
                {
                    if (new FileInfo(xmlFilePath).Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        new MessageSender(null, "Move Channel Files To Work Folder", fi, _brokeredMessage);
                    }
                    else
                    {
                        Console.WriteLine(e.InnerException);
                    }
                }
                
            }
        }
    }
}
