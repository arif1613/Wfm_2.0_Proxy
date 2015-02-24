using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{    
    public class RequestParameters
    {
        public RequestParameters() {
            HistoricalWorkFlowProcesses = new List<WorkFlowProcess>();
        }

        public WorkFlowType Action {get;set;}
        public TaskConfig Config {get;set;}
        public IList<WorkFlowProcess> HistoricalWorkFlowProcesses { get; set; }
        public WorkFlowProcess CurrentWorkFlowProcess { get; set; }

    }
}
