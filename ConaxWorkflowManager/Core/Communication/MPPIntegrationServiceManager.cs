using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    public sealed class MPPIntegrationServiceManager
    {
        private static volatile MPPIntegrationServicesWrapper instanceWithActiveEvent = null;
        private static volatile MPPIntegrationServicesWrapper instanceWithPassiveEvent = null;
        private static object syncRoot = new Object();
        private static object syncRoot2 = new Object();

        private MPPIntegrationServiceManager() { }

        public static MPPIntegrationServicesWrapper InstanceWithActiveEvent
        {
            get
            {
                if (instanceWithActiveEvent == null)
                {
                    lock (syncRoot)
                    {
                        if (instanceWithActiveEvent == null)
                        {
                            var systemConfig = (MPPConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.MPP);
                            instanceWithActiveEvent = new MPPIntegrationServicesWrapper(systemConfig.AccountIdForActiveEvent);
                        }
                    }
                }

                return instanceWithActiveEvent;
            }
        }

        public static MPPIntegrationServicesWrapper InstanceWithPassiveEvent
        {
            get
            {
                if (instanceWithPassiveEvent == null)
                {
                    lock (syncRoot2)
                    {
                        if (instanceWithPassiveEvent == null)
                        {
                            var systemConfig = (MPPConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.MPP);
                            instanceWithPassiveEvent = new MPPIntegrationServicesWrapper(systemConfig.AccountIdForPassiveEvent);
                        }
                    }
                }

                return instanceWithPassiveEvent;
            }
        }

    }
}
