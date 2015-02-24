using System.Linq;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class CreateContegoVodContent
    {
        private static string _xmlfilepath;
        public readonly IngestConfig _ingestConfig;


        public CreateContegoVodContent(IngestConfig ingestConfig, string xmlfilepath)
        {
            _xmlfilepath = xmlfilepath;
            _ingestConfig = ingestConfig;
        }
        public ContentData GenerateVodContent()
        {
            var cxt = new CableLabsXmlTranslator();
            var channelXmlTranslator = new ChannelXmlTranslator();
            XmlDocument xd = new XmlDocument();
            xd.Load(_xmlfilepath);
            ContentData cd = null;
            if (_ingestConfig.IngestXMLTypes.Count()>0)
            {
                foreach (var i in _ingestConfig.IngestXMLTypes)
                {
                    if (i.Trim() == "Channel_1_0")
                    {
                        cd = channelXmlTranslator.TranslateXmlToContentData(_ingestConfig, xd);
                    }
                    else
                    {
                        cd = cxt.TranslateXmlToContentData(_ingestConfig, xd);
                    }
                }
            }
            else
            {
                cd = cxt.TranslateXmlToContentData(_ingestConfig, xd);
            }
            return cd;
        }

    }
}
