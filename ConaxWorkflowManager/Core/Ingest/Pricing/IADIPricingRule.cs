using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.Pricing
{
    public interface IADIPricingRule
    {
        Dictionary<MultipleContentService, List<MultipleServicePrice>> GetPrice(IngestConfig ingestConfig, List<MultipleContentService> connectedServices, XmlDocument priceXml, String name);
    }
}
