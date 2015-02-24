using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.XML
{
    public class CableLabsIngestHandler : BaseStorageIngestHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public CableLabsIngestHandler(IFileHandler fileHandler)
            : base(fileHandler) { }

        #region IStorageIngestHandler Members

        public override IngestItem GetCompleteCRUDIngest(IngestConfig ingestConfig, IngestItem ingestItem)
        {
            //IngestItem ingestItem = new IngestItem();

            //XmlDocument orgXml = new XmlDocument();
            //orgXml = CommonUtil.LoadXML(fileInformation.Path);
            //ingestItem.OriginalIngestXML = orgXml;
            //ingestItem.OriginalIngestXMLPath = fileInformation.Path;


            //// Parese cablelabs xml to content object 
            //CableLabsXmlTranslator translator = new CableLabsXmlTranslator();
            //ContentData content = translator.TranslateXmlToContentData(ingestConfig, ingestItem.OriginalIngestXML);
            //content.Properties.Add(new Property("IngestIdentifier", ingestIdentifier));
            //CommonUtil.AddPublishInfoToContent(content, PublishState.Created);
            //List<ContentAgreement> agreements = mppWrapper.GetAllServicesForContent(content);
            //Dictionary<MultipleContentService, List<MultipleServicePrice>> prices = translator.TranslateXmlToPrices(ingestConfig, agreements[0].IncludedServices, ingestItem.OriginalIngestXML, content.Name);

            // only Add content via xml ingest I guess.
            // add dispacther if there is more then add content usecase for xml ingest.
            //ingestItem.Type = IngestType.AddContent;
            //ingestItem.contentData = content;
            //ingestItem.MultipleServicePrices = prices;

            ContentData content = ingestItem.contentData;
            FileInfo fi = new FileInfo(ingestItem.OriginalIngestXMLPath);

            // check if all assets is in place.
            if (ingestItem.contentData.Assets.Count == 0)
            {
                log.Warn("Assets is missing for the " + ingestItem.OriginalIngestXMLPath + " ingest XML. this ingest will not be triggered untill the Asset is set in the XML.");
                return null;
            }

            // check asset files if VOD
            if (ConaxIntegrationHelper.GetContentType(content) == ContentType.VOD)
            {

                foreach (Asset asset in ingestItem.contentData.Assets)
                {
                    String assetPath = Path.Combine(fi.Directory.FullName, Path.GetFileName(asset.Name));
                    if (!FileHandler.IsFileExclusive(assetPath))
                    {
                        log.Warn("Media file " + assetPath + " is missing or busy, ingest will not be triggered until the file is ready.");
                        return null;
                    }
                }
            }
            
            // add default image if needed.
            Int32 imageCount = ingestItem.contentData.LanguageInfos.Count(l => l.Images.Count > 0);
            if (imageCount == 0) {
                String assetPath = Path.Combine(fi.Directory.FullName, ingestConfig.DefaultImageFileName);
                if (!FileHandler.IsFileExclusive(assetPath)) {
                    log.Warn("default Image file " + assetPath + " is missing or busy, ingest will not be triggered until the file is ready.");
                    return null;
                }
                String newImage = fi.Name.Replace(fi.Extension, "") + "_" + ingestConfig.DefaultImageFileName;
                String newAssetPath = Path.Combine(fi.Directory.FullName, newImage);
                FileHandler.CopyTo(assetPath, newAssetPath);
                foreach (LanguageInfo iang in ingestItem.contentData.LanguageInfos) {

                    Image img = new Image();

                    var property = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName"));
                    if (property != null)
                        img.URI = Path.Combine(Path.GetDirectoryName(property.Value), newImage);
                    else
                        img.URI = newImage;

                    img.ClientGUIName = ingestConfig.DefaultImageClientGUIName;
                    img.Classification = ingestConfig.DefaultImageClassification;

                    iang.Images.Add(img);
                }
            }


            // check iamges
            foreach(LanguageInfo iang in ingestItem.contentData.LanguageInfos) {
                foreach(Image img in iang.Images) {
                    String assetPath = Path.Combine(fi.Directory.FullName, Path.GetFileName(img.URI));
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
            CableLabsXmlTranslator translator = new CableLabsXmlTranslator();
            ContentData content = translator.TranslateXmlToContentData(ingestConfig, ingestItem.OriginalIngestXML);
            content.Properties.Add(new Property("IngestIdentifier", ingestIdentifier));
            CommonUtil.AddPublishInfoToContent(content, PublishState.Created);
            List<ContentAgreement> agreements = mppWrapper.GetAllServicesForContent(content);
            
            if(agreements.Count == 0)
            {
                var ca = content.ContentAgreements.Count > 0 ? content.ContentAgreements[0].Name : "";
                var cro = content.ContentRightsOwner != null ? content.ContentRightsOwner.Name : "";
                var errorMessage = "Could not find a content agreement for content " + content.Name + ", which matches the configuration ContentRightsOwner: " +cro + " and ContentAgreement: " + ca;
                log.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            Dictionary<MultipleContentService, List<MultipleServicePrice>> prices = translator.TranslateXmlToPrices(ingestConfig, agreements[0].IncludedServices, ingestItem.OriginalIngestXML, content.Name);
           
            // only Add content via xml ingest I guess.
            // add dispacther if there is more then add content usecase for xml ingest.
            ingestItem.Type = IngestType.AddContent;
            ingestItem.contentData = content;
            ingestItem.MultipleServicePrices = prices;

            return ingestItem;
        }

        #endregion
    }
}
