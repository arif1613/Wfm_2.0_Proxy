using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public abstract class BaseIngestFileIngestHelper : BaseFileIngestHelper
    {
        public abstract Boolean MoveIngestFiles(String ingestXMLFileName, String fromDir, String toDir);
    }
}
