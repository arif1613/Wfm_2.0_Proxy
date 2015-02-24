using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class ContentAgreement
    {
        public UInt64? ObjectID { get; set; }
        public String Name { get; set; }
        public ContentRightsOwner ContentRightsOwner { get; set; }
        public List<MultipleContentService> IncludedServices = new List<MultipleContentService>();
    }
}
