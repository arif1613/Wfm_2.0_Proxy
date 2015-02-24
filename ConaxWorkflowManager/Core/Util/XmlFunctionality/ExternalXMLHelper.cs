using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.IO;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality
{
    public class ExternalXMLHelper
    {

        private ContentData Content;

        private MultipleContentService Service;

        public ExternalXMLHelper()
        {

        }

        private static XmlElement CreateContentElement(XmlDocument doc, string value)
        {
            XmlElement e = doc.CreateElement("Content");
            e.SetAttribute("Value", value);

            return e;
        }

        private static XmlElement CreateAppdataElement(XmlDocument doc, string name, string value)
        {
            XmlElement e = doc.CreateElement("App_Data");
            e.SetAttribute("Value", value);
            e.SetAttribute("Name", name);
            e.SetAttribute("App", "MOD");

            return e;
        }

        /// <summary>
        /// This method translates the Mpp contentmeta data to Columbus Specific CableLabs 1.1
        /// </summary>
        /// <param name="MppContentMetaData">The contentdata as it is returned from mpp</param>
        /// <returns>The contentdata as CableLabs 1.1</returns>
        public XmlDocument AsColumbusCableLabs1_1(XmlDocument mppContentMetaData, XmlDocument mppPriceData)
        {
            XmlDocument cableLabsXML = new XmlDocument();
            MppXmlTranslator mppTranslator = new MppXmlTranslator();
            List<ContentData> contents = mppTranslator.TranslateXmlToContentData(mppContentMetaData);
            ContentData content = contents[0];
            MultipleServicePrice price = mppTranslator.TranslateXmlToPriceData(mppPriceData);
            cableLabsXML = AsCableLabs1_1(content, price, true);
            return cableLabsXML;
        }

        /// <summary>
        /// This method translates the Mpp contentmeta data to Columbus Specific CableLabs 1.1
        /// </summary>
        /// <param name="contentData">The contentdata</param>
        /// <returns>The contentdata as CableLabs 1.1</returns>
        public XmlDocument AsColumbusCableLabs1_1(ContentData contentData, MultipleServicePrice price)
        {
            return AsCableLabs1_1(contentData, price, true); ;
        }

        /// <summary>
        /// This method translates the Mpp contentmeta data to CableLabs 1.1
        /// </summary>
        /// <param name="MppContentMetaData">The contentdata as it is returned from mpp</param>
        /// <returns>The contentdata as CableLabs 1.1</returns>
        public XmlDocument AsCableLabs1_1(XmlDocument mppContentMetaData, XmlDocument mppPriceData)
        {
            XmlDocument cableLabsXML = new XmlDocument();
            MppXmlTranslator mppTranslator = new MppXmlTranslator();
            List<ContentData> contents = mppTranslator.TranslateXmlToContentData(mppContentMetaData);
            ContentData content = contents[0];

            MultipleServicePrice price = mppTranslator.TranslateXmlToPriceData(mppPriceData);

            return AsColumbusCableLabs1_1(content, price);
        }

        /// <summary>
        /// This method returns the CableLabs XML for the contentData
        /// </summary>
        /// <param name="contentData">The contentdata to translate to CableLabs XML</param>
        /// <returns></returns>
        private XmlDocument AsCableLabs1_1(ContentData contentData, MultipleServicePrice price)
        {
            return AsCableLabs1_1(contentData, price, false);
        }

        private XmlDocument AsCableLabs1_1(ContentData contentData, MultipleServicePrice price, bool addColumbusSpecificValues)
        {
            XmlDocument cableLabsXML = new XmlDocument();

            XmlElement adiElement = cableLabsXML.CreateElement("ADI");
            cableLabsXML.AppendChild(adiElement);

            // PACKAGE
            XmlElement packageMetadataElement = cableLabsXML.CreateElement("Metadata");
            adiElement.AppendChild(packageMetadataElement);

            String value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Package_Metadata_AMS);
            if (!String.IsNullOrEmpty(value))
            {
                value = "<AMS " + value + " />";
                XmlDocumentFragment xfrag = cableLabsXML.CreateDocumentFragment();
                xfrag.InnerXml = value;
                packageMetadataElement.AppendChild(xfrag);
            }
            XmlElement element = CreateAppdataElement(cableLabsXML, "Metadata_Spec_Version", "CableLabsVOD1.1");
            packageMetadataElement.AppendChild(element);

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Provider_Content_Tier);
            if (!String.IsNullOrEmpty(value))
            {
                var pctElem = CreateAppdataElement(cableLabsXML, "Provider_Content_Tier", value);
                packageMetadataElement.AppendChild(pctElem);
            }

            // END PACKAGE

            // TITLE


            LanguageInfo languageInfo = GetLanguageInfo(contentData);

            XmlElement titleElement = cableLabsXML.CreateElement("Asset");
            adiElement.AppendChild(titleElement);

            XmlElement titleMetadataElement = cableLabsXML.CreateElement("Metadata");
            titleElement.AppendChild(titleMetadataElement);

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_AMS);
            if (!String.IsNullOrEmpty(value))
            {
                value = "<AMS " + value + " />";
                XmlDocumentFragment xfrag = cableLabsXML.CreateDocumentFragment();
                xfrag.InnerXml = value;
                titleMetadataElement.AppendChild(xfrag);
            }

            //value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_Type);
            //if (!String.IsNullOrEmpty(value))
            //{
            //    element = CreateAppdataElement(cableLabsXML, "Type", value);
            //    titleMetadataElement.AppendChild(element);
            //}

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_Type);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Type", value);
                titleMetadataElement.AppendChild(element);
            }

            if (languageInfo != null && !String.IsNullOrEmpty(languageInfo.SortName))
            {
                element = CreateAppdataElement(cableLabsXML, "Title_Sort_Name", languageInfo.SortName);
                titleMetadataElement.AppendChild(element);
            }


            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_Brief);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Title_Brief", value);
                titleMetadataElement.AppendChild(element);
            }

            if (languageInfo != null && !String.IsNullOrEmpty(languageInfo.Title))
            {
                element = CreateAppdataElement(cableLabsXML, "Title", languageInfo.Title);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_EIDR);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "EIDR", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_ISAN);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "ISAN", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_Episode_Name);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Episode_Name", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_Episode_ID);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Episode_ID", value);
                titleMetadataElement.AppendChild(element);
            }

            if (languageInfo != null && !String.IsNullOrEmpty(languageInfo.LongDescription))
            {
                element = CreateAppdataElement(cableLabsXML, "Summary_Long", languageInfo.LongDescription);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_Summary_Medium);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Summary_Medium", value);
                titleMetadataElement.AppendChild(element);
            }

            if (languageInfo != null && !String.IsNullOrEmpty(languageInfo.ShortDescription))
            {
                element = CreateAppdataElement(cableLabsXML, "Summary_Short", languageInfo.ShortDescription);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValueIgnoreCase(VODnLiveContentProperties.MovieRating);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Rating", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValueIgnoreCase(VODnLiveContentProperties.TVRating);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Rating", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_MSORating);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "MSORating", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_Advisories);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Advisories", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_Audience);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Audience", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Title_Closed_Captioning);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Closed_Captioning", value);
                titleMetadataElement.AppendChild(element);
            }

            if (contentData.RunningTime.HasValue)
            {
                DateTime runtime_tmp = new DateTime(contentData.RunningTime.Value.Ticks);
                element = CreateAppdataElement(cableLabsXML, "Run_Time", runtime_tmp.ToString("HH:mm:ss"));
                titleMetadataElement.AppendChild(element);
            }


            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Display_Runtime);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Display_Run_Time", value);
                titleMetadataElement.AppendChild(element);
            }

            if (contentData.ProductionYear != 0)
            {
                element = CreateAppdataElement(cableLabsXML, "Year", contentData.ProductionYear.ToString());
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(VODnLiveContentProperties.Country);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Country_of_Origin", value);
                titleMetadataElement.AppendChild(element);
            }

            foreach (String actor in contentData.GetPropertyValues(VODnLiveContentProperties.Cast))
            {
                List<String> actorsList = actor.Split(';').ToList();
                foreach (var a in actorsList)
                {
                    element = CreateAppdataElement(cableLabsXML, "Actors", a);
                    titleMetadataElement.AppendChild(element);
                }
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Actors_Display);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Actors_Display", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Writer_Display);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Writer_Display", value);
                titleMetadataElement.AppendChild(element);
            }

            foreach (String director in contentData.GetPropertyValues(VODnLiveContentProperties.Director))
            {
                element = CreateAppdataElement(cableLabsXML, "Director", director);
                titleMetadataElement.AppendChild(element);
            }

            foreach (String producer in contentData.GetPropertyValues(VODnLiveContentProperties.Producer))
            {
                element = CreateAppdataElement(cableLabsXML, "Producer", producer);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Studio);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Studio", value);
                titleMetadataElement.AppendChild(element);
            }

            foreach (String category in contentData.GetPropertyValues(VODnLiveContentProperties.Category))
            {
                element = CreateAppdataElement(cableLabsXML, "Category", category);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Studio);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Season_Premiere", contentData.GetPropertyValue(CableLabs1_1ContentProperties.Season_Premiere));
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Season_Finale);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Season_Finale", value);
                titleMetadataElement.AppendChild(element);
            }

            foreach (String genre in contentData.GetPropertyValues(VODnLiveContentProperties.Genre))
            {
                element = CreateAppdataElement(cableLabsXML, "Genre", genre);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Show_Type);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Show_Type", value);
                titleMetadataElement.AppendChild(element);
            }

            foreach (String chapter in contentData.GetPropertyValues(CableLabs1_1ContentProperties.Chapter))
            {
                element = CreateAppdataElement(cableLabsXML, "Category", chapter);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Box_Office);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Box_Office", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Propagation_Priority);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Propagation_Priority", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Billing_ID);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Billing_ID", value);
                titleMetadataElement.AppendChild(element);
            }



            element = CreateAppdataElement(cableLabsXML, "Licensing_Window_Start", contentData.EventPeriodFrom.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
            titleMetadataElement.AppendChild(element);


            element = CreateAppdataElement(cableLabsXML, "Licensing_Window_End", contentData.EventPeriodTo.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
            titleMetadataElement.AppendChild(element);


            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Preview_Period);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Preview_Period", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Home_Video_Window);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Home_Video_Window", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Display_As_New);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Display_As_New", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Display_As_Last_Chance);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Display_As_Last_Chance", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Maximum_Viewing_Length);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Maximum_Viewing_Length", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Provider_QA_Contact);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Provider_QA_Contact", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Contract_Name);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Contract_Name", value);
                titleMetadataElement.AppendChild(element);
            }

            if (price != null)
            {
                element = CreateAppdataElement(cableLabsXML, "Suggested_Price", price.Price.ToString().Replace(",", "."));
                titleMetadataElement.AppendChild(element);
            }


            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Distributor_Royalty_Percent);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Distributor_Royalty_Percent", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Distributor_Royalty_Minimum);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Distributor_Royalty_Minimum", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Distributor_Royalty_Flat_Rate);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Distributor_Royalty_Flat_Rate", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Distributor_Name);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Distributor_Name", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Studio_Royalty_Percent);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Studio_Royalty_Percent", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Studio_Royalty_Minimum);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Studio_Royalty_Minimum", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Studio_Royalty_Flat_Rate);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Studio_Royalty_Flat_Rate", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Studio_Name);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Studio_Name", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Studio_Code);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Studio_Code", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Programmer_Call_Letters);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Programmer_Call_Letters", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Recording_Artist);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Recording_Artist", value);
                titleMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Song_Title);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Song_Title", value);
                titleMetadataElement.AppendChild(element);
            }

            // END TITLE


            // MOVIE

            XmlElement movieElement = cableLabsXML.CreateElement("Asset");



            titleElement.AppendChild(movieElement);
            XmlElement movieMetadataElement = cableLabsXML.CreateElement("Metadata");
            movieElement.AppendChild(movieMetadataElement);

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_AMS);
            if (!String.IsNullOrEmpty(value))
            {
                value = "<AMS " + value + " />";
                XmlDocumentFragment xfrag = cableLabsXML.CreateDocumentFragment();
                xfrag.InnerXml = value;
                movieMetadataElement.AppendChild(xfrag);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Encryption);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Encryption", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Type);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Type", value);
                movieMetadataElement.AppendChild(element);
            }

            foreach (String audioType in contentData.GetPropertyValues(CableLabs1_1ContentProperties.Movie_Audio_Type))
            {
                element = CreateAppdataElement(cableLabsXML, "Audio_type", audioType);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Screen_Format);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Screen_Format", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Resolution);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Resolution", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Frame_Rate);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Frame_Rate", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Codec);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Codec", value);
                movieMetadataElement.AppendChild(element);
            }

            foreach (String languages in contentData.GetPropertyValues(CableLabs1_1ContentProperties.Movie_Languages))
            {
                element = CreateAppdataElement(cableLabsXML, "Languages", languages);
                movieMetadataElement.AppendChild(element);
            }

            foreach (String subtitle in contentData.GetPropertyValues(CableLabs1_1ContentProperties.Movie_Subtitle_Languages))
            {
                element = CreateAppdataElement(cableLabsXML, "Subtitle_Languages", subtitle);
                movieMetadataElement.AppendChild(element);
            }

            foreach (String dubbedLanguage in contentData.GetPropertyValues(CableLabs1_1ContentProperties.Movie_Dubbed_Languages))
            {
                element = CreateAppdataElement(cableLabsXML, "Dubbed_Languages", dubbedLanguage);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Copy_Protection);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Copy_Protection", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Copy_Protection_Verbose);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Copy_Protection_Verbose", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Analog_Protection_System);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Analog_Protection_System", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Encryption_Mode_Indicator);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Encryption_Mode_Indicator", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Constrained_Image_Trigger);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Constrained_Image_Trigger", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_CGMS_A);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "CGMS_A", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Viewing_Can_Be_Resumed);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Viewing_Can_Be_Resumed", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Bit_Rate);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Bit_Rate", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Content_FileSize);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Content_FileSize", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Content_CheckSum);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Content_CheckSum", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_trickModesRestricted);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "trickModesRestricted", value);
                movieMetadataElement.AppendChild(element);
            }

            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_Selectable_Output_Control);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "Selectable_Output_Control", value);
                movieMetadataElement.AppendChild(element);
            }
            value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Movie_3D_Mode);
            if (!String.IsNullOrEmpty(value))
            {
                element = CreateAppdataElement(cableLabsXML, "3D_Mode", value);
                movieMetadataElement.AppendChild(element);
            }


            // Columbus specific

            if (addColumbusSpecificValues)
            {
                value = contentData.GetPropertyValue(ColumbusContentProperties.Movie_HDContent);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "HDContent", value);
                    movieMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(ColumbusContentProperties.Movie_Watermarking);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Watermarking", value);
                    movieMetadataElement.AppendChild(element);
                }
            }

            // END Columbus specific

            var movieAsset = contentData.Assets.FirstOrDefault(v => v.IsTrailer == false);
            if (movieAsset != null)
            {
                var assetNameProperty = movieAsset.Properties.FirstOrDefault(a => a.Type.Equals("SourceFileName"));
                if (assetNameProperty != null)
                {
                    FileInfo fi = new FileInfo(assetNameProperty.Value);
                    element = CreateContentElement(cableLabsXML, fi.Name);
                    movieElement.AppendChild(element);
                }
            }

            // END MOVIE

            // TRAILER

            var trailerAsset = contentData.Assets.FirstOrDefault(v => v.IsTrailer == true);
            if (trailerAsset != null)
            {
                XmlElement trailerElement = cableLabsXML.CreateElement("Asset");


                titleElement.AppendChild(trailerElement);

                XmlElement trailerMetadataElement = cableLabsXML.CreateElement("Metadata");
                trailerElement.AppendChild(trailerMetadataElement);

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_AMS);
                if (!String.IsNullOrEmpty(value))
                {
                    value = "<AMS " + value + " />";
                    XmlDocumentFragment xfrag = cableLabsXML.CreateDocumentFragment();
                    xfrag.InnerXml = value;
                    trailerMetadataElement.AppendChild(xfrag);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Rating);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Rating", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_MSORating);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "MSORating", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Audience);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Audience", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Run_Time);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Run_Time", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Type);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Type", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Audio_Type);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Audio_Type", value);
                    trailerMetadataElement.AppendChild(element);
                }
                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Screen_Format);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Screen_Format", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Resolution);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Resolution", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Frame_Rate);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Frame_Rate", value);
                    trailerMetadataElement.AppendChild(element);
                }
                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Codec);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Codec", value);
                    trailerMetadataElement.AppendChild(element);
                }

                foreach (
                    String language in contentData.GetPropertyValues(CableLabs1_1ContentProperties.Trailer_Languages))
                {
                    element = CreateAppdataElement(cableLabsXML, "Languages", language);
                    trailerMetadataElement.AppendChild(element);
                }

                foreach (
                    String subtitle in
                        contentData.GetPropertyValues(CableLabs1_1ContentProperties.Trailer_Subtitle_Languages))
                {
                    element = CreateAppdataElement(cableLabsXML, "Subtitle_Languages", subtitle);
                    trailerMetadataElement.AppendChild(element);
                }

                foreach (
                    String dubbedLanguage in
                        contentData.GetPropertyValues(CableLabs1_1ContentProperties.Trailer_Dubbed_Languages))
                {
                    element = CreateAppdataElement(cableLabsXML, "Dubbed_Languages", dubbedLanguage);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Bit_Rate);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Bit_Rate", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Content_FileSize);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Content_FileSize", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_Content_CheckSum);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Content_CheckSum", value);
                    trailerMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Trailer_trickModesRestricted);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "trickModesRestricted", value);
                    trailerMetadataElement.AppendChild(element);
                }

                // Columbus specific

                if (addColumbusSpecificValues)
                {
                    value = contentData.GetPropertyValue(ColumbusContentProperties.Trailer_HDContent);
                    if (!String.IsNullOrEmpty(value))
                    {
                        element = CreateAppdataElement(cableLabsXML, "HDContent", value);
                        trailerMetadataElement.AppendChild(element);
                    }

                    value = contentData.GetPropertyValue(ColumbusContentProperties.Trailer_Watermarking);
                    if (!String.IsNullOrEmpty(value))
                    {
                        element = CreateAppdataElement(cableLabsXML, "Watermarking", value);
                        trailerMetadataElement.AppendChild(element);
                    }
                }


                var assetNameProperty = trailerAsset.Properties.FirstOrDefault(a => a.Type.Equals("SourceFileName"));
                if (assetNameProperty != null)
                {
                    FileInfo fi = new FileInfo(assetNameProperty.Value);
                    element = CreateContentElement(cableLabsXML, fi.Name);
                    trailerElement.AppendChild(element);
                }
            }
            // END Columbus specific

            // END TRAILER

            // BOX COVER

            if (ContentHasBoxCover(contentData))
            {
                XmlElement boxCoverElement = cableLabsXML.CreateElement("Asset");

                titleElement.AppendChild(boxCoverElement);

                XmlElement boxCoverMetadataElement = cableLabsXML.CreateElement("Metadata");
                boxCoverElement.AppendChild(boxCoverMetadataElement);

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.BoxCover_AMS);
                if (!String.IsNullOrEmpty(value))
                {
                    value = "<AMS " + value + " />";
                    XmlDocumentFragment xfrag = cableLabsXML.CreateDocumentFragment();
                    xfrag.InnerXml = value;
                    boxCoverMetadataElement.AppendChild(xfrag);
                }

                element = CreateAppdataElement(cableLabsXML, "Type", "box cover");
                boxCoverMetadataElement.AppendChild(element);

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.BoxCover_Content_FileSize);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Content_FileSize", value);
                    boxCoverMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.BoxCover_Image_Aspect_Ratio);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Image_Aspect_Ratio", value);
                    boxCoverMetadataElement.AppendChild(element);
                }


                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.BoxCover_Content_CheckSum);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Content_CheckSum", value);
                    boxCoverMetadataElement.AppendChild(element);
                }

                // Columbus specific

                if (addColumbusSpecificValues)
                {
                    value = contentData.GetPropertyValue(ColumbusContentProperties.Boxcover_Image_Content_Aspect);
                    if (!String.IsNullOrEmpty(value))
                    {
                        element = CreateAppdataElement(cableLabsXML, "Image_Content_Aspect", value);
                        boxCoverMetadataElement.AppendChild(element);
                    }
                }
                // END Columbus specific

                foreach (var image in languageInfo.Images)
                {
                    if (image.ClientGUIName == "box cover")
                    {
                        if (!String.IsNullOrEmpty(image.URI))
                        {
                            element = CreateContentElement(cableLabsXML, image.URI.Substring(image.URI.LastIndexOf(@"\") + 1));
                            boxCoverElement.AppendChild(element);
                        }
                    }
                }

            }

            // END BOX COVER

            // POSTER

            if (ContentHasPoster(contentData))
            {
                XmlElement posterElement = cableLabsXML.CreateElement("Asset");



                titleElement.AppendChild(posterElement);

                XmlElement posterMetadataElement = cableLabsXML.CreateElement("Metadata");
                posterElement.AppendChild(posterMetadataElement);

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Poster_AMS);
                if (!String.IsNullOrEmpty(value))
                {
                    value = "<AMS " + value + " />";
                    XmlDocumentFragment xfrag = cableLabsXML.CreateDocumentFragment();
                    xfrag.InnerXml = value;
                    posterMetadataElement.AppendChild(xfrag);
                }

                element = CreateAppdataElement(cableLabsXML, "Type", "poster");
                posterMetadataElement.AppendChild(element);

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Poster_Content_FileSize);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Content_FileSize", value);
                    posterMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Poster_Image_Aspect_Ratio);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Image_Aspect_Ratio", value);
                    posterMetadataElement.AppendChild(element);
                }

                value = contentData.GetPropertyValue(CableLabs1_1ContentProperties.Poster_Content_CheckSum);
                if (!String.IsNullOrEmpty(value))
                {
                    element = CreateAppdataElement(cableLabsXML, "Content_CheckSum", value);
                    posterMetadataElement.AppendChild(element);
                }

                // Columbus specific

                if (addColumbusSpecificValues)
                {
                    value = contentData.GetPropertyValue(ColumbusContentProperties.Poster_Image_Content_Aspect);
                    if (!String.IsNullOrEmpty(value))
                    {
                        element = CreateAppdataElement(cableLabsXML, "Image_Content_Aspect", value);
                        posterMetadataElement.AppendChild(element);
                    }
                }

                // END Columbus specific

                foreach (var image in languageInfo.Images)
                {
                    if (image.ClientGUIName == "poster")
                    {
                        if (!String.IsNullOrEmpty(image.URI))
                        {
                            element = CreateContentElement(cableLabsXML, image.URI.Substring(image.URI.LastIndexOf(@"\") + 1));
                            posterElement.AppendChild(element);
                        }
                    }
                }
            }

            // END POSTER

            return cableLabsXML;
        }

        private LanguageInfo GetLanguageInfo(ContentData content)
        {
            LanguageInfo languageInfo = null;
            if (content.LanguageInfos.Count > 0)
                languageInfo = content.LanguageInfos[0];
            return languageInfo;
        }

        private string TrimAMSNode(XmlNode amsNode)
        {
            String xml = amsNode.OuterXml;
            xml = xml.Replace("<AMS ", "");
            xml = xml.Replace("/>", "");
            return xml;
        }

        private bool ContentHasPoster(ContentData contentData)
        {
            foreach (LanguageInfo info in contentData.LanguageInfos)
            {
                foreach (Image image in info.Images)
                {
                    if (image.ClientGUIName.Equals("poster"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool ContentHasBoxCover(ContentData contentData)
        {
            foreach (LanguageInfo info in contentData.LanguageInfos)
            {
                foreach (Image image in info.Images)
                {
                    if (image.ClientGUIName.Equals("box cover"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
