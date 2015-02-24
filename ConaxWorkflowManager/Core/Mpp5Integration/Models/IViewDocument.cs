using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NodaTime;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models
{
    public interface IViewDocument
    {
        Guid Id { get; set; }
        bool Deleted { get; set; }
        Guid HolderId { get; set; }
        IList<Guid> HandledMessages { get; set; }
        IDictionary<string, Instant> FieldChanges { get; set; }
        Instant LastChangeTime { get; set; }
    }
}
