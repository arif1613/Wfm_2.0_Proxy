using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration
{
    public class Mpp5Configuration : SystemConfig
    {
        public Mpp5Configuration(XmlNode systemConfigNode) : base(systemConfigNode) { }

        public String RestApiUrl
        {
            get
            {
                return this.GetConfigParam("RestApiUrl");
            }
        }

        public String RestLiveApiUrl
        {
            get
            {
                return this.GetConfigParam("RestLiveApiUrl");
            }
        }

        public String HolderID
        {
            get
            {
                return this.GetConfigParam("HolderID");
            }
        }
        public String ClientID
        {
            get
            {
                return this.GetConfigParam("ClientID");
            }
        }
        public String PrivateKey
        {
            get
            {
                return this.GetConfigParam("PrivateKey");
            }
        }
        public String UserName
        {
            get
            {
                return this.GetConfigParam("UserName");
            }
        }
        public String Password
        {
            get
            {
                return this.GetConfigParam("Password");
            }
        }
    }
}
