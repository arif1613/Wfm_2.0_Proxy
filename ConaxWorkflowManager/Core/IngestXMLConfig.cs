using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class IngestXMLConfig
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Dictionary<String, String> configParams = new Dictionary<String, String>();
        public String IngestXMLType { get; private set; }

        public IngestXMLConfig(XmlNode ingestXMLConfigNode)
        {
            this.IngestXMLType = ingestXMLConfigNode.Attributes["ingestXMLType"].Value;

            foreach (XmlNode configNode in ingestXMLConfigNode.SelectNodes("ConfigParam"))
            {
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
            set {
                configParams = value;
            }
        }


        /// <summary>
        /// Defines which Ingest parse implementation to use.
        /// Used by:
        ///     VOD and Live ingest
        /// </summary>
        public String IngestHandler {
            get {
                return this.GetConfigParam("IngestHandler");
            }
        }

        /// <summary>
        /// Defines which XSD to use to valide the ignest xml with.
        /// Used by:
        ///     VOD and Live ingst.
        /// </summary>
        public String XSD {
            get {
                return this.GetConfigParam("XSD");
            }
        }

        /// <summary>
        /// Defines which file ingest helper implementation to use.
        /// Used by:
        ///     VOD and Channel.
        /// </summary>
        public String FileIngestHelper {
            get {
                return this.GetConfigParam("FileIngestHelper");
            }
        }        
    }
}
