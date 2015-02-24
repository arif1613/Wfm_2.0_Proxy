using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder
{
    public class ProfileValues
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public String ID { get; set; }

        public String Name { get; set; }

        public String Resolution { get; set; }

        public String ResolutionHeight { get; set; }

        public String ResolutionWidth { get; set; }

        public List<AssetFormatType> AssetFormatTypes { get; set; }

        public List<String> AudioTracks { get; set; }

        //public List<String> AudioTrackPids { get; set; }

        public List<String> SubtitleLanguages { get; set; }

        public String SubtitleFormat { get; set; }

       // public String SubtitleType { get; set; }

        public String InputContainer { get; set; }

        public String Encoder { get; set; }

        public String Service { get; set; }

        public String ContentProvider { get; set; }

        public bool Encrypted { get; set; }

        public String DisplayAspectRatio { get; set; }

        public bool ContainsAllSubtitleLanguages(List<String> languages)
        {
            if (SubtitleLanguages.Count == 0)
            {
                log.Debug("No subtitle languages on profile, matches all");
                return true;
            }
            if (SubtitleLanguages[0].Equals("*") && languages.Count > 0)
            {
                log.Debug("subtitle language set to * and have atleast one subtitle on source, returning true");
                return true;
            }

            if (SubtitleLanguages.Count() != languages.Count())
            {
                log.Debug("Profiles have different count of subtitle languages, not match");
                return false;
            }

            foreach (String language in languages)
            {
                if (!SubtitleLanguages.Contains(language))
                {
                    log.Debug("Language " + language + " doesn't exist on profile, no match");
                    return false;
                }
            }
            return true;
        }

        public bool HaveMatchingSubtitleFormat(String subtitleFormat, int noOfSubtitles)
        {
            bool match = (SubtitleFormat.Equals("*") && noOfSubtitles > 0) || String.IsNullOrEmpty(SubtitleFormat) || SubtitleFormat.Equals(subtitleFormat, StringComparison.OrdinalIgnoreCase);
            log.Debug("subtitle format " + subtitleFormat + " match= " + match.ToString());
            return match;
        }

        /// <summary>
        /// Checks how many of the sent languages that exists on a profile. If not all exists -1 will be returned, othervise the no of languages will be returned
        /// </summary>
        /// <param name="languages"></param>
        /// <returns></returns>
        public int NoOfMatchingLanguages(List<String> languages)
        {
            foreach (String lan in languages)
                log.Debug(lan);
            if (AudioTracks.Count() > languages.Count()) // not all expected languages exists on source
                return -1;
            int existingLanguages = 0;
            foreach (String language in AudioTracks)
            {
                log.Debug("checking language " + language);
                if (!languages.Contains(language))
                    return -1;
                else
                    existingLanguages++;
            }
            return existingLanguages;
        }

        public int NoOfLanguagesIfLessThenInput(List<String> languages)
        {
            if (AudioTracks.Count() > languages.Count()) // not all expected languages exists on source
                return -1;
            return AudioTracks.Count();
        }

        public bool HaveSameLanguagesCount(List<String> languages)
        {
            log.Debug("checking language counts, input languages= " + languages.Count().ToString() + " profile languages = " + AudioTracks.Count().ToString());
            if (AudioTracks.Count() == languages.Count()) 
                return true;
            return false;
        }

        public bool ContainsAllAssetFormatTypes(List<AssetFormatType> assetFormats)
        {
            if (assetFormats.Count() != AssetFormatTypes.Count())
                return false;
            foreach (AssetFormatType assetFormat in assetFormats)
            {
                if (!AssetFormatTypes.Contains(assetFormat))
                    return false;
            }
            return true;
        }

        public bool MatchesResolution(int resolutionHeight, int resolutionWidth)
        {
            log.Debug("Checking resolution, media info height= " + resolutionHeight.ToString() + ", width = " + resolutionWidth.ToString());
            String[] parts = ResolutionHeight.ToLower().Split("and".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            log.Debug("Checking height resolution, no of parts to check = " + parts.Count());
            foreach (String resolutionStatement in parts)
            {
                log.Debug("checking statement = " + resolutionStatement);
                if (!ResolutionIsOk(resolutionHeight, resolutionStatement))
                {
                    log.Debug("Resolution height out of boundaries");
                    return false;
                }
                else
                {
                    log.Debug("Resolution height check ok");
                }
            }

            parts = ResolutionWidth.ToLower().Split("and".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            log.Debug("Checking width resolution, no of parts to check = " + parts.Count());
            foreach (String resolutionStatement in parts)
            {
                if (!ResolutionIsOk(resolutionWidth, resolutionStatement))
                {
                    log.Debug("Resolution width out of bounderies");
                    return false;
                }
                else
                {
                    log.Debug("Resolution width check ok");
                }
            }
            return true;
        }

        private bool ResolutionIsOk(int resolution, string resolutionStatement)
        {
            resolutionStatement = resolutionStatement.Replace(" ", "");
            int indexOfFirstNumber = resolutionStatement.IndexOfAny("1234567890".ToCharArray());
            String comparisonType = resolutionStatement.Substring(0, indexOfFirstNumber);
            int resolutionBoundery = int.Parse(resolutionStatement.Substring(indexOfFirstNumber));
            if (comparisonType.Equals("<="))
                return resolution <= resolutionBoundery;
            else if (comparisonType.Equals("<"))
                return resolution < resolutionBoundery;
            else if (comparisonType.Equals(">="))
                return resolution >= resolutionBoundery;
            else if (comparisonType.Equals(">"))
                return resolution > resolutionBoundery;
            else
                throw new Exception("No valid operator found for Resolution check, resolution statement= " + resolutionStatement);

        }

    }

    
}
