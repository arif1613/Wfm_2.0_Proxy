using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models
{
    public interface ICommand : IMessage
    {
        Guid Id { get; set; }
    }
}
