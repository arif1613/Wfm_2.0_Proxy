using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Unified;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    public sealed class UnifiedServicesWrapperManager
    {
        private static volatile IUnifiedServicesWrapper instance = null;
        private static object syncRoot = new Object();


        private UnifiedServicesWrapperManager() { }

        public static IUnifiedServicesWrapper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            var systemConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "Unified");

                            if (systemConfig != null &&
                                systemConfig.ConfigParams.ContainsKey("UnifiedServiceWrapperAssembly"))
                            {
                                instance = (IUnifiedServicesWrapper)Activator.CreateInstance(systemConfig.GetConfigParam("UnifiedServiceWrapperAssembly"), systemConfig.GetConfigParam("UnifiedServiceWrapper")).Unwrap();
                            } else
                            {
                                instance = new UnifiedServicesWrapper();    
                            }                            
                        }
                    }
                }

                return instance;
            }
        }
    }
}
