using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration
{
    class ElementalEncoderConfig : SystemConfig
    {
        public ElementalEncoderConfig(XmlNode systemConfigNode) : base(systemConfigNode) { }

        public String EncoderUploadFolder
        {
            get
            {
                return this.GetConfigParam("EncoderUploadFolder");
            }
        }
        public String EncoderMappedFilePath
        {
            get
            {
                return this.GetConfigParam("EncoderMappedFilePath");
            }
        }
        public String EncoderMappedFileAreaRoot
        {
            get
            {
                return this.GetConfigParam("EncoderMappedFileAreaRoot");
            }
        }
        public String EncoderJobXmlFileAreaRoot
        {
            get
            {
                return this.GetConfigParam("EncoderJobXmlFileAreaRoot");
            }
        }

        public String ElementalEncoderOutFolder
        {
            get
            {
                return this.GetConfigParam("EncoderOutFolder");
            }
        }
    }
}
