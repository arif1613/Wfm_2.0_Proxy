using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.FileOperations;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers
{
    public class MoveAssetsToEncoderUploadFoldermsg
    {
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        public MoveAssetsToEncoderUploadFoldermsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            var t = new System.Threading.Tasks.Task(MoveFile);
            t.Start();
            t.Wait();
        }
        private void MoveFile()
        {
            string xmlFilePath = _brokeredMessage.Properties["FileName"].ToString();
            try
            {
                FileMover fileMover = new FileMover(xmlFilePath, "encoderUpload");

            }
            catch (Exception e)
            {
                new MessageSender(e.Message, "Move To Encoder Upload Folder", new FileInfo(xmlFilePath), _brokeredMessage);

            }
          
        }
    }
}
