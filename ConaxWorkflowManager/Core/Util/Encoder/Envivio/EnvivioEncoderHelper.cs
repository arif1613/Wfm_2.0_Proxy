using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Envivio
{
    public class EnvivioEncoderHelper : EncoderHelper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static String GetProfileID(String encoderName, ContentData content, Boolean trailer)
        {
            log.Debug("In getProfileID for content with name " + content.Name + " trailer= " + trailer.ToString() + " encodertype= " + encoderName);
            List<ProfileValues> matches = GetProfilesFromBasicMatches(encoderName, content, trailer);
            ProfileValues profileMatch = DoEncoderSpecificMatch(matches, content, trailer);
            return profileMatch.ID;
        }

        protected static ProfileValues DoEncoderSpecificMatch(List<ProfileValues> profiles, ContentData content, Boolean trailer)
        {
            try
            {
                ProfileValues profileMatch = null;
                log.Debug("Found matches before languagecheck= " + profiles.Count().ToString());
                Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == trailer);
                List<String> languages = ConaxIntegrationHelper.GetAudioTrackLanguageWithPids(asset);

                int highestMatch = 0;
                foreach (ProfileValues profile in profiles)
                {
                    log.Debug("Checking languages for profile " + profile.Name);
                    int matches = profile.NoOfMatchingLanguages(languages);
                    if (matches != -1)
                    {
                        log.Debug("Found " + matches.ToString() + " matching languages");
                        if (matches > highestMatch)
                        {
                            log.Debug("Found higher matching profile");
                            highestMatch = matches;
                            profileMatch = profile;
                        }
                    }
                }
                if (profileMatch != null)
                    return profileMatch;
                else
                    throw new Exception("No profile matching the right combination of languages and pids was found");
            }
            catch (Exception ex)
            {
                log.Error("Error fetching profileName", ex);
                throw;
            }
        }

        //public static String GetProfileID(ContentData content, Boolean trailer)
        //{
        //    log.Debug("In getProfileID for content with name " + content.Name + " trailer= " + trailer.ToString() + " and encoderType= " + encoderType);
        //    var encoderConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
        //    try
        //    {
        //        Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == trailer);
        //        // ResolutionType resolution = ConaxIntegrationHelper.GetResolutionType(asset);
        //        //if (resolution == ResolutionType.NotSpecified)
        //        //     throw new Exception("No resolution found for content " + content.Name);
        //        //  log.Debug("ResolutionType= " + resolution.ToString());
        //        String fileContainer = GetFileType(asset);
        //        List<ProfileValues> profiles = LoadAndParsePresetList();
        //        Resolution resolution = ConaxIntegrationHelper.GetResolution(asset);

        //        List<String> languages = ConaxIntegrationHelper.GetAudioTrackLanguageWithPids(asset);

        //        log.Debug("FileContainer= " + fileContainer);
        //        log.Debug("Encrypted = " + trailer.ToString());
        //        log.Debug("Encoder type = Envivio");
        //        var profileSelection = profiles.Where<ProfileValues>(p => p.Encoder.Equals("Envivio", StringComparison.OrdinalIgnoreCase) && p.Encrypted != trailer && p.InputContainer.Equals(fileContainer, StringComparison.OrdinalIgnoreCase) && p.MatchesResolution(resolution.Height, resolution.Width));
        //        ProfileValues profileMatch = null;
        //        log.Debug("Found matches before languagecheck= " + profileSelection.Count().ToString());
        //        int highestMatch = 0;
        //        foreach (ProfileValues profile in profileSelection)
        //        {
        //            log.Debug("Checking languages for profile " + profile.Name);
        //            int matches = profile.NoOfMatchingLanguages(languages);
        //            if (matches != -1)
        //            {
        //                log.Debug("Found " + matches.ToString() + " matching languages");
        //                if (matches > highestMatch)
        //                {
        //                    log.Debug("Found higher matching profile");
        //                    highestMatch = matches;
        //                    profileMatch = profile;
        //                }
        //            }
        //        }
        //        if (profileMatch != null)
        //            return profileMatch.Name;
        //        else
        //            throw new Exception("No profile match was found");
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Error("Error fetching profileName", ex);
        //        throw;
        //    }

        //}
    }
}
