using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class ServiceViewMatchRule
    {
        public String serviceViewName { get; set; }
        public UInt64? serviceViewObjectId { get; set; }
        public String Region { get; set; }
        public String ServiceViewLanugageISO { get; set; }
    }
}
