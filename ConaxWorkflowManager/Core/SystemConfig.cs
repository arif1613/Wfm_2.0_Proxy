using System;
using System.Collections.Generic;
using System.Xml;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class SystemConfig
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Dictionary<String, String> configParams = new Dictionary<string, string>();
        public String SystemName { get; private set; }

        public SystemConfig(XmlNode systemConfigNode)
        {
            this.SystemName = systemConfigNode.Attributes["name"].Value;

            foreach (XmlNode configNode in systemConfigNode.SelectNodes("ConfigParam"))
            {
                if (configParams.ContainsKey(configNode.Attributes["key"].Value))
                {
                    throw new ApplicationException("duplicate of key " + configNode.Attributes["key"].Value +
                                                   ", please correct it in the workflow manager configuration xml.");
                }
                configParams.Add(configNode.Attributes["key"].Value, configNode.Attributes["value"].Value);
            }
        }

        public String GetConfigParam(String key)
        {
            try
            {
                return configParams[key];
            }
            catch (Exception ex)
            {
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

    public class SystemConfigNames
    {
        public const String ConaxWorkflowManager = "ConaxWorkflowManager";
        public const String MPP = "MPP";
        public const String ElementalEncoder = "ElementalEncoder";
        public const String MPP5 = "MPP5";
    }
}
