using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Envivio;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class UpdatePropertiesForContentParameter
    {
        public ContentData Content { get; set; }

        public List<KeyValuePair<String, Property>> Properties  = new List<KeyValuePair<string, Property>>();
    }
}
