using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums
{
    public enum TitanJobStatus
    {
        waiting,
        pending,
        starting,
        encoding,
        paused,
        pause_user,
        resuming,
        complete,aborted,
        invalid,
        probing
    }
}
