using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder
{
    public class IncludedWorkFlow
    {
        public String WorkFlowGuid { get; set; }

        public List<String> Parameters { get; set; }

        public WorkTypes WorkType { get; set; }

        public Boolean UseTempFolderForOutput { get; set; }

        public Boolean UseParametersFromPreviousJob { get; set; }

        public String TempFolder { get; set; }

        public String Name { get; set; }

        public int JobOrder { get; set; }
    }
}
