using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.SFAnytime;
using System.Xml;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest.SFA
{
    public class SFAMediaTranslator
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static SFAMedia Translate(RootMediaDetails_1_3 rmd)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SFAnytime").SingleOrDefault();
            var profilesConfig = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "SFAnytimeProfiles").SingleOrDefault();
            var maturityRatingConfig = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "SFAnytimeToCubiMaturityRatingMapping").SingleOrDefault();

            List<SFAProfile> profiles = new List<SFAProfile>();
            string[] HD = profilesConfig.GetConfigParam("HDProfiles").Split(new char[] {','});

            foreach (var item in profilesConfig.GetConfigParam("Priority").Split(new char[] {','}))
            {
                string quality = HD.Contains(item) ? "HD" : "SD";
                profiles.Add(new SFAProfile(int.Parse(item), quality));
            }

            SFAMedia media = new SFAMedia();

            FullMedia_1_3 fm = rmd.media;

            XmlDocument cableLabsXML = new XmlDocument();

            XmlElement adiElement = cableLabsXML.CreateElement("ADI");
            cableLabsXML.AppendChild(adiElement);

            XmlElement metadataElement = cableLabsXML.CreateElement("Metadata");
            adiElement.AppendChild(metadataElement);

            XmlElement amsElement = cableLabsXML.CreateElement("AMS");
            amsElement.SetAttribute("Asset_Name", fm.title);
            amsElement.SetAttribute("Provider", systemConfig.GetConfigParam("Provider"));
            metadataElement.AppendChild(amsElement);

            metadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "MPS_DeviceType", "PC"));
            metadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "MPS_DeviceType", "MAC"));
            metadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "MPS_DeviceType", "STB"));
            metadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "MPS_DeviceType", "iPad"));
            metadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "MPS_DeviceType", "iPhone"));
            metadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "MPS_IngestSource", "SFAnytime"));
            metadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "MPS_ContentPrice", (fm.price / 100).ToString()));
            metadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "MPS_ContentPriceCurrency", fm.currency));
            metadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "MPS_ContentPricePeriodLength", fm.duration.ToString()));

            XmlElement asset = cableLabsXML.CreateElement("Asset");
            adiElement.AppendChild(asset);

            XmlElement assetMetadataElement = cableLabsXML.CreateElement("Metadata");
            asset.AppendChild(assetMetadataElement);

            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Title_Sort_Name", fm.sortName));
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Title", fm.title));
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Episode_Name", fm.brieftitle));
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Summary_Long", fm.info));
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Summary_Short", fm.@short));

            string rating;
            try
            {
                rating = maturityRatingConfig.GetConfigParam(fm.ageLimit.ToString());
            }
            catch (Exception e)
            {
                rating = "G";
                log.Warn("Invalid or missing rating from SFA. Setting rating to -G-", e);
            }

            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Rating", rating));

            TimeSpan duration = new TimeSpan(0, (fm.length == null) ? 0 : (int)fm.length, 0);
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Run_Time", duration.ToString()));
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Year", fm.year.ToString()));
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Provider_Asset_ID", "SFA-" + fm.id.ToString()));
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Country_of_Origin", fm.productionCountry));

            foreach (var item in rmd.cast.Where(x => x.categoryId == "2"))
            {
                assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Actors", item.name));
            }

            string director = "";
            foreach (var item in rmd.cast.Where(x => x.categoryId == "1")) // Might be more than one Director
            {
                director += item.name + ", ";
            }
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Director", director.TrimEnd(new char[] { ',', ' ' })));

            //assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Producer", ""));   // Producer not available

            string addedAsCat = "";
            Category1 cat = rmd.category.FirstOrDefault(x => x.main == "1");
            if (cat == null)
            {
                cat = rmd.category.First();
                addedAsCat = cat.id;
            }
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Category", cat.name));

            foreach (var item in rmd.category.Where(x => x.main == "0"))
            {
                if (item.id != addedAsCat)
                {
                    assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Genre", item.name));
                }
            }

            DateTime dtStart = DateTime.Parse(fm.publishDate);
            DateTime dtEnd = DateTime.Parse(fm.unPublishDate);
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Licensing_Window_Start", dtStart.ToString("yyyy-MM-dd")));
            assetMetadataElement.AppendChild(CreateAppdataElement(cableLabsXML, "Licensing_Window_End", dtEnd.ToString("yyyy-MM-dd")));

            var tsFile = rmd.tsFile.FirstOrDefault(file => file.typeId == 1 && file.profileId == profiles[0].id);
            foreach (var item in profiles)
            {
                tsFile = rmd.tsFile.FirstOrDefault(file => file.typeId == 1 && file.profileId == item.id);
                if (tsFile != null)
                {
                    break;
                }
            }

            if (tsFile == null)
            {
                log.Warn("No file found for any of the selected profiles.");
                return null;
            }
            media.VideoUrl = tsFile.fileUrl;
            media.VideoFileName = tsFile.fileName;
            media.videoPath = tsFile.filePath;
            if (tsFile.fileName.EndsWith(".gpg"))
            {
                media.VideoFileName = tsFile.fileName.Substring(0, tsFile.fileName.Length - 4);
                media.EncryptedFileName = tsFile.fileName;
            }

            asset.AppendChild(CreateVideoAssetElement(cableLabsXML, "movie", tsFile.language, profiles[tsFile.profileId].quality, tsFile.fileSize, media.VideoFileName));

            var tsTrailerFile = rmd.tsFile.FirstOrDefault(file => file.typeId == 2 && file.profileId == profiles[0].id);
            foreach (var item in profiles)
            {
                tsTrailerFile = rmd.tsFile.FirstOrDefault(file => file.typeId == 2 && file.profileId == item.id);
                if (tsTrailerFile != null)
                {
                    break;
                }
            }
            if (tsTrailerFile == null)
            {
                log.Warn("No trailer file found for any of the selected profiles.");
                return null;
            }

            log.Debug("Media id: " + fm.id + " - Feature found in profile " + tsFile.profileId + ". Trailer found in profile " + tsTrailerFile.profileId + ".");

            media.TrailerUrl = tsTrailerFile.fileUrl;
            media.TrailerFilename = tsTrailerFile.fileName;
            media.TrailerPath = tsTrailerFile.filePath;
            media.NewTrailerFilename = media.TrailerFilename.Insert(media.TrailerFilename.IndexOf("tr"), "_");

            asset.AppendChild(CreateVideoAssetElement(cableLabsXML, "preview", tsTrailerFile.language, profiles[tsTrailerFile.profileId].quality, tsTrailerFile.fileSize, media.NewTrailerFilename));

            var imageItem = rmd.image.First(image => image.typeId == "1");
            media.ImageUrl = imageItem.imageUrl;
            media.ImageFileName = "_" + fm.id.ToString() + "_" + media.ImageUrl.Substring(media.ImageUrl.LastIndexOf('/') + 1);
            asset.AppendChild(CreateBoxCoverElement(cableLabsXML, media.ImageFileName));

            media.CableLabsXml = cableLabsXML;

            return media;
        }


        private static XmlElement CreateBoxCoverElement(XmlDocument doc, string filename)
        {
            XmlElement asset = doc.CreateElement("Asset");
            XmlElement metaData = doc.CreateElement("Metadata");
            asset.AppendChild(metaData);

            metaData.AppendChild(CreateAppdataElement(doc, "Type", "box cover"));
            metaData.AppendChild(CreateAppdataElement(doc, "Image_Aspect_Ratio", "320x240"));

            XmlElement c = doc.CreateElement("Content");
            c.SetAttribute("Value", filename);
            asset.AppendChild(c);

            return asset;
        }

        private static XmlElement CreateVideoAssetElement(XmlDocument doc, string type, string language, string resolution, string filesize, string fileName)
        {
            XmlElement asset = doc.CreateElement("Asset");
            XmlElement metaData = doc.CreateElement("Metadata");
            asset.AppendChild(metaData);

            metaData.AppendChild(CreateAppdataElement(doc,"Type", type));
            if (string.IsNullOrEmpty(language))
            {
                language = "EN";
            }
            metaData.AppendChild(CreateAppdataElement(doc, "Languages", language));
            metaData.AppendChild(CreateAppdataElement(doc, "Resolution_Type", resolution));
            metaData.AppendChild(CreateAppdataElement(doc, "Content_FileSize", filesize));

            XmlElement c = doc.CreateElement("Content");
            c.SetAttribute("Value", fileName);
            asset.AppendChild(c);

            return asset;
        }

        private static XmlElement CreateAppdataElement(XmlDocument doc, string name, string value)
        {
            XmlElement e = doc.CreateElement("App_Data");
            e.SetAttribute("Value", value);
            e.SetAttribute("Name", name);
            e.SetAttribute("App", "MOD");

            return e;
        }
    }
    public class SFAProfile
    {
        public int id;
        public string quality;
        
        public SFAProfile(int profileId, string profileQuality)
        {
            id = profileId;
            quality = profileQuality;
        }
    }
}
