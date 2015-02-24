using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler;
using System.Xml;
using System.Xml.Schema;
using log4net;
using System.Reflection;
using System.Net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.XML
{
    public abstract class BaseStorageIngestHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected IFileHandler FileHandler;

        public BaseStorageIngestHandler(IFileHandler fileHandler)
        {
            this.FileHandler = fileHandler;
        }

        public void ValidateXML(FileInformation xmlFile, String XSDFile)
        {
            XmlDocument xdoc = new XmlDocument();
            ValidationEventHandler eventHandler = new ValidationEventHandler(XmlValidationEventHandler);

            try
            {
                //xdoc.Load(xmlFile.Path);
                xdoc = CommonUtil.LoadXML(xmlFile.Path);
                xdoc.Schemas.Add(null, XSDFile);
                xdoc.Validate(eventHandler);
            }
            catch (WebException ex)
            {
                // most likely 404 page not found, can't located XSD file.
                // that's ok, we will just skip the XSD validation then.
                if (ex.Response != null && ex.Response.ResponseUri != null)
                    log.Warn(ex.Message + " " + ex.Response.ResponseUri.ToString(), ex);
                else
                    log.Warn(ex.Message, ex);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // can't locate XSD file.
                // that's ok, we will just skip the XSD validation then.
                log.Warn(ex.Message + " " + ex.ToString(), ex);
            }
        }

        private static void XmlValidationEventHandler(object sender, ValidationEventArgs e)
        {
            switch (e.Severity)
            {
                case XmlSeverityType.Error:
                    string message = e.Message.Replace("The value '' is invalid according to its datatype 'NonEmptyString'", "The value cannot be an empty string");
                    throw new XmlSchemaValidationException(message);
                case XmlSeverityType.Warning:
                    // not handling it atm.
                    break;
            }
        }

        public abstract IngestItem GetCompleteCRUDIngest(IngestConfig ingestConfig, IngestItem ingestItem);

        public abstract IngestItem InitIngestItem(IngestConfig ingestConfig, FileInformation fileInformation, String ingestIdentifier);
    }
}
