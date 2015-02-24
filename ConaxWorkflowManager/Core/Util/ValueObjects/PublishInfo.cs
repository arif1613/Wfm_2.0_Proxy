using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class PublishInfo
    {
        public DateTime from { get; set; }
        public DateTime to { get; set; }
        public String Region { get; set; }
        public PublishState PublishState { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
    }
}
