using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.XML
{
    public class ChannelIngestHandler : BaseStorageIngestHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public ChannelIngestHandler(IFileHandler fileHandler)
            : base(fileHandler) { }

        public override IngestItem GetCompleteCRUDIngest(IngestConfig ingestConfig, IngestItem ingestItem)
        {
            ContentData content = ingestItem.contentData;
            FileInfo fi = new FileInfo(ingestItem.OriginalIngestXMLPath);

            // check if all assets is in place.
            if (ingestItem.contentData.Assets.Count == 0)
            {
                log.Warn("Assets is missing for the " + ingestItem.OriginalIngestXMLPath + " ingest XML. this ingest will not be triggered untill the Asset is set in the XML.");
                return null;
            }

            // add default image if needed.
            foreach(LanguageInfo langinfo in ingestItem.contentData.LanguageInfos) {
                if (langinfo.Images.Count == 0) {
                    String assetPath = Path.Combine(fi.Directory.FullName, ingestConfig.DefaultImageFileName);
                    if (!FileHandler.IsFileExclusive(assetPath))
                    {
                        log.Warn("default Image file " + assetPath + " is missing or busy, ingest will not be triggered until the file is ready.");
                        return null;
                    }
                    String newImage = fi.Name.Replace(fi.Extension, "") + "_" + ingestConfig.DefaultImageFileName;
                    String newAssetPath = Path.Combine(fi.Directory.FullName, newImage);
                    FileHandler.CopyTo(assetPath, newAssetPath);
                  
                    Image img = new Image();

                    var property = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName"));
                    if (property != null)
                        img.URI = Path.Combine(Path.GetDirectoryName(property.Value), newImage);
                    else
                        img.URI = newImage;

                    img.ClientGUIName = ingestConfig.DefaultImageClientGUIName;
                    img.Classification = ingestConfig.DefaultImageClassification;

                    langinfo.Images.Add(img);
                }
            }

            // check iamges
            XmlDocument doc = new XmlDocument();
            doc = CommonUtil.LoadXML(ingestItem.OriginalIngestXMLPath);
            XmlNodeList imgNodes = doc.SelectNodes("//BoxCover");
            foreach (XmlElement imgNode in imgNodes)
            {
                String file = imgNode.InnerText;
                if (!String.IsNullOrWhiteSpace(file))
                {
                    String assetPath = Path.Combine(fi.Directory.FullName, file);
                    if (!FileHandler.IsFileExclusive(assetPath))
                    {
                        log.Warn("Image file " + assetPath + " is missing or busy, ingest will not be triggered until the file is ready.");
                        return null;
                    }
                }
            }
            return ingestItem;
        }

        public override IngestItem InitIngestItem(IngestConfig ingestConfig, FileInformation fileInformation, String ingestIdentifier)
        {
            IngestItem ingestItem = new IngestItem();

            XmlDocument orgXml = new XmlDocument();
            orgXml = CommonUtil.LoadXML(fileInformation.Path);
            ingestItem.OriginalIngestXML = orgXml;
            ingestItem.OriginalIngestXMLPath = fileInformation.Path;


            // Parese cablelabs xml to content object 
            ChannelXmlTranslator translator = new ChannelXmlTranslator();
            ContentData content = translator.TranslateXmlToContentData(ingestConfig, ingestItem.OriginalIngestXML);
            content.Properties.Add(new Property("IngestIdentifier", ingestIdentifier));
            CommonUtil.AddPublishInfoToContent(content, PublishState.Created);
            List<ContentAgreement> agreements = mppWrapper.GetAllServicesForContent(content);
            Dictionary<MultipleContentService, List<MultipleServicePrice>> prices = translator.TranslateXmlToPrices(ingestConfig, agreements[0].IncludedServices, ingestItem.OriginalIngestXML, content.Name);

            ingestItem.Type = IngestType.AddContent;
            ingestItem.contentData = content;
            ingestItem.MultipleServicePrices = prices;

            return ingestItem;
        }
    }
}
