using System;
using System.Collections.Generic;
using System.Xml;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;


namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class TaskConfig
    {
        private Dictionary<String, String> configParams = new Dictionary<string, string>();
        public string Task { get; set; }

        public TaskConfig(XmlNode taskNode)
        {
            this.Task = taskNode.Attributes["class"].Value;

            foreach (XmlNode configNode in taskNode.SelectNodes("ConfigParam"))
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
            catch (Exception ex) {
                throw;
            }
        }

        public String GetId()
        {
            if (!configParams.ContainsKey("Id") || String.IsNullOrEmpty(configParams["Id"]))
                return "";
            string id = configParams["Id"];
            return id;
        }

        public bool IsIgnoredWorkFlow(WorkFlowType workFlowType)
        {
            if (!configParams.ContainsKey("IgnoredWorkFlows") || String.IsNullOrEmpty(configParams["IgnoredWorkFlows"]))
                return false;
            string[] ignoredWorkFlows = configParams["IgnoredWorkFlows"].Split(',');
            foreach (string ignoredWorkFlow in ignoredWorkFlows)
            {
                if (ignoredWorkFlow.ToLower().Trim(' ').Equals(workFlowType.ToString().ToLower()))
                    return true;
            }
            return false;
        }

        public bool ShouldProcessWorkflow(WorkFlowType workFlowType)
        {
            if (!configParams.ContainsKey("WorkFlowsToProcess") || String.IsNullOrEmpty(configParams["WorkFlowsToProcess"]))
                return true;
            string[] workFlows = configParams["WorkFlowsToProcess"].Split(',');
            foreach (string workFlow in workFlows)
            {
                if (workFlow.ToLower().Trim(' ').Equals(workFlowType.ToString().ToLower()))
                    return true;
            }
            return false;
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
