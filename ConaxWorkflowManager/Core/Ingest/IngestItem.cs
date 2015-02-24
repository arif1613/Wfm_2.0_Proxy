using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest
{
    public class IngestItem
    {
        public IngestItem() {
            MultipleServicePrices = new Dictionary<MultipleContentService, List<MultipleServicePrice>>();
        }
        public XmlDocument OriginalIngestXML { get; set; }
        public String OriginalIngestXMLPath { get; set; }
        public ContentData contentData { get; set; }
        public Dictionary<MultipleContentService, List<MultipleServicePrice>> MultipleServicePrices { get; set; }
        public IngestType Type { get; set; }
        public bool ExistsInMpp { get; set; }
    }
}
