using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models
{
    public class EncodeModel
    {
        public String State { get; set; }
        public String CurrentTask { get; set; }
        public double TaskProgress { get; set; }
        public string ErrorMessage { get; set; }
    }
}
