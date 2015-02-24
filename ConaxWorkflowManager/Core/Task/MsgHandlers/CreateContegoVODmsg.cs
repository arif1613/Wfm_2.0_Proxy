using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.EncoderTask;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.FileOperations;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.XmlOperations;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Data;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.MediaInfo;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using MessageSender = MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers.MessageSender;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class CreateContegoVODmsg
    {
        private static XmlDocument EncoderJobXml { get; set; }
        private static ContentData ConaxVodContentData { get; set; }
        private static BrokeredMessage _brokeredMessage;
        private readonly ConaxWorkflowManagerConfig _systemConfig;
        private static MPPConfig _mppConfig;
        private static IngestConfig IngestConfig { get; set; }
        private string XmlFilename { get; set; }
        private static DateTime Dt { get; set; }
        private static List<Asset> _assetsList { get; set; }

        public CreateContegoVODmsg(string xmlFile)
        {
            XmlFilename = xmlFile;
            var systemConfig =
                (ConaxWorkflowManagerConfig)
                    Config.GetConfig()
                        .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            var mppConfig =
                (MPPConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.MPP);
            _mppConfig = mppConfig;
        }
        public CreateContegoVODmsg(BrokeredMessage br, DateTime dt)
        {
            XmlFilename = br.Properties["FileName"].ToString();
            _brokeredMessage = br;
            var systemConfig =
                (ConaxWorkflowManagerConfig)
                    Config.GetConfig()
                        .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            var mppConfig =
                (MPPConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.MPP);
            _mppConfig = mppConfig;
            Dt = dt;
        }
        public void CreateContegoVOD()
        {
            string xmlFilePath = _brokeredMessage.Properties["FileName"].ToString();
            var xmlfileInfo = new FileInfo(xmlFilePath);
            var encoderConfig =
                (ElementalEncoderConfig)
                    Config.GetConfig()
                        .SystemConfigs.FirstOrDefault(r => r.SystemName == SystemConfigNames.ElementalEncoder);
            string encoderJobXmlFileAreaRoot = encoderConfig.EncoderJobXmlFileAreaRoot;
            try
            {
                ConaxVodContentData = GetContentData();
                var ca = new CheckAssetsInWorkFolder(ConaxVodContentData, _brokeredMessage);

                if (ca.checkAssetInWorkFolder())
                {
                    createPublishingDir();

                    foreach (var x in ConaxVodContentData.Assets.Select(r => r.Name).Distinct())
                    {
                        Asset asset = ConaxVodContentData.Assets.Where(r => r.Name == x).FirstOrDefault();
                        MediaFileInfo mi = getMediaFileInfo(asset);

                        string filename = Path.Combine(_systemConfig.FileIngestWorkDirectory, asset.Name);

                        if (mi != null)
                        {
                            sendMediaFilesToEncoderUploadFolder(asset);



                            #region create jobxml file
                            var fi = new FileInfo(filename);
                            //var createdir = new CreateDirectory(Path.Combine(EncoderJobXmlFileAreaRoot, fi.Directory.Name));

                            if (!Directory.Exists(Path.Combine(encoderJobXmlFileAreaRoot, fi.Directory.Name)))
                            {
                                Directory.CreateDirectory(Path.Combine(encoderJobXmlFileAreaRoot, fi.Directory.Name));
                            }
                            string[] s = asset.Name.Split('.');
                            string newAssetname = null;
                            for (int i = 0; i < s.Length - 1; i++)
                            {
                                newAssetname = newAssetname + s[i];
                                string jobXmlFile = Path.Combine(encoderJobXmlFileAreaRoot, newAssetname + ".xml");
                                if (!File.Exists(jobXmlFile))
                                {
                                    File.Create(jobXmlFile);
                                }
                            }

                            #endregion
                            Console.WriteLine("JobXml will be sent to encoder for encoding informations for {0} ",asset.Name);
                            Console.WriteLine();
                        }
                    }

                    #region writing in mpp5


                    var createMpp5VodContent = new CreateMpp5Content(ConaxVodContentData, _brokeredMessage);
                    if (createMpp5VodContent.createA_VodinMPP())
                    {
                        Console.WriteLine("Successful vod creation for id: " + ConaxVodContentData.Mpp5_Id);

                    }
                    else
                    {
                        Console.WriteLine("Failed to create vod for id: " + ConaxVodContentData.Mpp5_Id);
                        new MessageSender(null, "Publish Vod to MPP5", xmlfileInfo, _brokeredMessage);
                    }

                    #endregion

                }
                else
                {
                    var checkXml = new CheckXmlFile(xmlfileInfo);
                    List<string> assList = new List<string>();
                    if (checkXml.totalAssestsinXml() != null)
                    {
                        assList = checkXml.totalAssestsinXml();
                    }

                    //String workFolder = xmlfileInfo.DirectoryName;
                    string Uploaddirectory = _systemConfig.FileIngestUploadDirectory;

                    foreach (var x in assList)
                    {
                        var assetFilepath = new FileInfo(Path.Combine(Uploaddirectory, x));
                        new FileMover(assetFilepath.FullName, "work");
                    }
                }
            }
            catch (Exception e)
            {
                var checkXml = new CheckXmlFile(xmlfileInfo);
                List<string> assList = new List<string>();
                if (checkXml.totalAssestsinXml() != null)
                {
                    assList = checkXml.totalAssestsinXml();
                }
                
                
                Console.WriteLine("media file error: "+e.InnerException);

                String workFolder = _systemConfig.FileIngestWorkDirectory;
                new MessageSender(e.Message, "Move To Reject Folder", xmlfileInfo, _brokeredMessage);
                foreach (var x in assList)
                {
                    var assetFileInfo = Path.Combine(workFolder, x);
                    new FileMover(assetFileInfo, "reject");
                }
            }
        }
        private void sendMediaFilesToEncoderUploadFolder(Asset asset)
        {
            string workFolder = _systemConfig.FileIngestWorkDirectory;
          
                string mediaFileName = Path.Combine(workFolder, asset.Name);
                new FileMover(mediaFileName, "encoderUpload");
        }
        private MediaFileInfo getMediaFileInfo(Asset asset)
        {

            string filename = Path.Combine(_systemConfig.FileIngestWorkDirectory, asset.Name);
            return MediaInfoHelper.GetMediaInfoForFile(filename);
        }
        private void createPublishingDir()
        {
            string contentRightsOwner = ConaxVodContentData.ContentRightsOwner.Name;
            foreach (var x in ConaxVodContentData.ContentAgreements)
            {
                string contentAgreement = x.Name;
                string publishingDir = null;
                string enableQA = ConaxVodContentData.Properties.FirstOrDefault(r => r.Type == "EnableQA").Value;
                if (enableQA == "True" || enableQA == "true")
                {
                    publishingDir = _systemConfig.NeedQAPublishDir;
                }
                else
                {
                    publishingDir = _systemConfig.DirectPublishDir;
                }
                string dirname = Path.Combine(publishingDir, contentRightsOwner, contentAgreement);
                if (!Directory.Exists(dirname))
                {
                    Directory.CreateDirectory(dirname);
                }

            }
        }
        public ContentData GetContentData()
        {
            ContentData contentData = null;
            var xmlfileInfo = new FileInfo(XmlFilename);
            string folderSettingsFileName = Path.Combine(xmlfileInfo.DirectoryName, _systemConfig.FolderSettingsFileName);
            var configfileInfo = new FileInfo(folderSettingsFileName);

            //ingestconfig
            var ingestConfig = GetIngestConfig(configfileInfo);
            IngestConfig = ingestConfig;

            var gi = new CreateContegoVodContent(ingestConfig, XmlFilename);
            contentData = gi.GenerateVodContent();
            return contentData;
        }
        public IngestConfig GetIngestConfig(FileInfo folderSettings)
        {
            var ingestConfig = new IngestConfig();

            ingestConfig.HostID = _mppConfig.HostID;
            //ingestConfig.CAS = mppConfig.DefaultCAS;

            var folderConfigDoc = new XmlDocument();
            folderConfigDoc.Load(folderSettings.FullName);

            var ingestSettingsNode = (XmlElement)folderConfigDoc.SelectSingleNode("FolderConfiguration/IngestSettings");
            ingestConfig.EnableQA = Boolean.Parse(ingestSettingsNode.GetAttribute("enableQA"));

            var URIProfileNode = (XmlElement)folderConfigDoc.SelectSingleNode("FolderConfiguration/URIProfile");
            ingestConfig.URIProfile = URIProfileNode.InnerText;

            var PricingRuleNode = (XmlElement)folderConfigDoc.SelectSingleNode("FolderConfiguration/PricingRule");
            if (PricingRuleNode != null && !String.IsNullOrWhiteSpace(PricingRuleNode.InnerText))
                ingestConfig.ADIPricingRule = (ADIPricingRuleType)Enum.Parse(typeof(ADIPricingRuleType), PricingRuleNode.InnerText, true);

            XmlNodeList ingestXMLTypeNodes = folderConfigDoc.SelectNodes("FolderConfiguration/IngestXMLTypes/IngestXMLType");
            foreach (XmlNode ingestXMLTypeNode in ingestXMLTypeNodes)
                ingestConfig.IngestXMLTypes.Add(ingestXMLTypeNode.InnerText);

            XmlNode cronode = folderConfigDoc.SelectSingleNode("FolderConfiguration/ContentRightsOwner");
            ingestConfig.ContentRightsOwner = cronode.InnerText;

            XmlNode caNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/ContentAgreement");
            ingestConfig.ContentAgreement = caNode.InnerText;

            XmlNode diNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/DefaultImageFileName");
            if (diNode != null)
                ingestConfig.DefaultImageFileName = diNode.InnerText;

            ingestConfig.DefaultImageClientGUIName = _mppConfig.DefaultImageClientGUIName ?? null;
            ingestConfig.DefaultImageClassification = _mppConfig.DefaultImageClassification ?? null;


            XmlNodeList eviceNodes = folderConfigDoc.SelectNodes("FolderConfiguration/Devices/Device");
            foreach (XmlNode eviceNode in eviceNodes)
                ingestConfig.Devices.Add(eviceNode.InnerText);

            XmlNode metadataMappingConfigurationFileNameNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataMappingConfigurationFileName");
            ingestConfig.MetadataMappingConfigurationFileName = metadataMappingConfigurationFileNameNode.InnerText;

            XmlNode defaultRatingTypeNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/DefaultRatingType");
            ingestConfig.DefaultRatingType = defaultRatingTypeNode != null
                                                 ? defaultRatingTypeNode.InnerText
                                                 : VODnLiveContentProperties.MovieRating;

            //add metadata default values.

            XmlNode movieRatingNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataDefaultValues/" + VODnLiveContentProperties.MovieRating);
            if (movieRatingNode != null && !String.IsNullOrEmpty(movieRatingNode.InnerText))
                ingestConfig.MetaDataDefaultValues.Add(VODnLiveContentProperties.MovieRating, movieRatingNode.InnerText);

            XmlNode tvRatingNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataDefaultValues/" + VODnLiveContentProperties.TVRating);
            if (tvRatingNode != null && !String.IsNullOrEmpty(tvRatingNode.InnerText))
                ingestConfig.MetaDataDefaultValues.Add(VODnLiveContentProperties.TVRating, tvRatingNode.InnerText);

            XmlNode genreNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataDefaultValues/Genre");
            if (genreNode != null && !String.IsNullOrEmpty(genreNode.InnerText))
                ingestConfig.MetaDataDefaultValues.Add(VODnLiveContentProperties.Genre, genreNode.InnerText);

            XmlNode categoryNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataDefaultValues/Category");
            if (categoryNode != null && !String.IsNullOrEmpty(categoryNode.InnerText))
                ingestConfig.MetaDataDefaultValues.Add(VODnLiveContentProperties.Category, categoryNode.InnerText);

            // add default prices
            XmlElement defaultRentalPriceNode = (XmlElement)folderConfigDoc.SelectSingleNode("FolderConfiguration/DefaultConfigurationForAllServices/Prices/RentalPrice");
            if (defaultRentalPriceNode != null)
            {
                MultipleServicePrice defaultPrice = new MultipleServicePrice();
                defaultPrice.Price = DataParseHelper.ParsePrice((defaultRentalPriceNode.GetAttribute("amount")));
                defaultPrice.Currency = defaultRentalPriceNode.GetAttribute("currency");
                defaultPrice.ContentLicensePeriodLength = Int64.Parse(defaultRentalPriceNode.GetAttribute("periodLengthInhrs"));
                defaultPrice.IsRecurringPurchase = false;
                ingestConfig.DefaultServicePrices.Add("*", defaultPrice);
            }
            XmlNodeList CategoryNodes = folderConfigDoc.SelectNodes("FolderConfiguration/DefaultConfigurationForAllServices/Prices/Categories/Category");
            foreach (XmlElement CategoryNode in CategoryNodes)
            {
                XmlElement rentalPriceNode = (XmlElement)CategoryNode.SelectSingleNode("RentalPrice");
                if (rentalPriceNode != null)
                {
                    MultipleServicePrice defaultPrice = new MultipleServicePrice();
                    defaultPrice.Price = DataParseHelper.ParsePrice(rentalPriceNode.GetAttribute("amount"));
                    defaultPrice.Currency = rentalPriceNode.GetAttribute("currency");
                    defaultPrice.ContentLicensePeriodLength = Int64.Parse(rentalPriceNode.GetAttribute("periodLengthInhrs"));
                    defaultPrice.IsRecurringPurchase = false;
                    ingestConfig.DefaultServicePrices.Add(CategoryNode.GetAttribute("name"), defaultPrice);
                }
            }
            // add service prices
            XmlNodeList serviceNodes = folderConfigDoc.SelectNodes("FolderConfiguration/ConfigurationForServices/Service");
            foreach (XmlElement serviceNode in serviceNodes)
            {
                MultipleContentService service = new MultipleContentService();
                service.ObjectID = UInt64.Parse(serviceNode.GetAttribute("objectId"));
                List<MultipleServicePrice> prices = new List<MultipleServicePrice>();

                XmlNodeList subscriptionPriceNodes = serviceNode.SelectNodes("Prices/SubscriptionPrice");
                foreach (XmlElement subscriptionPriceNode in subscriptionPriceNodes)
                {
                    MultipleServicePrice subPrice = new MultipleServicePrice();
                    subPrice.ID = UInt64.Parse(subscriptionPriceNode.GetAttribute("id"));
                    subPrice.IsRecurringPurchase = true;
                    prices.Add(subPrice);
                }
                XmlElement rentalPriceNode = (XmlElement)serviceNode.SelectSingleNode("Prices/RentalPrice");
                if (rentalPriceNode != null)
                {
                    MultipleServicePrice rentPrice = new MultipleServicePrice();
                    rentPrice.Price = DataParseHelper.ParsePrice(rentalPriceNode.GetAttribute("amount"));
                    rentPrice.Currency = rentalPriceNode.GetAttribute("currency");
                    rentPrice.ContentLicensePeriodLength = Int64.Parse(rentalPriceNode.GetAttribute("periodLengthInhrs"));
                    rentPrice.IsRecurringPurchase = false;
                    prices.Add(rentPrice);
                }
                if (prices.Count > 0)
                { // if any price to add.
                    PriceMatchParameter pmp = new PriceMatchParameter();
                    pmp.Service = service;
                    pmp.Category = "*";
                    ingestConfig.ServicePrices.Add(pmp, prices);
                }

                // load category prices
                XmlNodeList serviceCategoryNodes = serviceNode.SelectNodes("Prices/Categories/Category");
                foreach (XmlElement serviceCategoryNode in serviceCategoryNodes)
                {
                    prices = new List<MultipleServicePrice>();
                    XmlNodeList categorySubscriptionPriceNodes = serviceCategoryNode.SelectNodes("SubscriptionPrice");
                    foreach (XmlElement categorySubscriptionPriceNode in categorySubscriptionPriceNodes)
                    {
                        MultipleServicePrice subPrice = new MultipleServicePrice();
                        subPrice.ID = UInt64.Parse(categorySubscriptionPriceNode.GetAttribute("id"));
                        subPrice.IsRecurringPurchase = true;
                        prices.Add(subPrice);
                    }
                    XmlElement categoryRentalPriceNode = (XmlElement)serviceCategoryNode.SelectSingleNode("RentalPrice");
                    if (categoryRentalPriceNode != null)
                    {
                        MultipleServicePrice categoryRentPrice = new MultipleServicePrice();
                        categoryRentPrice.Price = DataParseHelper.ParsePrice(categoryRentalPriceNode.GetAttribute("amount"));
                        categoryRentPrice.Currency = categoryRentalPriceNode.GetAttribute("currency");
                        categoryRentPrice.ContentLicensePeriodLength = Int64.Parse(categoryRentalPriceNode.GetAttribute("periodLengthInhrs"));
                        categoryRentPrice.IsRecurringPurchase = false;
                        prices.Add(categoryRentPrice);
                    }
                    PriceMatchParameter categorypmp = new PriceMatchParameter();
                    categorypmp.Service = service;
                    categorypmp.Category = serviceCategoryNode.GetAttribute("name");
                    ingestConfig.ServicePrices.Add(categorypmp, prices);
                }
            }
            return ingestConfig;
        }
    }
}
