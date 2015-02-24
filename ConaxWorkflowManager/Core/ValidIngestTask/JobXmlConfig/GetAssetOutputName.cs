using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.ServiceBus.Notifications;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.JobXmlFile
{
    class GetAssetOutputName
    {
        private static string _jobXmlfilename;
        private static string _assetName;
        private static XmlDocument _xmlDocument { get; set; }

        public GetAssetOutputName(string jobxmlfilename,string assetname)
        {
            _assetName = assetname;
            _jobXmlfilename = jobxmlfilename;
        }

        public GetAssetOutputName(XmlDocument xd, string assetname)
        {
            _assetName = assetname;
            _xmlDocument = xd;
        }

        public List<string> GetAssetList()
        {
            var fileNames=new List<string>();
            var extensionList=new List<string>();
            var encoderConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
            String encoderOutFolder = encoderConfig.GetConfigParam("EncoderMappedFileAreaRoot");
            string[] s = _assetName.Split('.');
            string newAssetname = null;
            for (int i = 0; i < s.Length-1; i++)
            {
                newAssetname = newAssetname + s[i];
            }
            var xmlReader = new XmlTextReader(_jobXmlfilename);
            while (xmlReader.Read())
            {
                String encodedFilePath = Path.Combine(encoderOutFolder, newAssetname);           
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == "name_modifier")
                        {
                            encodedFilePath = encodedFilePath  + xmlReader.ReadInnerXml();
                            fileNames.Add(encodedFilePath);
                           
                        }
                        break;
                }
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == "extension")
                        {
                            extensionList.Add(xmlReader.ReadInnerXml());
                        }
                        break;
                }
            }

            for(int i=0;i<fileNames.Count;i++)
            {
                fileNames[i] = fileNames[i] + "." + extensionList[i];
            }
            return fileNames;
        }

        public List<string> GetAssetListForMPP()
        {
            var fileNames = new List<string>();
            var extensionList = new List<string>();
            var encoderConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
            String encoderOutFolder = encoderConfig.GetConfigParam("EncoderMappedFileAreaRoot");
            string[] s = _assetName.Split('.');
            string newAssetname = null;
            for (int i = 0; i < s.Length - 1; i++)
            {
                newAssetname = newAssetname + s[i];
            }
            var xmlReader = new XmlNodeReader(_xmlDocument);
            while (xmlReader.Read())
            {
                String encodedFilePath = Path.Combine(encoderOutFolder, newAssetname);
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == "name_modifier")
                        {
                            encodedFilePath = encodedFilePath + xmlReader.ReadInnerXml();
                            fileNames.Add(encodedFilePath);

                        }
                        break;
                }
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == "extension")
                        {
                            extensionList.Add(xmlReader.ReadInnerXml());
                        }
                        break;
                }
            }

            for (int i = 0; i < fileNames.Count; i++)
            {
                fileNames[i] = fileNames[i] + "." + extensionList[i];
            }
            return fileNames;
        }
    }
}
