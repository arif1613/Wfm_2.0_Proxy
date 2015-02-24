using System;
using System.Collections.Generic;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.SFAnytime;
using WFMProxy.Models;
using File = System.IO.File;

namespace WFMProxy.Controllers
{
    public class ReadMediaInfo
    {
        public static XmlDocument IngestXml;
        public ReadMediaInfo()
        {

        }
        public ReadMediaInfo(string ingestXmlPath)
        {
            if (File.Exists(ingestXmlPath))
            {
                var xd = new XmlDocument();
                xd.Load(ingestXmlPath);
                IngestXml = xd;
                }
            
        }
        public List<MediaInfos> Getmediainfos()
        {

            List<MediaInfos> li = new List<MediaInfos>();
            XmlNodeList croNodes = IngestXml.SelectNodes("ADI/Asset/Asset");

            foreach (XmlElement adNode in IngestXml.SelectNodes("ADI/Asset/Asset"))
            {
                XmlElement typeNode = (XmlElement)adNode.SelectSingleNode("Metadata/App_Data[@Name='Type']");
                if (typeNode == null)
                    continue;
                if (typeNode.GetAttribute("Value").Equals("movie", StringComparison.OrdinalIgnoreCase))
                {
                    string fileType = typeNode.GetAttribute("Value");
                    XmlElement AMSNode = (XmlElement)adNode.SelectSingleNode("Content");
                    string fileName = AMSNode.GetAttribute("Value");
                    if (fileName != null)
                    {
                        li.Add(new MediaInfos
                        {
                            FileType = fileType,
                            FileName = fileName
                        });
                    }
                }
                else if (typeNode.GetAttribute("Value").Equals("preview", StringComparison.OrdinalIgnoreCase))
                {
                    string fileType = typeNode.GetAttribute("Value");
                    XmlElement AMSNode = (XmlElement)adNode.SelectSingleNode("Content");
                    string fileName = AMSNode.GetAttribute("Value");
                    if (fileName != null)
                    {
                        li.Add(new MediaInfos
                        {
                            FileType = fileType,
                            FileName = fileName
                        });
                    }
                }
                else
                {
                    string fileType = typeNode.GetAttribute("Value");
                    XmlElement AMSNode = (XmlElement)adNode.SelectSingleNode("Content");
                    string fileName = AMSNode.GetAttribute("Value");
                    if (fileName != null)
                    {
                        li.Add(new MediaInfos
                        {
                            FileType = fileType,
                            FileName = fileName
                        });
                    }
                    
                }
            }

            return li;
        }
        
    }
}
