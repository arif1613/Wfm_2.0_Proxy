using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.System
{
    public class PublishEvent
    {
        public UInt64 RelatedObjectId { get; set; }
        public UInt64 ServiceObjectId { get; set; }        
    }
}
