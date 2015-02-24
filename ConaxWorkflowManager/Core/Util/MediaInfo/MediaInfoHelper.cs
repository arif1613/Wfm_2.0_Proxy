using MediaInfoLib;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.MediaInfo
{
    public class MediaInfoHelper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static MediaFileInfo GetMediaInfoForFile(string fileName)
        {
            var mfi = new MediaFileInfo();

            var  mi = new MediaInfoLib.MediaInfo();

            mi.Open(fileName);

            mfi.Width = int.Parse(mi.Get(StreamKind.Video, 0, "Width"));
            mfi.Height = int.Parse(mi.Get(StreamKind.Video, 0, "Height"));

            // examine audio tracks
            int audioCount = mi.Count_Get(StreamKind.Audio);
            for (int i = 0; i < audioCount; i++)
            {
                string lang = mi.Get(StreamKind.Audio, i, "Language");
                string id = mi.Get(StreamKind.Audio, i, "ID");
                mfi.AudioInfos.Add(new AudioInfo(lang, id));
            }

            // examine subtitles
            int subCount = mi.Count_Get(StreamKind.Text);
            for (int i = 0; i < subCount; i++)
            {
                string lang = mi.Get(StreamKind.Text, i, "Language");
                string format = mi.Get(StreamKind.Text, i, "Format");
                mfi.SubtitleInfos.Add(new SubtitleInfo(lang, format));
            }

            // examine display aspect ratio (not needed?)
            string dar = mi.Get(StreamKind.Video, 0, 132);
            log.Debug("Aspect Ratio = " + dar);
            mfi.DisplayAspectRatio = dar;

            mi.Close();
            return mfi;
        }
    }
}
