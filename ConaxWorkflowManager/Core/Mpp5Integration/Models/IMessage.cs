using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NodaTime;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models
{
    public interface IMessage
    {
        Guid CausationId { get; set; }
        Guid MessageId { get; set; }
        Guid CorrelationId { get; set; }
        Instant Timestamp { get; set; }
    }
}
