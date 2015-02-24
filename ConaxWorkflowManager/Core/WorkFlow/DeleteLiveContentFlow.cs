using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow
{

    public class DeleteLiveContentFlow : BaseFlow
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /*
        public DeleteLiveContentFlow(TaskConfig contextConfig)
            : base(contextConfig) {}
        */
        public override RequestResult Process(RequestParameters requestParameters)
        {
 	        throw new NotImplementedException();
        }
    }
}
