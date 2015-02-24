using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.IO;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class AddColumbusPropertiesHandler : ResponsibilityHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            var conaxConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String workDir = conaxConfig.GetConfigParam("FileIngestWorkDirectory");
            

            var ingestXMLFileNameProperty = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName", StringComparison.OrdinalIgnoreCase));
            String cableLabsXmlFile = Path.Combine(workDir, ingestXMLFileNameProperty.Value);
            XmlDocument cableLabsXml = new XmlDocument();
            try
            {
                cableLabsXml = CommonUtil.LoadXML(cableLabsXmlFile);
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong loading CableLabs Xml", ex);
                throw;
            }

            // Fetch Package Info
            XmlNode packageNode = cableLabsXml.SelectSingleNode("ADI/Metadata");
            XmlNode amsNode = packageNode.SelectSingleNode("AMS");
            XmlElement dataNode = null;
            XmlNodeList dataNodes = null;
            if (amsNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Package_Metadata_AMS, TrimAMSNode(amsNode));

            dataNode = (XmlElement)packageNode.SelectSingleNode("App_Data[@Name='Provider_Content_Tier']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Provider_Content_Tier, dataNode.GetAttribute("Value"));


            // Fetch title info
            XmlNode titleNode = GetTitleNode(cableLabsXml);
            amsNode = (XmlElement)titleNode.SelectSingleNode("AMS");
            if (amsNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_AMS, TrimAMSNode(amsNode));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Type']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_Type, dataNode.GetAttribute("Value"));

            //dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Title_Sort_Name']");
            //if (dataNode != null)
            //    content.AddPropertyValue(ColumbusContentProperties.Title_Sort_Name, dataNode.GetAttribute("Value"));

            dataNodes = titleNode.SelectNodes("App_Data[@Name='Subscriber_View_Limit']");
            foreach (XmlElement node in dataNodes)
                content.AddPropertyValue(ColumbusContentProperties.Title_Subscriber_View_Limit, node.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Title_Brief']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_Brief, dataNode.GetAttribute("Value"));

            //dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Title']");
            //if (dataNode != null)
            //    content.AddPropertyValue(ColumbusContentProperties.Title, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='EIDR']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_EIDR, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='ISAN1']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_ISAN, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Episode_Name']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_Episode_Name, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Episode_ID']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_Episode_ID, dataNode.GetAttribute("Value"));


            //dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Summary_Long']");
            //if (dataNode != null)
            //    content.AddPropertyValue(ColumbusContentProperties.Title_Summary_Long, dataNode.GetAttribute("Value"));

            //dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Summary_Medium']");
            //if (dataNode != null)
            //    content.AddPropertyValue(ColumbusContentProperties.Title_Summary_Medium, dataNode.GetAttribute("Value"));

            //dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Summary_Short']");
            //if (dataNode != null)
            //    content.AddPropertyValue(ColumbusContentProperties.Title_Summary_Short, dataNode.GetAttribute("Value"));


            //dataNodes = titleNode.SelectNodes("App_Data[@Name='Rating']");
            //foreach (XmlElement node in dataNodes)
            //    content.AddPropertyValue(ColumbusContentProperties.Rating, node.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='MSORating']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_MSORating, dataNode.GetAttribute("Value"));


            dataNodes = titleNode.SelectNodes("App_Data[@Name='Advisories']");
            foreach (XmlElement node in dataNodes)
                content.AddPropertyValue(ColumbusContentProperties.Title_Advisories, node.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Audience']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_Audience, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Closed_Captioning']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_Closed_Captioning, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Display_Run_Time']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Display_Runtime, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Country_of_Origin']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Country_of_Origin, dataNode.GetAttribute("Value"));

            //dataNodes = titleNode.SelectNodes("App_Data[@Name='Actors']");
            //foreach (XmlElement node in dataNodes)
            //    content.AddPropertyValue(ColumbusContentProperties.Actors, node.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Actors_Display']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Actors_Display, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Writer_Display']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Writer_Display, dataNode.GetAttribute("Value"));



            //dataNodes = titleNode.SelectNodes("App_Data[@Name='Director']");
            //foreach (XmlElement node in dataNodes)
            //    content.AddPropertyValue(ColumbusContentProperties.Director, node.GetAttribute("Value"));

            //dataNodes = titleNode.SelectNodes("App_Data[@Name='Producer']");
            //foreach (XmlElement node in dataNodes)
            //    content.AddPropertyValue(ColumbusContentProperties.Producers, node.GetAttribute("Value"));



            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Studio']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Studio, dataNode.GetAttribute("Value"));

            //dataNodes = titleNode.SelectNodes("App_Data[@Name='Category']");
            //foreach (XmlElement node in dataNodes)
            //    content.AddPropertyValue(ColumbusContentProperties.Category, node.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Season_Premiere']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Season_Premiere, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Season_Finale']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Season_Finale, dataNode.GetAttribute("Value"));


            //dataNodes = titleNode.SelectNodes("App_Data[@Name='Genre']");
            //foreach (XmlElement node in dataNodes)
            //    content.AddPropertyValue(ColumbusContentProperties.Genre, node.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Show_Type']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Show_Type, dataNode.GetAttribute("Value"));

            dataNodes = titleNode.SelectNodes("App_Data[@Name='Chapter']");
            foreach (XmlElement node in dataNodes)
                content.AddPropertyValue(ColumbusContentProperties.Chapter, node.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Box_Office']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Box_Office, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Propagation_Priority']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Propagation_Priority, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Billing_ID']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Billing_ID, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Run_Time']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Title_Run_Time, dataNode.GetAttribute("Value"));


            //dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Licensing_Window_Start']");
            //if (dataNode != null)
            //    content.AddPropertyValue(ColumbusContentProperties.Licensing_Window_Start, dataNode.GetAttribute("Value"));


            //dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Licensing_Window_End']");
            //if (dataNode != null)
            //    content.AddPropertyValue(ColumbusContentProperties.Licensing_Window_End, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Preview_Period']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Preview_Period, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Home_Video_Window']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Home_Video_Window, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Display_As_New']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Display_As_New, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Display_As_Last_Chance']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Display_As_Last_Chance, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Maximum_Viewing_Length']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Maximum_Viewing_Length, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Provider_QA_Contact']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Provider_QA_Contact, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Contract_Name']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Contract_Name, dataNode.GetAttribute("Value"));


            //dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Suggested_Price']");
            //if (dataNode != null)
            //    content.AddPropertyValue(ColumbusContentProperties.Suggested_Price, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Distributor_Royalty_Percent']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Distributor_Royalty_Percent, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Distributor_Royalty_Minimum']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Distributor_Royalty_Minimum, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Distributor_Royalty_Flat_Rate']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Distributor_Royalty_Flat_Rate, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Distributor_Name']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Distributor_Name, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Studio_Royalty_Percent']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Studio_Royalty_Percent, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Studio_Royalty_Minimum']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Studio_Royalty_Minimum, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Studio_Royalty_Flat_Rate']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Studio_Royalty_Flat_Rate, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Studio_Name']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Studio_Name, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Studio_Code']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Studio_Code, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Programmer_Call_Letters']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Programmer_Call_Letters, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Recording_Artist']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Recording_Artist, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)titleNode.SelectSingleNode("App_Data[@Name='Song_Title']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Song_Title, dataNode.GetAttribute("Value"));




            // Fetch movie info
            XmlNode movieNode = GetNodeByType(cableLabsXml, "movie");
            amsNode = movieNode.SelectSingleNode("AMS");
            if (amsNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_AMS, TrimAMSNode(amsNode));

            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Encryption']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Encryption, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Type']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Type, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Audio_Type']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Audio_Type, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Screen_Format']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Screen_Format, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Resolution']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Resolution, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Frame_Rate']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Frame_Rate, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Codec']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Codec, dataNode.GetAttribute("Value"));

            dataNodes = movieNode.SelectNodes("App_Data[@Name='Languages']");
            foreach (XmlElement node in dataNodes)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Languages, node.GetAttribute("Value"));

            dataNodes = movieNode.SelectNodes("App_Data[@Name='Subtitle_Languages']");
            foreach (XmlElement node in dataNodes)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Subtitle_Languages, node.GetAttribute("Value"));


            dataNodes = movieNode.SelectNodes("App_Data[@Name='Dubbed_Languages']");
            foreach (XmlElement node in dataNodes)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Dubbed_Languages, node.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Copy_Protection']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Copy_Protection, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Copy_Protection_Verbose']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Copy_Protection_Verbose, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Analog_Protection_System']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Analog_Protection_System, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Encryption_Mode_Indicator']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Encryption_Mode_Indicator, dataNode.GetAttribute("Value"));



            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Constrained_Image_Trigger']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Constrained_Image_Trigger, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='CGMS_A']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_CGMS_A, dataNode.GetAttribute("Value"));



            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Viewing_Can_Be_Resumed']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Viewing_Can_Be_Resumed, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Bit_Rate']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Bit_Rate, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Content_FileSize']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Content_FileSize, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Content_CheckSum']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Content_CheckSum, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='trickModesRestricted']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_trickModesRestricted, dataNode.GetAttribute("Value"));


            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='Selectable_Output_Control']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_Selectable_Output_Control, dataNode.GetAttribute("Value"));

            dataNode = (XmlElement)movieNode.SelectSingleNode("App_Data[@Name='3D_Mode']");
            if (dataNode != null)
                content.AddPropertyValue(ColumbusContentProperties.Movie_3D_Mode, dataNode.GetAttribute("Value"));






            // Fetch trailer info

            XmlNode trailerNode = GetNodeByType(cableLabsXml, "preview");
            if (trailerNode != null)
            {
                amsNode = trailerNode.SelectSingleNode("AMS");
                if (amsNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_AMS, TrimAMSNode(amsNode));

                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Rating']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Rating, dataNode.GetAttribute("Value"));


                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='MSORating']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_MSORating, dataNode.GetAttribute("Value"));

                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Audience']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Audience, dataNode.GetAttribute("Value"));


                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Run_Time']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Run_Time, dataNode.GetAttribute("Value"));

                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Type']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Type, dataNode.GetAttribute("Value"));


                dataNodes = trailerNode.SelectNodes("App_Data[@Name='Audio_Type']");
                foreach (XmlElement node in dataNodes)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Audio_Type, node.GetAttribute("Value"));


                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Screen_Format']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Screen_Format, dataNode.GetAttribute("Value"));

                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Resolution']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Resolution, dataNode.GetAttribute("Value"));

                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Frame_Rate']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Frame_Rate, dataNode.GetAttribute("Value"));

                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Codec']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Codec, dataNode.GetAttribute("Value"));

                dataNodes = trailerNode.SelectNodes("App_Data[@Name='Languages']");
                foreach (XmlElement node in dataNodes)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Languages, node.GetAttribute("Value"));


                dataNodes = trailerNode.SelectNodes("App_Data[@Name='Subtitle_Languages']");
                foreach (XmlElement node in dataNodes)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Subtitle_Languages, node.GetAttribute("Value"));

                dataNodes = trailerNode.SelectNodes("App_Data[@Name='Dubbed_Languages']");
                foreach (XmlElement node in dataNodes)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Dubbed_Languages, node.GetAttribute("Value"));

                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Bit_Rate']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Bit_Rate, dataNode.GetAttribute("Value"));



                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Content_FileSize']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Content_FileSize, dataNode.GetAttribute("Value"));


                dataNode = (XmlElement)trailerNode.SelectSingleNode("App_Data[@Name='Content_CheckSum']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_Content_CheckSum, dataNode.GetAttribute("Value"));


                dataNodes = trailerNode.SelectNodes("App_Data[@Name='trickModesRestricted']");
                foreach (XmlElement node in dataNodes)
                    content.AddPropertyValue(ColumbusContentProperties.Trailer_trickModesRestricted, node.GetAttribute("Value"));

            }

          


            // Fetch box cover info

            XmlNode boxNodeNode = GetNodeByType(cableLabsXml, "box cover");
            if (boxNodeNode != null)
            {
                amsNode = boxNodeNode.SelectSingleNode("AMS");
                if (amsNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.BoxCover_AMS, TrimAMSNode(amsNode));

                dataNode = (XmlElement)boxNodeNode.SelectSingleNode("App_Data[@Name='Image_Aspect_Ratio']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.BoxCover_Image_Aspect_Ratio, dataNode.GetAttribute("Value"));

                dataNode = (XmlElement)boxNodeNode.SelectSingleNode("App_Data[@Name='Content_FileSize']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.BoxCover_Content_FileSize, dataNode.GetAttribute("Value"));

                dataNode = (XmlElement)boxNodeNode.SelectSingleNode("App_Data[@Name='Content_CheckSum']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.BoxCover_Content_CheckSum, dataNode.GetAttribute("Value"));




            }

            // Fetch poster info
            XmlNode posterNodeNode = GetNodeByType(cableLabsXml, "poster");
            if (posterNodeNode != null)
            {
                amsNode = posterNodeNode.SelectSingleNode("AMS");
                if (amsNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Poster_AMS, TrimAMSNode(amsNode));

                dataNode = (XmlElement)posterNodeNode.SelectSingleNode("App_Data[@Name='Image_Aspect_Ratio']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Poster_Image_Aspect_Ratio, dataNode.GetAttribute("Value"));

                dataNode = (XmlElement)posterNodeNode.SelectSingleNode("App_Data[@Name='Content_FileSize']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Poster_Content_FileSize, dataNode.GetAttribute("Value"));

                dataNode = (XmlElement)posterNodeNode.SelectSingleNode("App_Data[@Name='Content_CheckSum']");
                if (dataNode != null)
                    content.AddPropertyValue(ColumbusContentProperties.Poster_Content_CheckSum, dataNode.GetAttribute("Value"));
            }

            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            mppWrapper.UpdateContent(content, false);

            return new RequestResult(RequestResultState.Successful);
        }

        private string TrimAMSNode(XmlNode amsNode)
        {
            String xml = amsNode.OuterXml;
            xml = xml.Replace("<AMS ", "");
            xml = xml.Replace("/>", "");
            return xml;
        }

        public static XmlNode GetTitleNode(XmlDocument cableLabsXml)
        {
            XmlNode adNode = cableLabsXml.SelectSingleNode("ADI/Asset");
            return adNode.SelectSingleNode("Metadata");

        }

        private static XmlNode GetNodeByType(XmlDocument cableLabsXml, string typeName)
        {
            XmlNodeList nodes = cableLabsXml.SelectNodes("ADI/Asset/Asset");
            foreach (XmlElement adNode in nodes)
            {
                XmlElement typeNode = (XmlElement)adNode.SelectSingleNode("Metadata/App_Data[@Name='Type']");
                if (typeNode == null)
                    continue;
                if (typeNode.GetAttribute("Value").Equals(typeName, StringComparison.OrdinalIgnoreCase))
                {
                    return adNode.SelectSingleNode("Metadata");
                }
            }
            return null;
        }
    }
}
