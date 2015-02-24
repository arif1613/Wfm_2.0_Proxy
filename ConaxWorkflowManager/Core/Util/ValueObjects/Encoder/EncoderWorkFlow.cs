using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder
{
    public class EncoderWorkFlow
    {

        public String WorkFlowGuid { get; set; }

        public List<IncludedWorkFlow> WorkFlow = new List<IncludedWorkFlow>();

        public String Path { get; set; }
    }
}
