using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class WorkFlowConfig
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<KeyValuePair<String, Boolean>> handlers = new List<KeyValuePair<String, Boolean>>();
        public String WorkFlowName { get; private set; }

        public WorkFlowConfig(XmlNode workFlowConfigNode)
        {
            this.WorkFlowName = workFlowConfigNode.Attributes["name"].Value;

            foreach (XmlNode handlerNode in workFlowConfigNode.SelectNodes("Handler"))
            {
                Boolean enabled = true;
                if (handlerNode.Attributes["enabled"] != null) {
                    Boolean.TryParse(handlerNode.Attributes["enabled"].Value, out enabled);
                }
                handlers.Add(new KeyValuePair<String, Boolean>(handlerNode.Attributes["name"].Value, enabled));
            }
        }

        public List<KeyValuePair<String, Boolean>> Handlers
        {
            get
            {
                return handlers;
            }
            set
            {
                handlers = value;
            }
        }
    }
}
