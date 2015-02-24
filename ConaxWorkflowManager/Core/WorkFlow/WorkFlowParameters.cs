using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow
{
    public class WorkFlowParameters
    {
        public WorkFlowParameters()
        {
            MultipleContentServices = new List<MultipleContentService>();
        }

        public String Basket { get; set; }
        public ContentData Content { get; set; }
        //public List<KeyValuePair<MultipleContentService, List<MultipleServicePrice>>> MultipleServicePrices { get; set; }
        public List<MultipleContentService> MultipleContentServices { get; set; }
        
    }
}
