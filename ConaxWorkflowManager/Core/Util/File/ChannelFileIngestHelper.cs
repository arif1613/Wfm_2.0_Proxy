using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public class ChannelFileIngestHelper : BaseIngestFileIngestHelper
    {
        public override bool MoveIngestFiles(String ingestXMLFileName, String fromDir, String toDir)
        {
            String ingestXMLfullName = Path.Combine(fromDir, ingestXMLFileName);
            XmlDocument doc = new XmlDocument();

            List<String> files = new List<String>();
            files.Add(ingestXMLFileName);
            try
            {
                doc = CommonUtil.LoadXML(ingestXMLfullName);
            }
            catch (Exception ex)
            {
                // move ingest XML Only.    
                return MoveIngestFiles(files, fromDir, toDir);
            }

            try
            {
                XmlNodeList imgNodes = doc.SelectNodes("//BoxCover");
                foreach (XmlElement imgNode in imgNodes)
                {
                    String file = imgNode.InnerText;
                    if (!String.IsNullOrWhiteSpace(file) && !files.Contains(file))
                    {
                        files.Add(file);
                    }
                }
            }
            catch (Exception ex) { }

            return MoveIngestFiles(files, fromDir, toDir);
        }
    }
}
