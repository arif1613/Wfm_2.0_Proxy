using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core
{
    public class ValidateXml
    {
        private static string _cmdMessage;
        private static string _xsdPath;
        private static FileInfo _fileInfo;

        public ValidateXml(FileInfo fi, string xsdPath)
        {
            _fileInfo = fi;
            _xsdPath = xsdPath;
            _cmdMessage = null;
        }
        private void ValidationCallBack(object sender, ValidationEventArgs e)
        {
            if (e != null)
            {
                _cmdMessage = e.Exception.Message;
            }
        }
        public string Validate()
        {
            var schemafile = new XmlDocument();
            schemafile.Load(_xsdPath);
            var xmld = new XmlDocument();
            xmld.Load(_fileInfo.FullName);
            xmld.Schemas.Add(null, schemafile.BaseURI);
            xmld.Validate(ValidationCallBack);
            return _cmdMessage;
        }
    }
}
