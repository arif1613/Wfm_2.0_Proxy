using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Policy;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Elemental;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.MediaInfo;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.JobXmlFile;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.JobXmlConfig
{
    internal class CreateEncoderJobXml
    {

        private static List<string> _assetUrls { get; set; }
        private static string _mediafilename { get; set; }
        private static Asset _asset { get; set; }
        private static XmlDocument _EncoderJobXml { get; set; }
        private static bool _istrailor { get; set; }
        private static ContentData _conaxVodContentData { get;set; }

        public CreateEncoderJobXml(Asset asset, ContentData ConaxVodContentData)
        {
            _asset = asset;
            var encoderConfig =
                (ElementalEncoderConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ElementalEncoder);

            _istrailor = asset.IsTrailer;
            String encoderUploadFolder = encoderConfig.EncoderUploadFolder;
            String mezzanineName = asset.Name;
            String fullPathFrom = Path.Combine(encoderUploadFolder, mezzanineName);
            String fullPathTo = Path.Combine(encoderConfig.ElementalEncoderOutFolder, mezzanineName);
            _mediafilename = Path.Combine(encoderUploadFolder, asset.Name);
            MediaFileInfo mi = null;
            try
            {
                mi = MediaInfoHelper.GetMediaInfoForFile(_mediafilename);

            }
            catch (Exception)
            {
                Console.WriteLine("media info could not be created as media files are not present");
            }
            if (mi != null)
            {
                ConaxIntegrationHelper.SetResolution(asset, mi.Width, mi.Height);
                ConaxIntegrationHelper.AddAudioTrackLanguages(asset, mi.AudioInfos);
                ConaxIntegrationHelper.AddSubtitleLanguages(asset, mi.SubtitleInfos);
                ConaxIntegrationHelper.AddDisplayAspectRatio(asset, mi.DisplayAspectRatio);
                ConaxIntegrationHelper.SetConaxContegoContentID(ConaxVodContentData,
                    ConaxVodContentData.HostID);
                ConaxIntegrationHelper.GetURIProfile(ConaxVodContentData);
                string ProfileName = ElementalEncoderHelper.GetProfileID("Elemental", ConaxVodContentData,
                    _istrailor);
                var evw = new ElementalVODServicesWrapper();
                List<JobParameter> jobParameters = evw.CreateDefaultParameters(fullPathFrom, fullPathTo,
                    ConaxIntegrationHelper.GetConaxContegoContentID(ConaxVodContentData));

                //create jobXML
                _EncoderJobXml = evw.GetJobXml(ProfileName, jobParameters, _istrailor,
                     ConaxVodContentData);


                saveJobXml();
            }
        }
        public void saveJobXml()
        {

            var encoderConfig =
               Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
            string EncoderJobXmlFileAreaRoot = encoderConfig.GetConfigParam("EncoderJobXmlFileAreaRoot");
            string[] s = _asset.Name.Split('.');
            string newAssetname = null;
            for (int i = 0; i < s.Length - 1; i++)
            {
                newAssetname = newAssetname + s[i];
            }
            string jobxmlfilename = Path.Combine(EncoderJobXmlFileAreaRoot, newAssetname + ".xml");
            var jobXmlFileInfo = new FileInfo(jobxmlfilename);
            if (!Directory.Exists(jobXmlFileInfo.DirectoryName))
            {
                Directory.CreateDirectory(jobXmlFileInfo.DirectoryName);
            }
            if (!File.Exists(jobXmlFileInfo.FullName))
            {
                File.Create(jobXmlFileInfo.FullName);
            }
            _EncoderJobXml.Save(jobXmlFileInfo.FullName);
            var assetnames = new List<string>();
            var getAssetOutputName = new GetAssetOutputName(jobxmlfilename, _asset.Name);
            assetnames = getAssetOutputName.GetAssetList();

            //here asset will be published in MPP5
            foreach (var n in assetnames)
            {
                //here Asset urls will be added to MPP5
                //CreateMpp5Content createMpp5Content=new CreateMpp5Content();
                //foreach (var v in ConaxVo)
                //{
                    
                //}
                //createMpp5Content.PublishAssetInMPP5();
                Console.WriteLine(n);
                //_assetUrls.Add(n);
            }
            _assetUrls = assetnames;
        }
        public List<string> getAssetUrls()
        {
            return _assetUrls;
        }
        public XmlDocument GetEncoderJobXmlDocument()
        {
            return _EncoderJobXml;
        }
    }
}

