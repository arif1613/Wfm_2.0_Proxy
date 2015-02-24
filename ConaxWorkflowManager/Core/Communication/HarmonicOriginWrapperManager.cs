using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Harmonic;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    public sealed class HarmonicOriginWrapperManager
    {

        private static volatile IHarmonicOriginWrapper instance = null;
        private static object syncRoot = new Object();


        private HarmonicOriginWrapperManager() { }

        public static IHarmonicOriginWrapper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            var systemConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "HarmonicOrigin");

                            if (systemConfig != null &&
                                systemConfig.ConfigParams.ContainsKey("HarmonicServiceWrapperAssembly"))
                            {
                                instance = (IHarmonicOriginWrapper)Activator.CreateInstance(systemConfig.GetConfigParam("HarmonicServiceWrapperAssembly"), systemConfig.GetConfigParam("HarmonicServiceWrapper")).Unwrap();
                            } else
                            {
                                instance = new HarmonicOriginWrapper();    
                            }                            
                        }
                    }
                }

                return instance;
            }
        }

    }
}
