using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.IO;
using System.Xml;
using log4net;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public class CableLabsFileIngestHelper : BaseIngestFileIngestHelper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Boolean MoveIngestFiles(ContentData content, String fromDir, String toDir)
        {
            
            var filesToMove = new List<string>();
            
        try
            {
                log.Debug("Start copy files from folder " + fromDir + " to folder " + toDir);
                // copy files to work folder
                var ingestXMLFileNameProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(VODnLiveContentProperties.IngestXMLFileName, StringComparison.OrdinalIgnoreCase));
                var xmlFile = ingestXMLFileNameProperty.Value.Substring(ingestXMLFileNameProperty.Value.LastIndexOf(@"\") + 1);    
            // copy xml
                filesToMove.Add(xmlFile);


                //// copy asset and trailer if VOD
                //if (!CommonUtil.ContentIsChannel(content))
                //{
                foreach (Asset va in content.Assets)
                {
                    var fileName = va.Name.Substring(va.Name.LastIndexOf(@"\")+1);
                    if (!filesToMove.Contains(fileName))
                    {
                        filesToMove.Add(fileName);
                    }
                }
                //}

                // copy images
                foreach (LanguageInfo lang in content.LanguageInfos)
                {
                    
                    foreach (Image image in lang.Images)
                    {
                        var fileName = image.URI.Substring(image.URI.LastIndexOf(@"\") + 1);
                        if (!filesToMove.Contains(fileName))
                            filesToMove.Add(fileName);
                    }
                }

                return MoveIngestFiles(filesToMove, fromDir, toDir);
            }
            catch (Exception ex)
            {
                log.Warn("Error when moving files", ex);
                // remove already copied files from work folder
                return false;
            }

        }

        public override Boolean MoveIngestFiles(String ingestXMLFileName, String fromDir, String toDir)
        {
            String ingestXMLfullName = Path.Combine(fromDir, ingestXMLFileName);
            XmlDocument doc = new XmlDocument();

            List<String> files = new List<String>();
            files.Add(ingestXMLFileName);
            try
            {
                doc = CommonUtil.LoadXML(ingestXMLfullName);
                //doc.Load(ingestXMLfullName);
            } catch (Exception ex) {
                // move ingest XML Only.    
                return MoveIngestFiles(files, fromDir, toDir);
            }

            try
            {
                XmlNodeList assetNodes = doc.SelectNodes("ADI/Asset/Asset/Content");
                foreach (XmlElement assetNode in assetNodes)
                {
                    string file = assetNode.GetAttribute("Value");
                    if (!string.IsNullOrEmpty(file) && !file.StartsWith("http://"))
                    {
                        files.Add(file);
                    }
                }
            } catch (Exception ex) {}

            return MoveIngestFiles(files, fromDir, toDir);
            //CableLabsXmlTranslator translator = new CableLabsXmlTranslator();
            //ContentData content = translator.TranslateXmlToContentData(doc);

            //return MoveIngestFiles(content, fromDir, toDir);
        }

    }
}
