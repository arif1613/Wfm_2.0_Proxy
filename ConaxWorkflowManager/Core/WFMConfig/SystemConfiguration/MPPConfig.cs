using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration
{
    public class MPPConfig : SystemConfig
    {
        public MPPConfig(XmlNode systemConfigNode) : base(systemConfigNode) { }

        /// <summary>
        /// Defines which MPPP this WFM communicate with.
        /// Used by:
        ///     VOD and Live ingst
        /// </summary>
        public String HostID {
            get {
                return this.GetConfigParam("HostID");
            }
        }

        /// <summary>
        /// Defines the default CAS to use.
        /// Used by:
        ///     VOD and Live ingest.
        /// </summary>
        //public String DefaultCAS {
        //    get {
        //        return this.GetConfigParam("DefaultCAS");
        //    }
        //}

        /// <summary>
        /// Defines default image ClientGUI to use.
        /// Used by:
        ///     VOD and Live ingest.
        /// </summary>
        public String DefaultImageClientGUIName {
            get {
                return this.GetConfigParam("DefaultImageClientGUIName");
            }
        }

        /// <summary>
        /// Defines default image Classification to use.
        /// Used by:
        ///     VOD and Live ignest.
        /// </summary>
        public String DefaultImageClassification {
            get {
                return this.GetConfigParam("DefaultImageClassification");
            }
        }

        /// <summary>
        /// Mpp User account id, this user will be used when need to trigger MPP events that WFM should be take action on later.
        /// </summary>
        public String AccountIdForActiveEvent
        {
            get {
                return this.GetConfigParam("AccountIdForActiveEvent");
            }
        }

        /// <summary>
        /// Mpp User account id, this user will be used when NO need to trigger MPP events that WFM should be take action on later.
        /// </summary>
        public String AccountIdForPassiveEvent
        {
            get
            {
                return this.GetConfigParam("AccountIdForPassiveEvent");
            }    
        }
        
        /// <summary>
        /// Defines which MPP servcie implementaion to use.
        /// Default is the "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.MPP.MPPIntegrationService"
        /// </summary>
        public String MPPService
        {
            get
            {
                if (this.ConfigParams.ContainsKey("MPPService"))
                    return this.GetConfigParam("MPPService");
                else
                    return "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.MPP.MPPIntegrationService";
            }
        }

        /// <summary>
        /// Defines if the reply from the server should be zipped.
        /// </summary>
        public bool ZipReply
        {
            get
            {
                if (ConfigParams.ContainsKey("ZipReply"))
                {
                    bool zipReply = true;
                    if (bool.TryParse(this.GetConfigParam("ZipReply"), out zipReply))
                        return zipReply;
                    return false;

                }
                return false;
            }
        }

        /// <summary>
        /// Defines the timeout in seconds
        /// </summary>
        public int TimeOut
        {
            get
            {
                if (ConfigParams.ContainsKey("TimeOut"))
                {
                    int timeout = 120000;
                    if (int.TryParse(this.GetConfigParam("TimeOut"), out timeout))
                    {
                        return timeout * 1000;
                    }
                    return 120000;

                }
                return 120000;
            }
        }

        /// <summary>
        /// Defiens which assembly the MPP servcie implementation is.
        /// if not defined, default will look into same assembly.
        /// </summary>
        public String MPPServiceAssembly
        {
            get
            {
                if (this.ConfigParams.ContainsKey("MPPServiceAssembly"))
                    return this.GetConfigParam("MPPServiceAssembly");
                else
                    return String.Empty;
            }
        }

        /// <summary>
        /// Defines the default currency value to use for ingest.
        /// </summary>
        public String DefaultCurrency
        {
            get
            {
                return this.GetConfigParam("DefaultCurrency");
            }
        }

    }
}
