using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class Image
    {
        public String ClientGUIName { get; set; }
        public String Classification { get; set; }
        public String URI { get; set; }
        public Boolean? IsActive { get; set; }
    }
}

