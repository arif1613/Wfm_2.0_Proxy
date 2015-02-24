using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database
{
    public sealed class DBManager
    {
        private static volatile IDBWrapper instance;
        private static object syncRoot = new Object();

        private DBManager() { }

        public static IDBWrapper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);

                            if (!String.IsNullOrWhiteSpace(systemConfig.DBWrapperAssembly))                            
                                instance = (IDBWrapper)Activator.CreateInstance(systemConfig.DBWrapperAssembly, systemConfig.DBWrapper).Unwrap();
                            else
                                instance = Activator.CreateInstance(System.Type.GetType(systemConfig.DBWrapper)) as IDBWrapper;
                        }
                    }
                }

                return instance;
            }
        }
    }
}
