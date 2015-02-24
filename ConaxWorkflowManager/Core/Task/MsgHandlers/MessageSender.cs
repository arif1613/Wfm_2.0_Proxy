using System;
using System.IO;
using Microsoft.ServiceBus.Messaging;
using NodaTime;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers
{
    public class MessageSender
    {
        private static string _errorMsg;
        private static string _cmdMessage;
        private static string _filename;
        private static BrokeredMessage _brokeredMessage;
        public MessageSender(string errorMsg, string messageToSend, FileInfo fileInfo, BrokeredMessage br1)
        {
            _errorMsg = errorMsg;
            _cmdMessage = messageToSend;
            _brokeredMessage = br1;

            if (fileInfo != null)
            {
                _filename = fileInfo.FullName;
                sendmessage();
            }
            else
            {
                if (errorMsg!=null)
                {
                    SendErrorMessage();
                }
                else
                {
                    SendFolderCheckingMessage();
                }
            }
        }
        private void SendFolderCheckingMessage()
        {
            string connectionString = "Endpoint=sb://mpswfm.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=NAtTBQ6YbyQ2tdRcYTrslNQTmuRId/IfafHHgXSFljU=";
            TopicClient msgSenderClient = TopicClient.CreateFromConnectionString(connectionString, "MpsMsgListener");
            try
            {
                var br = new BrokeredMessage
                {
                    CorrelationId = _brokeredMessage.CorrelationId,
                    Label = _errorMsg
                };
                br.CorrelationId = !string.IsNullOrEmpty(_brokeredMessage.CorrelationId) ? _brokeredMessage.CorrelationId : Guid.NewGuid().ToString();
                br.Properties.Add("CmdMessage", _cmdMessage);
                br.Properties.Add("CmdMsgId", Guid.NewGuid());
                Instant timestamp = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime());
                DateTime dt = timestamp.ToDateTimeUtc();
                br.Properties.Add("TimeStamp", dt);
                br.Properties.Add("CausationId", _brokeredMessage.Properties["CmdMsgId"]);
                //Console.WriteLine("timestamp:" + dt);
                Console.WriteLine(string.Format("Command Message:{0}", br.Properties["CmdMessage"]));
                Console.WriteLine(string.Format("Error Message:{0}", br.Properties["FileName"]));
                Console.WriteLine(string.Format("CmdMsgId:{0}", br.Properties["CmdMsgId"]));
                Console.WriteLine(string.Format("Correlation Id:{0}", br.CorrelationId));
                Console.WriteLine(string.Format("Causation Id:{0}", br.Properties["CausationId"]));
                Console.WriteLine("\n");
                msgSenderClient.SendAsync(br);
            }
            catch (Exception)
            {
                SendErrorMessage();
            }
        }
        private void sendmessage()
        {
            string connectionString = "Endpoint=sb://mpswfm.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=NAtTBQ6YbyQ2tdRcYTrslNQTmuRId/IfafHHgXSFljU=";
            TopicClient msgSenderClient = TopicClient.CreateFromConnectionString(connectionString, "MpsMsgListener");
            try
            {

                var br = new BrokeredMessage
                {
                    CorrelationId = _brokeredMessage.CorrelationId
                };
                if (!string.IsNullOrEmpty(_brokeredMessage.CorrelationId))
                {
                    br.CorrelationId = _brokeredMessage.CorrelationId;
                }
                else
                {
                    br.CorrelationId = Guid.NewGuid().ToString();
                }
                if (!_brokeredMessage.Properties.ContainsKey("ErrorMsg"))
                {
                    br.Properties.Add("ErrorMsg", _errorMsg);
                }
                else
                {
                    br.Properties.Add("ErrorMsg", _brokeredMessage.Properties["ErrorMsg"]);
                    Console.WriteLine(string.Format("Optional Message:{0}", br.Properties["ErrorMsg"]));
                }
                br.Properties.Add("FileName", _filename);
                br.Properties.Add("CmdMessage", _cmdMessage);
                br.Properties.Add("CmdMsgId", Guid.NewGuid());
                Instant timestamp = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime());
                DateTime dt = timestamp.ToDateTimeUtc();
                br.Properties.Add("TimeStamp", dt);
                br.Properties.Add("CausationId", _brokeredMessage.Properties["CmdMsgId"]);


                //Console.WriteLine("timestamp:" + dt);
                Console.WriteLine(string.Format("Command Message:{0}", br.Properties["CmdMessage"]));
                Console.WriteLine(string.Format("File Name:{0}", br.Properties["FileName"]));
                Console.WriteLine(string.Format("CmdMsgId:{0}", br.Properties["CmdMsgId"]));
                Console.WriteLine(string.Format("Correlation Id:{0}", br.CorrelationId));
                Console.WriteLine(string.Format("Causation Id:{0}", br.Properties["CausationId"]));
                Console.WriteLine("\n");
                msgSenderClient.SendAsync(br);
            }
            catch (Exception)
            {
                sendmessage();
            }
        }
        private void SendErrorMessage()
        {
            string connectionString = "Endpoint=sb://mpswfm.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=NAtTBQ6YbyQ2tdRcYTrslNQTmuRId/IfafHHgXSFljU=";
            TopicClient msgSenderClient = TopicClient.CreateFromConnectionString(connectionString, "MpsMsgListener");
            try
            {
                var br = new BrokeredMessage
                {
                    CorrelationId = _brokeredMessage.CorrelationId,
                    Label = _errorMsg
                };
                br.CorrelationId = !string.IsNullOrEmpty(_brokeredMessage.CorrelationId) ? _brokeredMessage.CorrelationId : Guid.NewGuid().ToString();
                br.Properties.Add("CmdMessage", _cmdMessage);
                br.Properties.Add("CmdMsgId", Guid.NewGuid());
                Instant timestamp = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime());
                DateTime dt = timestamp.ToDateTimeUtc();
                br.Properties.Add("TimeStamp", dt);
                br.Properties.Add("CausationId", _brokeredMessage.Properties["CmdMsgId"]);


                //Console.WriteLine("timestamp:" + dt);
                Console.WriteLine(string.Format("Command Message:{0}", br.Properties["CmdMessage"]));
                Console.WriteLine(string.Format("Error Message:{0}", br.Properties["FileName"]));
                Console.WriteLine(string.Format("CmdMsgId:{0}", br.Properties["CmdMsgId"]));
                Console.WriteLine(string.Format("Correlation Id:{0}", br.CorrelationId));
                Console.WriteLine(string.Format("Causation Id:{0}", br.Properties["CausationId"]));
                Console.WriteLine("\n");
                msgSenderClient.SendAsync(br);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException);
            }
        }
    }
}
