using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class MPPStationServerEvent
    {

        public UInt64 ObjectId { get; set; }
        public DateTime Occurred { get; set; }
        public EventType Type { get; set; }
        public UInt64 RelatedPersistentObjectId { get; set; }
        public String RelatedPersistentClassName { get; set; }
        public UInt64 UserId { get; set; }
        public WorkFlowJobState State { get; set; }
        
    }
}
