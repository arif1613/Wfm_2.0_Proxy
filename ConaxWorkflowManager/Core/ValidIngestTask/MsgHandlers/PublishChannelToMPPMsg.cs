using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ServiceBus.Messaging;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.MsgHandlers
{
    public class PublishChannelToMPPMsg
    {
        private static BrokeredMessage _brokeredMessage;
        private static DateTime _dt;

        public PublishChannelToMPPMsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
        }
    }
}
