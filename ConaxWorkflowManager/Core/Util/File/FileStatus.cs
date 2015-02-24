using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public enum FileStatus
    {
        Undefined = 0,
        Exists = 1,
        Missing = 2,
        PendingDelete = 3,
        Delete = 4,
        NoValue = 5,
        ChangedSize
    }
}
