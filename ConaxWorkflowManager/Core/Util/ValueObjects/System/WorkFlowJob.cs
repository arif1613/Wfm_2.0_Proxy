using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.System
{
    public class WorkFlowJob
    {
        public UInt64? Id { get; set; }
        public UInt64 SourceId { get; set; }
        public EventType Type { get; set; }
        public Object Message { get; set; }
        public String MessageType { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime NotUntil { get; set; }
        public WorkFlowJobState State { get; set; }
    }
}
