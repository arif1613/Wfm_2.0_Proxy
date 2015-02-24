using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.Archive
{
    public interface IArchiveOrder
    {

        void ArchiveAssets(ContentData content, List<UInt64> serviceobjectId, DateTime startTime, DateTime endTime);
    }
}
