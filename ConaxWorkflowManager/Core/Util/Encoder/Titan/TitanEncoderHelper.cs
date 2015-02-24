using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Titan
{
    public class TitanEncoderHelper : EncoderHelper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static String GetProfileID(String encoderName, ContentData content, Boolean trailer)
        {
            log.Debug("In getProfileID for content with name " + content.Name + " trailer= " + trailer.ToString() + " encodertype= " + encoderName);
            List<ProfileValues> matches = GetProfilesFromBasicMatches(encoderName, content, trailer);
            ProfileValues profileMatch = DoEncoderSpecificMatch(matches, content, trailer);
            return profileMatch.Name;
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
    }
}
