using System;
using System.Collections.Generic;
using System.Linq;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder;
using System.IO;
using System.Data;
using System.Data.OleDb;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder
{
    public class EncoderHelper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected static string GetFileType(Asset asset)
        {
            int fileExtensionPos = asset.Name.LastIndexOf('.');
            return asset.Name.Substring(fileExtensionPos + 1);
        }

        protected static List<ProfileValues> GetProfilesFromBasicMatches(String encoderName, ContentData content, Boolean trailer)
        {
            //Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == trailer);
            Asset asset = content.Assets.Where(r => r.IsTrailer == false).FirstOrDefault();
            String fileContainer = GetFileType(asset);

            String displayAspectRating = ConaxIntegrationHelper.GetDisplayAspectRatio(asset);
            List<AssetFormatType> assetFormatTypes = ConaxIntegrationHelper.GetEncodingTypes(content, trailer);
            List<ProfileValues> allProfiles = LoadAndParsePresetList();
            var encoderSpecificprofiles = allProfiles.Where<ProfileValues>(p => p.Encoder.Equals(encoderName, StringComparison.OrdinalIgnoreCase));
            Resolution resolution = ConaxIntegrationHelper.GetResolution(asset);
            List<String> languages = ConaxIntegrationHelper.GetAudioTrackLanguages(asset);
            String contentRightsOwner = content.ContentRightsOwner.Name;
            String subtitleFormat = ConaxIntegrationHelper.GetSubtitleFormat(asset);
            List<String> subtitleLanguages = ConaxIntegrationHelper.GetSubtitleLanguages(asset);


            List<ProfileValues> profileMatches = new List<ProfileValues>();
            foreach (ProfileValues pf in encoderSpecificprofiles)
            {
                String profileName = pf.Name;
                //log.Debug("Checking if profile " + profileName + ", id " + pf.ID + " matches");
                String mismatchError = CheckProfile(encoderName, trailer, fileContainer, displayAspectRating, assetFormatTypes, resolution, contentRightsOwner, subtitleFormat, subtitleLanguages, pf);
                if (String.IsNullOrEmpty(mismatchError))
                    profileMatches.Add(pf);
                else
                    log.Debug(Environment.NewLine + "Profile with name " + profileName + " and id " + pf.ID + " didnt match because of:" + Environment.NewLine + mismatchError);
            }

            if (profileMatches.Count() == 0)
            {
                Console.WriteLine("No profile match was found matching the search criterias");
            }
            else
            {
                Console.WriteLine("Found " + profileMatches.Count() + " profiles from search criterias");
            }
            return profileMatches;
        }

        private static String CheckProfile(String encoderName, Boolean trailer, String fileContainer, String displayAspectRating, List<AssetFormatType> assetFormatTypes, Resolution resolution, String contentRightsOwner, String subtitleFormat, List<String> subtitleLanguages, ProfileValues pf)
        {
            String mismatchError = "";
            bool encrypted = !trailer;

            if (pf.Encrypted != encrypted)
                mismatchError += Environment.NewLine + "Encryption didn't match, expected " + encrypted.ToString() + ", found = " + pf.Encrypted;
            if (!(pf.InputContainer.Equals("*") || pf.InputContainer.Equals(fileContainer, StringComparison.OrdinalIgnoreCase)))
                mismatchError += Environment.NewLine + "InputContainer didn't match, expected = " + fileContainer + ", found = " + pf.InputContainer;
            try
            {
                if (!pf.MatchesResolution(resolution.Height, resolution.Width))
                    mismatchError += Environment.NewLine + "Resolution didn't match";
            }
            catch (Exception)
            {
                Console.WriteLine("NO valid media infos found");

            }

            if (!pf.ContainsAllAssetFormatTypes(assetFormatTypes))
                mismatchError += Environment.NewLine + "Profile didn't contain all assetFormatTypes";
            if (!(pf.DisplayAspectRatio.Equals("*") || pf.DisplayAspectRatio.Equals(displayAspectRating)))
                mismatchError += Environment.NewLine + "DisplayAspectRation didnt match, expected = " + displayAspectRating + ", found = " + pf.DisplayAspectRatio;
            if (!(pf.ContentProvider.Equals("*") || pf.ContentProvider.Equals(contentRightsOwner, StringComparison.OrdinalIgnoreCase)))
                mismatchError += "ContentProvider didn't match, expected= " + contentRightsOwner + " found = " + pf.ContentProvider;
            if (!pf.HaveMatchingSubtitleFormat(subtitleFormat, subtitleLanguages.Count))
                mismatchError += "SubtitleFormat didn't match, expected= " + subtitleFormat + " found = " + pf.SubtitleFormat;
            if (!pf.ContainsAllSubtitleLanguages(subtitleLanguages))
                mismatchError += "Profile didnt have all subtitles";
            return mismatchError;
        }

        protected static List<ProfileValues> LoadAndParsePresetList()
        {
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String fileName = "";
            if (managerConfig.ConfigParams.ContainsKey("EncoderProfileMapping"))
            {
                fileName = managerConfig.GetConfigParam("EncoderProfileMapping");
            }
            else
            {
                throw new Exception("No EncoderProfileMapping file was found in configuration");
            }
            log.Debug("<---------------------------------------- profile handling ----------------------------------------------------->");
            List<ProfileValues> ret = new List<ProfileValues>();

            OleDbConnection connection;

            connection = new System.Data.OleDb.OleDbConnection("provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + fileName + "';Extended Properties=Excel 12.0 Xml;");
            log.Debug("connection created");
            try
            {
                //After connecting to the Excel sheet here we are selecting the data 
                //using select statement from the Excel sheet
                OleDbCommand ocmd = new OleDbCommand("select distinct PROFILE_ID from [Sheet1$]", connection);
                connection.Open();  //Here [Sheet1$] is the name of the sheet 
                log.Debug("Connection opened");
                //in the Excel file where the data is present
                OleDbDataReader reader = ocmd.ExecuteReader();
                List<String> profileIDs = GetAllProfileIDs(reader);
                foreach (String profileID in profileIDs)
                {
                    log.Debug("Adding profileID " + profileID);
                    ret.Add(BuildProfile(profileID, connection));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                log.Error("Error loading profiles from excel file", ex);
                throw;
            }
            try
            {
                connection.Close();
            }
            catch (Exception exc)
            {
                log.Debug(exc.Message);
            }
            return ret;
        }

        private static List<string> GetAllProfileIDs(OleDbDataReader reader)
        {
            List<String> profileIDs = new List<string>();
            while (reader.Read())
            {
                object o = reader["PROFILE_ID"];
                if (o != null && !String.IsNullOrEmpty(o.ToString()))
                    profileIDs.Add(o.ToString());
            }
            return profileIDs;
        }

        private static ProfileValues BuildProfile(string profileID, OleDbConnection connection)
        {
            ProfileValues ret = new ProfileValues();
            try
            {
                OleDbCommand ocmd = new OleDbCommand("select * from [Sheet1$] where PROFILE_ID='" + profileID + "'", connection);
                OleDbDataReader reader = ocmd.ExecuteReader();
                reader.Read();

                ret.ID = GetValue(reader, "PROFILE_ID");

                ret.Name = GetValue(reader, "NAME");

                ret.Encoder = GetValue(reader, "ENCODER");

                ret.Service = GetValue(reader, "SERVICE");

                ret.ContentProvider = GetValue(reader, "CONTENT_PROVIDER");

                ret.InputContainer = GetValue(reader, "INPUT_CONTAINER");

                ret.AssetFormatTypes = GetAllAssetFormats(GetAllValuesFromSameRow(reader, "OUTPUT_FORMAT"));

                ret.Resolution = GetValue(reader, "RESOLUTION");

                ret.ResolutionHeight = GetValue(reader, "RESOLUTION_HEIGHT");

                ret.ResolutionWidth = GetValue(reader, "RESOLUTION_WIDTH");

                // ret.SubtitleType = GetValue(reader, "SUBTITLES_TYPE");

                ret.DisplayAspectRatio = GetValue(reader, "DISPLAY_AR");

                String encrypted = GetValue(reader, "ENCRYPTION");
                ret.Encrypted = encrypted.Equals("yes", StringComparison.OrdinalIgnoreCase);
                String value = "";
                ret.AudioTracks = new List<string>();
                value = GetValue(reader, "AUDIO_TRACKS");
                if (!String.IsNullOrEmpty(value))
                    ret.AudioTracks.Add(value);
                ret.SubtitleLanguages = new List<string>();
                value = GetValue(reader, "SUBTITLES_LANGUAGE");
                if (!String.IsNullOrEmpty(value))
                    ret.SubtitleLanguages.Add(value);
                ret.SubtitleFormat = GetValue(reader, "SUBTITLES_FORMAT");

                while (reader.Read())
                {
                    value = GetValue(reader, "AUDIO_TRACKS");
                    if (!String.IsNullOrEmpty(value))
                        ret.AudioTracks.Add(value);
                    value = GetValue(reader, "SUBTITLES_LANGUAGE");
                    if (!String.IsNullOrEmpty(value))
                        ret.SubtitleLanguages.Add(value);
                }
            }
            catch (DataException ex)
            {
                log.Error("Error building profile", ex);
            }

            return ret;
        }

        private static List<AssetFormatType> GetAllAssetFormats(List<string> assetFormats)
        {
            List<AssetFormatType> ret = new List<AssetFormatType>();
            foreach (String assetFormatType in assetFormats)
            {
                try
                {
                    AssetFormatType formatType = (AssetFormatType)Enum.Parse(typeof(AssetFormatType), assetFormatType);
                    if (!ret.Contains(formatType))
                        ret.Add(formatType);
                }
                catch (Exception ex)
                {
                    log.Error("Error parsing assetFormatType to enum", ex);
                    throw;
                }
            }
            return ret;
        }

        internal static string GetValue(OleDbDataReader reader, String field)
        {
            object value = reader[field];
            if (value != null)
                return value.ToString();
            else
                return "";
        }

        internal static List<string> GetAllValuesFromSameRow(OleDbDataReader reader, String field)
        {
            List<String> ret = new List<string>();

            String value = "";
            object o = reader[field];
            if (o != null)
            {
                value = o.ToString();
                if (!String.IsNullOrEmpty(value))
                {
                    String[] values = value.Split(',');
                    foreach (String val in values)
                        ret.Add(val.Trim());
                }
            }


            return ret;

        }

        public static String GetFileAreaPath(ContentData content, bool isTrailer)
        {
            var xTendManagerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

            String fileAreaRoot = xTendManagerConfig.GetConfigParam("FileAreaRoot");

            if (isTrailer)
            {
                if (xTendManagerConfig.ConfigParams.ContainsKey("FileAreaTrailerRoot") && !String.IsNullOrEmpty(xTendManagerConfig.GetConfigParam("FileAreaTrailerRoot")))
                    fileAreaRoot = xTendManagerConfig.GetConfigParam("FileAreaTrailerRoot");
            }

            String customerID = xTendManagerConfig.GetConfigParam("CustomerID");

            MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Asset asset = content.Assets.FirstOrDefault<MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Asset>(a => a.IsTrailer == isTrailer);
            String fileAreaPath = Path.Combine(fileAreaRoot, Path.GetDirectoryName(asset.Name).Replace(@"\", "/"), content.ObjectID.ToString() + "_" + customerID);
            return fileAreaPath;
        }
    }
}
