using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow
{
    public class WorkFlowProcess
    {
        public UInt64 Id { get; set; }
        public UInt64 WorkFlowJobId { get; set; }
        public String MethodName { get; set; }
        public WorkFlowProcessState State { get; set; }
        public DateTime TimeStamp { get; set; }
        public WorkFlowParameters WorkFlowParameters { get; set; }
        public String Message { get; set; }
    }
}
