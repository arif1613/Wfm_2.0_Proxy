using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class ServiceConfig
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Dictionary<String, String> configParams = new Dictionary<String, String>();
        public UInt64 ServiceObjectId { get; private set; }

        public ServiceConfig(XmlNode serviceConfigNode)
        {
            this.ServiceObjectId = UInt64.Parse(serviceConfigNode.Attributes["objectId"].Value);

            foreach (XmlNode configNode in serviceConfigNode.SelectNodes("ConfigParam"))
            {
                if (configParams.ContainsKey(configNode.Attributes["key"].Value))
                    throw new ApplicationException("duplicate of key " + configNode.Attributes["key"].Value + ", please correct it in the workflow manager configuration xml.");

                configParams.Add(configNode.Attributes["key"].Value, configNode.Attributes["value"].Value);
            }
        }

        public String GetConfigParam(String key)
        {
            try
            {
                return configParams[key];
            } catch (Exception ex) {
                log.Warn("Parameter " + key + " could not be found.");
                throw;
            }
        }

        public Dictionary<String, String> ConfigParams
        {
            get
            {
                return configParams;
            }
            set
            {
                configParams = value;
            }
        }
    }
}
