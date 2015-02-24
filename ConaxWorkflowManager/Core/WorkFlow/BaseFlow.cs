using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow
{
    public abstract class BaseFlow
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /*
        protected TaskConfig _ContextConfig;

        public BaseFlow(TaskConfig contextConfig)
        {
            _ContextConfig = contextConfig;            
        }
        */
        private List<ResponsibilityHandler> handlers = new List<ResponsibilityHandler>();

        public BaseFlow() {
            var WorkFlowConfig = Config.GetConfig().WorkFlowConfigs.Where(c => c.WorkFlowName.Equals(this.GetType().Name, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();

            if (WorkFlowConfig == null) {
                log.Warn("No handler defined for " + this.GetType().Name + " in the config xml.");
                return;
            }

            foreach(KeyValuePair<String, Boolean> kvp in WorkFlowConfig.Handlers) {
                try
                {
                    if (kvp.Value)
                    {  // load handler
                        ResponsibilityHandler handler = Activator.CreateInstance(System.Type.GetType(kvp.Key)) as ResponsibilityHandler;
                        var lasthandler = handlers.LastOrDefault();
                        if (lasthandler != null)
                            lasthandler.SetSuccessor(handler);
                        handlers.Add(handler);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("failed to load handler " + kvp.Key + " for workflow " + this.GetType().Name, ex);
                    throw;
                }
            }
        }

        protected virtual RequestResult HandleRequest(RequestParameters requestParameters)
        {
            var handler = handlers.FirstOrDefault();
            if (handler == null) {
                log.Warn("Workflow " + this.GetType().Name + " doesn't have any handler defined.");
                return new RequestResult(Util.Enums.RequestResultState.Successful);
            }
            return handler.HandleRequest(requestParameters);
        }

        public virtual RequestResult Process(RequestParameters requestParameters)
        {
            RequestResult result = HandleRequest(requestParameters);
            return result;
        }
    }
}
