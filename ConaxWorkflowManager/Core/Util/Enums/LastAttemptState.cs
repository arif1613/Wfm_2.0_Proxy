using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums
{
    public enum LastAttemptState
    {        
        Unknown,        // Initial state.        
        Failed,         // failed to call servcie 
        Succeeded,       // successfully call servcie
    }
}
