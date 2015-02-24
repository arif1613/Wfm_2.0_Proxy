using System;
using System.IO;
using System.Linq;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.PublishTask
{
    public class CreateMppPublishingProfile
    {
        private static ContentData ConaxVodContentData { get; set; }
        private static string _publishingprofilefilename;

        public CreateMppPublishingProfile(ContentData conaxVodContentData)
        {
            ConaxVodContentData = conaxVodContentData;
            CreateMppContent();
        }
        public void CreateMppContent()
        {
            var mppXmlTranslator = new MppXmlTranslator(ConaxVodContentData.Mpp5_Id);
            var mppXmlDocument = mppXmlTranslator.TranslateContentDataToXml(ConaxVodContentData);
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig()
                        .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            string contentRightsOwner = ConaxVodContentData.ContentRightsOwner.Name;
            foreach (var x in ConaxVodContentData.ContentAgreements)
            {
                string contentAgreement = x.Name;
                string publishingDir = null;
                string enableQA = ConaxVodContentData.Properties.FirstOrDefault(r => r.Type == "EnableQA").Value;
                if (enableQA == "True" || enableQA == "true")
                {
                    publishingDir = systemConfig.NeedQAPublishDir;
                }
                else
                {
                    publishingDir = systemConfig.DirectPublishDir;
                }
                if (!Directory.Exists(Path.Combine(publishingDir, contentRightsOwner, contentAgreement)))
                {
                    Directory.CreateDirectory(Path.Combine(publishingDir, contentRightsOwner, contentAgreement));
                    string publishProfileFileName = Path.Combine(publishingDir, contentRightsOwner, contentAgreement,
                    ConaxVodContentData.Name.Trim() + ".xml");
                    if (!File.Exists(publishProfileFileName))
                    {
                        File.Create(publishProfileFileName);
                    }
                    try
                    {
                        mppXmlDocument.Save(publishProfileFileName);
                        _publishingprofilefilename = publishProfileFileName;
                    }
                    catch (Exception)
                    {
                        CreateMppContent();
                    }
                   }
                else
                {
                    string publishProfileFileName = Path.Combine(publishingDir, contentRightsOwner, contentAgreement,
                     ConaxVodContentData.Name.Trim() + ".xml");
                    if (!File.Exists(publishProfileFileName))
                    {
                        File.Create(publishProfileFileName);
                    }
                    try
                    {
                        mppXmlDocument.Save(publishProfileFileName);
                        _publishingprofilefilename = publishProfileFileName;
                    }
                    catch (Exception)
                    {
                        CreateMppContent();
                    }
                }

            }
        }
        public string MppPublishingProfileFileName()
        {
            return _publishingprofilefilename;
        }
    }
}
