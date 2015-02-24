using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers
{
    public class MoveToRejectFoldermsg
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        private ConaxWorkflowManagerConfig _systemConfig;
        private static IngestItem ingestItem;
        public MoveToRejectFoldermsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            System.Threading.Tasks.Task t=new System.Threading.Tasks.Task(MoveFile);
            t.Start();
            t.Wait();
        }
        private void MoveFile()
        {
            string xmlFilePath=null;
            try
            {
                xmlFilePath = _brokeredMessage.Properties["FileName"].ToString();
            }
            catch (Exception e)
            {
                if (new FileInfo(xmlFilePath).Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    new MessageSender(e.Message, "Move To Reject Folder", null, _brokeredMessage);
                }
                else
                {
                    Console.WriteLine(e.InnerException);
                }
            }

            FileInfo fi = null;
            if (!string.IsNullOrEmpty(xmlFilePath))
            {
                fi = new FileInfo(xmlFilePath);
            }
            else
            {
                if (new FileInfo(xmlFilePath).Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    new MessageSender(null, "Move To Reject Folder", null, _brokeredMessage);
                }
            }
            string newFolderPath = fi.Directory.Name;
            string targetPath = _systemConfig.FileIngestRejectDirectory + @"\" + newFolderPath;
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            else
            {
                if (File.Exists(Path.Combine(targetPath, fi.Name)))
                {
                    File.Delete(Path.Combine(targetPath, fi.Name));
                }
            }
            if (File.Exists(xmlFilePath))
            {
                if (!File.Exists(Path.Combine(targetPath, fi.Name)))
                {
                    Console.WriteLine(fi.Name + " moving to reject folder has started......");
                    try
                    {
                        fi.MoveTo(Path.Combine(targetPath, fi.Name));
                        File.SetAttributes(Path.Combine(targetPath, fi.Name), FileAttributes.Normal);
                        File.Delete(xmlFilePath);
                        Thread.Sleep(5000);
                        Console.WriteLine("File moving is finished.");
                        Console.WriteLine();
                    }
                    catch (Exception e)
                    {
                        if (new FileInfo(xmlFilePath).Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            new MessageSender(e.Message, "Move To Reject Folder", fi, _brokeredMessage);
                        }
                        Thread.Sleep(5000);

                    }
                }
                else
                {
                    File.Delete(xmlFilePath);
                    Thread.Sleep(5000);
                }
            }
        }
    }
}
