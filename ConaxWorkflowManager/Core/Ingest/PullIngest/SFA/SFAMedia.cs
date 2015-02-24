using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest.SFA
{
    public class SFAMedia
    {
        public XmlDocument CableLabsXml { get; set; }

        public string VideoFileName { get; set; }
        public string VideoUrl { get; set; }
        public string videoPath { get; set; }
        public string EncryptedFileName { get; set; }

        public string TrailerFilename { get; set; }
        public string NewTrailerFilename { get; set; }
        public string TrailerUrl { get; set; }
        public string TrailerPath { get; set; }

        public string ImageFileName { get; set; }
        public string ImageUrl { get; set; }
    }
}
