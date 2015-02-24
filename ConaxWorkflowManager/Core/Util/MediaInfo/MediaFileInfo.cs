using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.MediaInfo
{
    public class MediaFileInfo
    {
        public List<AudioInfo> AudioInfos = new List<AudioInfo>();
        public List<SubtitleInfo> SubtitleInfos = new List<SubtitleInfo>();
        public int Width;
        public int Height;
        public string DisplayAspectRatio;
    }

    public class SubtitleInfo
    {
        public string Language;
        public string Format;

        public SubtitleInfo(string lang, string format)
        {
            Language = lang;
            Format = format;
        }
    }

    public class AudioInfo
    {
        public string Language;
        public string ID;

        public AudioInfo(string lang, string pid)
        {
            Language = lang;
            ID = pid;
        }
    }
}
