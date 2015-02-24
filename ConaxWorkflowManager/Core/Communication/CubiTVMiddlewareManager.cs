using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    public sealed class CubiTVMiddlewareManager
    {
        private static volatile Dictionary<UInt64, ICubiTVMWServiceWrapper> instance = new Dictionary<UInt64, ICubiTVMWServiceWrapper>();
        private static object syncRoot = new Object();
        //private CubiTVMiddlewareManager() { }

        public static ICubiTVMWServiceWrapper Instance(UInt64 serviceObjectId)
        {
            if (!instance.ContainsKey(serviceObjectId))
            {
                lock (syncRoot)
                {
                    if (!instance.ContainsKey(serviceObjectId))
                    {
                        var ServiceConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == serviceObjectId);
                        if (ServiceConfig != null)
                        {
                            if (ServiceConfig.ConfigParams.ContainsKey("CubiServiceWrapperAssembly"))
                            {
                                ICubiTVMWServiceWrapper newWrapper = (ICubiTVMWServiceWrapper)Activator.CreateInstance(ServiceConfig.GetConfigParam("CubiServiceWrapperAssembly"), ServiceConfig.GetConfigParam("CubiServiceWrapper")).Unwrap();
                                instance.Add(serviceObjectId, newWrapper);
                            }  else
                            {
                                ICubiTVMWServiceWrapper newWrapper = new CubiTVMiddlewareServiceWrapper(ServiceConfig);
                                instance.Add(serviceObjectId, newWrapper);
                            }
                        }
                    }
                }
            }

            return instance.FirstOrDefault(i => i.Key == serviceObjectId).Value;
        }

        public static void Clean()
        {
            instance = new Dictionary<UInt64, ICubiTVMWServiceWrapper>();
        }
    }
}
