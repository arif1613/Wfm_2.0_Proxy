using System;
using System.Collections.Generic;
using System.Linq;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Elemental
{
    public class ElementalEncoderHelper : EncoderHelper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /*  important */

        public static String GetProfileID(String encoderName, ContentData content, Boolean trailer)
        {
            List<ProfileValues> matches = GetProfilesFromBasicMatches(encoderName, content, trailer);
            ProfileValues profileMatch = DoEncoderSpecificMatch(matches, content, trailer);
            Console.WriteLine("Found profile match for source, using " + profileMatch.Name);
            return profileMatch.Name;
        }

        /*  important */

        protected static ProfileValues DoEncoderSpecificMatch(List<ProfileValues> profiles, ContentData content, Boolean trailer)
        {
            log.Debug("Found matches before languagecheck= " + profiles.Count().ToString());
            Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == trailer);
            List<String> languages = ConaxIntegrationHelper.GetAudioTrackLanguages(asset);

            ProfileValues profileMatch = null;
            foreach (ProfileValues profile in profiles)
            {
                log.Debug("Checking languages for profile " + profile.Name);
                if (profile.AudioTracks.Contains("*") || profile.HaveSameLanguagesCount(languages))
                {
                    log.Debug("Found a profile with same audio count");
                    profileMatch = profile;
                    break;
                }
            }
            if (profileMatch != null)
            {
                return profileMatch;
            }
            else
            {
                Console.WriteLine("No profile match with correct amount of audio tracks was found");
                return null;
            }
        }

        //public static String GetProfileID(ContentData content, Boolean trailer)
        //{
        //    log.Debug("In getProfileID for content with name " + content.Name + " trailer= " + trailer.ToString());
        //    //var encoderConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
        //    try
        //    {
        //        Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == trailer);

        //        String fileContainer = GetFileType(asset);
        //        List<ProfileValues> profiles = LoadAndParsePresetList();
        //        Resolution resolution = ConaxIntegrationHelper.GetResolution(asset);
        //        List<String> languages = ConaxIntegrationHelper.GetAudioTrackLanguages(asset);
        //        log.Debug("FileContainer= " + fileContainer);
        //        log.Debug("Encrypted = " + trailer.ToString());
        //        var profileSelection = profiles.Where<ProfileValues>(p => p.Encoder.Equals("Elemental", StringComparison.OrdinalIgnoreCase) && p.Encrypted != trailer && p.InputContainer.Equals(fileContainer, StringComparison.OrdinalIgnoreCase) && p.MatchesResolution(resolution.Height, resolution.Width));
        //        ProfileValues profileMatch = null;
        //        if (profileSelection.Count() == 0)
        //            throw new Exception("No profile match was found matching the first search criterias");


        //    }
        //    catch (Exception ex)
        //    {
        //        log.Error("Error fetching profileName", ex);
        //        throw;
        //    }
        //}
    }
}
