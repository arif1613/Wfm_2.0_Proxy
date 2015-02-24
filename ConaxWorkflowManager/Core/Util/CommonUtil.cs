using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.FileOperations;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Net;
using System.Net.Mail;
using System.Xml.Linq;
using System.Xml.XPath;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.Globalization;
using System.Xml.Serialization;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using System.Net.Sockets;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.EPG;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util
{
    public class CommonUtil
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static String WorkFlowStateName = "WorkFlowState";

        public static AssetFormatType GetAssetFormatTypeFromFileName(String fileName)
        {
            if (fileName.IndexOf(".isml", 0, StringComparison.OrdinalIgnoreCase) > 0)
                return AssetFormatType.SmoothStreaming;
            else if (fileName.IndexOf(".m3u8", 0, StringComparison.OrdinalIgnoreCase) > 0)
                return AssetFormatType.HTTPLiveStreaming; // ...m3u8
            else
                return AssetFormatType.Unknown;
        }

        public static bool ContentIsChannel(ContentData content)
        {
            if (content.Properties.FirstOrDefault(p => p.Type == CatchupContentProperties.CubiChannelId) != null)
            {
                return true;
            }
            return false;
        }

        public static string EncodeTo64UrlSafe(string toEncode)
        {
            String returnValue = CommonUtil.EncodeTo64(toEncode);
            // escape unsafe url char
            returnValue = returnValue.Replace('+', '-').Replace('/', '_');

            return returnValue;
        }

        public static string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes
                    = System.Text.Encoding.UTF8.GetBytes(toEncode);
            //= System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            String returnValue
                  = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        public static string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes
                = System.Convert.FromBase64String(encodedData);
            String returnValue =
                System.Text.Encoding.UTF8.GetString(encodedDataAsBytes);
            //System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);

            return returnValue;
        }

        public static List<List<T>> SplitIntoChunks<T>(List<T> list, Int32 chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentException("chunkSize must be greater than 0.");
            }

            List<List<T>> retVal = new List<List<T>>();
            Int32 index = 0;
            while (index < list.Count)
            {
                Int32 count = list.Count - index > chunkSize ? chunkSize : list.Count - index;
                retVal.Add(list.GetRange(index, count));

                index += chunkSize;
            }

            return retVal;
        }

        public static Dictionary<UInt64, List<ContentData>> SortContentByServices(List<ContentData> contents)
        {
            Dictionary<UInt64, List<ContentData>> cbs = new Dictionary<UInt64, List<ContentData>>();

            foreach (ContentData content in contents)
            {
                foreach (ContentAgreement agreement in content.ContentAgreements)
                {
                    foreach (MultipleContentService service in agreement.IncludedServices)
                    {
                        if (!cbs.ContainsKey(service.ObjectID.Value))
                        {
                            log.Debug("Adding service with objectID " + service.ObjectID.Value.ToString());
                            cbs.Add(service.ObjectID.Value, new List<ContentData>());
                        }
                        List<ContentData> contentlist = null;
                        if (cbs.TryGetValue(service.ObjectID.Value, out contentlist))
                            contentlist.Add(content);
                        else
                            log.Error("Failed to find contentlist for service " + service.ObjectID.Value);
                    }
                }
            }

            return cbs;
        }

        public static List<ContentData> FilterAlreadyExistingEpgs(List<ContentData> contents, UInt64 serviceObjectId)
        {
            // only filter exist catchup right now, not sure if possible to check npvr
            List<ContentData> newList = new List<ContentData>();

            // only get content that doesn't have cubiEpgId for this servcie. they are most lilkely new content.
            var filterList = contents.Where(c => c.Properties.Count(p => p.Type.Equals(CatchupContentProperties.CubiEpgId) && p.Value.StartsWith(serviceObjectId + ":")) == 0);
            newList.AddRange(filterList);


            return newList;
        }

        /// <summary>
        /// This method returns content that is not locked by the synkTask
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        public static List<ContentData> FilterLockedEpgs(List<ContentData> contents)
        {
            List<ContentData> newList = new List<ContentData>();

            foreach (ContentData content in contents)
            {
                if (!ContentLocker.ContentIsLocked(content))
                    newList.Add(content);
            }
            return newList;
        }


        public static void SetWorkFlowState(ContentData content, String workFlowStateXMLStr)
        {
            var property = content.Properties.Where(p => p.Type == WorkFlowStateName).SingleOrDefault();
            if (property == null)
            {
                content.Properties.Add(new Property(WorkFlowStateName, workFlowStateXMLStr));
            }
            else
            {
                property.Value = workFlowStateXMLStr;
            }
        }

        public static void SetWorkFlowState(MultipleServicePrice servicePrice, String workFlowStateXMLStr)
        {
            Int32 startPos = servicePrice.LongDescription.IndexOf("WorkFlowState={");
            Int32 endPos = servicePrice.LongDescription.LastIndexOf("}");

            if (startPos >= 0 && endPos >= 0)
            {
                startPos += 14;
                endPos += 1;
                String oldStr = servicePrice.LongDescription.Substring(startPos, endPos - startPos);
                servicePrice.LongDescription = servicePrice.LongDescription.Replace(oldStr, workFlowStateXMLStr);
            }
            else
            {
                servicePrice.LongDescription += "WorkFlowState=" + workFlowStateXMLStr + ",";
            }
        }

        public static void SendFailedVODIngestNotification(ContentData content, Exception e)
        {
            var proeprty = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName", StringComparison.OrdinalIgnoreCase));

            string exceptionString = e.Message + Environment.NewLine + e.StackTrace;
            SendFailedVODIngestNotification(proeprty.Value, exceptionString);
        }

        public static void SendFailedVODIngestNotification(String IngestXMLName, Exception e)
        {
            string exceptionString = e.Message + Environment.NewLine + e.StackTrace;
            SendFailedVODIngestNotification(IngestXMLName, exceptionString);
        }

        public static void SendFailedVODIngestNotification(ContentData content, String ErrorMessage)
        {
            var proeprty = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName", StringComparison.OrdinalIgnoreCase));
            SendFailedVODIngestNotification(proeprty.Value, ErrorMessage);
        }

        public static void SendFailedVODIngestNotification(String IngestXMLName, String ErrorMessage)
        {

            EmailTemplate emailTemplate = GetMailTemplate(EmailTemplateType.FailedVODIngest);
            if (emailTemplate != null)
            {
                emailTemplate.Subject = emailTemplate.Subject.Replace("{#IngestXML}", IngestXMLName);
                emailTemplate.Body = emailTemplate.Body.Replace("{#IngestXML}", IngestXMLName);

                if (string.IsNullOrEmpty(ErrorMessage))
                {
                    ErrorMessage = "Please see log for details.";
                }
                emailTemplate.Body = emailTemplate.Body.Replace("{#ErrorMessage}", Environment.NewLine + ErrorMessage);

                SendNotification(emailTemplate);
            }
        }

        public static void SendSuccessfulVODIngestNotification(ContentData content)
        {
            EmailTemplate emailTemplate = GetMailTemplate(EmailTemplateType.SuccessfulVODIngest);
            if (emailTemplate != null)
            {

                var proeprty = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName", StringComparison.OrdinalIgnoreCase));
                emailTemplate.Subject = emailTemplate.Subject.Replace("{#IngestXML}", proeprty.Value);
                emailTemplate.Body = emailTemplate.Body.Replace("{#IngestXML}", proeprty.Value);
                emailTemplate.Body = emailTemplate.Body.Replace("{#Content.Name}", content.Name);
                emailTemplate.Body = emailTemplate.Body.Replace("{#Content.Id}", content.ID.Value.ToString());

                SendNotification(emailTemplate);
            }
        }


        public static void SendFailedVODPublishNotification(ContentData content, MultipleContentService service, RequestResult result)
        {
            if (result.Ex != null)
                SendFailedVODPublishNotification(content, service, result.Ex);
            else
                SendFailedVODPublishNotification(content, service, result.Message);
        }

        public static void SendFailedVODPublishNotification(ContentData content, MultipleContentService service, Exception e)
        {
            String exceptionString = e.Message + Environment.NewLine + e.StackTrace;
            SendFailedVODPublishNotification(content, service, exceptionString);
        }

        public static void SendFailedVODPublishNotification(ContentData content, MultipleContentService service, String ErrorMessage)
        {

            EmailTemplate emailTemplate = GetMailTemplate(EmailTemplateType.FailedVODPublish);
            if (emailTemplate != null)
            {
                emailTemplate.Subject = emailTemplate.Subject.Replace("{#Content.Name}", content.Name);
                emailTemplate.Body = emailTemplate.Body.Replace("{#Content.Name}", content.Name);
                emailTemplate.Body = emailTemplate.Body.Replace("{#Content.Id}", content.ID.Value.ToString());
                emailTemplate.Body = emailTemplate.Body.Replace("{#Service.Name}", service.Name);
                emailTemplate.Body = emailTemplate.Body.Replace("{#Service.Id}", service.ID.Value.ToString());

                if (string.IsNullOrEmpty(ErrorMessage))
                {
                    ErrorMessage = "Please see log for details.";
                }
                emailTemplate.Body = emailTemplate.Body.Replace("{#ErrorMessage}", Environment.NewLine + ErrorMessage);

                SendNotification(emailTemplate);
            }
        }

        public static void SendSuccessfulVODPublishNotification(ContentData content, MultipleContentService service)
        {
            EmailTemplate emailTemplate = GetMailTemplate(EmailTemplateType.SuccessfulVODPublish);
            if (emailTemplate != null)
            {
                emailTemplate.Subject = emailTemplate.Subject.Replace("{#Content.Name}", content.Name);
                emailTemplate.Body = emailTemplate.Body.Replace("{#Content.Name}", content.Name);
                emailTemplate.Body = emailTemplate.Body.Replace("{#Content.Id}", content.ID.Value.ToString());
                emailTemplate.Body = emailTemplate.Body.Replace("{#Service.Name}", service.Name);
                emailTemplate.Body = emailTemplate.Body.Replace("{#Service.Id}", service.ID.Value.ToString());

                SendNotification(emailTemplate);
            }
        }

        private static void SendNotification(EmailTemplate emailTemplate)
        {
            try
            {
                var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

                if (!Boolean.Parse(managerConfig.GetConfigParam("SendNotification")))
                    return;

                List<String> recipients = new List<String>();
                if (emailTemplate.Type == EmailTemplateType.FailedVODIngest || emailTemplate.Type == EmailTemplateType.SuccessfulVODIngest)
                    recipients.AddRange(managerConfig.GetConfigParam("VODIngestNotificationRecipients").Split(';'));
                if (emailTemplate.Type == EmailTemplateType.FailedVODPublish || emailTemplate.Type == EmailTemplateType.SuccessfulVODPublish)
                    recipients.AddRange(managerConfig.GetConfigParam("VODPublishNotificationRecipients").Split(';'));

                try
                {
                    SendEmail(recipients, emailTemplate);
                }
                catch (Exception mex)
                {
                    log.Warn("Failed to Send Mail", mex);
                }
            }
            catch (Exception ex)
            {
                log.Warn("Failed to load VOD Ingest Notification Recipients.", ex);
            }
        }

        private static EmailTemplate GetMailTemplate(EmailTemplateType templateType)
        {

            EmailTemplate emailTemplate = null;
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

            try
            {
                XElement templates = XElement.Load(managerConfig.GetConfigParam("EmailTemplate"));
                XElement templateNode = templates.XPathSelectElement("EmailTemplate[@type='" + templateType.ToString("G") + "']");
                emailTemplate = new EmailTemplate();
                emailTemplate.Type = templateType;
                emailTemplate.From = templateNode.Element("From").Value;
                emailTemplate.Subject = templateNode.Element("Subject").Value;
                emailTemplate.Body = templateNode.Element("Body").Value;
            }
            catch (Exception ex)
            {
                log.Warn("Failed to load Email template " + templateType.ToString("G") + ".", ex);
            }
            return emailTemplate;
        }

        private static void SendEmail(List<String> recipients, EmailTemplate emailTemplate)
        {
            if (recipients.Count == 0)
                return;

            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

            String smtpHost = managerConfig.GetConfigParam("SMTPHost");
            Int32 smtpPort = Int32.Parse(managerConfig.GetConfigParam("SMTPPort"));

            string email = emailTemplate.From;
            //string password = "put-your-GMAIL-password-here";
            //var loginInfo = new NetworkCredential(email, password);
            var msg = new MailMessage();
            var smtpClient = new SmtpClient(smtpHost, smtpPort);

            msg.From = new MailAddress(email);
            foreach (String address in recipients)
            {
                msg.To.Add(new MailAddress(address));
            }
            msg.Subject = emailTemplate.Subject;
            msg.Body = emailTemplate.Body;
            msg.IsBodyHtml = true;

            smtpClient.EnableSsl = false;
            smtpClient.UseDefaultCredentials = true;
            //smtpClient.Credentials = loginInfo;
            smtpClient.Send(msg);
        }

        public static Dictionary<String, List<SSChunk>> GetSSChunksFromManifest(XmlDocument manifestDoc, DateTime UTCStartTime)
        {

            Dictionary<String, List<SSChunk>> manifestShunks = new Dictionary<String, List<SSChunk>>();

            // load manfiest
            XmlNodeList StreamIndexnodes = manifestDoc.SelectNodes("SmoothStreamingMedia/StreamIndex");


            foreach (XmlElement StreamIndexnode in StreamIndexnodes)
            {
                String streamIndexType = StreamIndexnode.GetAttribute("Type");
                String chunksCount = StreamIndexnode.GetAttribute("Chunks");

                DateTime streamIndexStarttime = UTCStartTime.AddSeconds(0);

                // chunks
                UInt32 chunkCount = UInt32.Parse(chunksCount);
                UInt32 counter = 0;
                SSChunk[] chunks = new SSChunk[chunkCount];
                XmlNodeList chunkNodes = StreamIndexnode.SelectNodes("c");
                foreach (XmlElement chunkNode in chunkNodes)
                {
                    SSChunk c = new SSChunk();
                    chunks[counter] = c;
                    if (!String.IsNullOrEmpty(chunkNode.GetAttribute("t")))
                    {
                        c.T = Int64.Parse(chunkNode.GetAttribute("t"));
                    }
                    else
                    {
                        if (counter == 0)
                        {
                            c.T = 0;
                        }
                        else
                        {
                            Int64 oldT = chunks[counter - 1].T;
                            Int32 oldD = chunks[counter - 1].D;
                            c.T = (oldT + oldD);
                        }
                    }

                    if (!String.IsNullOrEmpty(chunkNode.GetAttribute("d")))
                    {
                        c.D = Int32.Parse(chunkNode.GetAttribute("d"));
                        c.UTCStartTime = UTCStartTime.AddTicks(c.T);
                        c.UTCEndTime = c.UTCStartTime.AddTicks(c.D);
                    }
                    counter++;
                    if (!String.IsNullOrEmpty(chunkNode.GetAttribute("r")))
                    {
                        Int32 rep = Int32.Parse(chunkNode.GetAttribute("r"));
                        for (Int32 x = 1; x < rep; x++)
                        {
                            SSChunk rc = new SSChunk();
                            chunks[counter] = rc;
                            rc.D = c.D;
                            Int64 oldRT = chunks[counter - 1].T;
                            Int32 oldRD = chunks[counter - 1].D;
                            rc.T = oldRT + oldRD;
                            rc.UTCStartTime = UTCStartTime.AddTicks(rc.T);
                            rc.UTCEndTime = rc.UTCStartTime.AddTicks(rc.D);
                            counter++;
                        }
                    }
                }

                List<SSChunk> ssChunks = new List<SSChunk>();
                ssChunks.AddRange(chunks);
                manifestShunks.Add(streamIndexType, ssChunks);
            }

            return manifestShunks;
        }

        public static String GetThreeLetterISOLanguageName(String twoLetterISOLanguageName)
        {
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            foreach (var culture in cultures)
            {
                // log.Debug("mathing " + twoLetterISOLanguageName + " with " + culture.TwoLetterISOLanguageName);
                if (culture.TwoLetterISOLanguageName.Equals(twoLetterISOLanguageName))
                    return culture.ThreeLetterISOLanguageName;
            }
            if (twoLetterISOLanguageName.Equals("no", StringComparison.OrdinalIgnoreCase))
                return "nor";
            return "";
        }


        public static IngestXMLType GetIngestXMLType(String ingestXMLPath)
        {
            FileInfo fi=new FileInfo(ingestXMLPath);
            if (System.IO.File.Exists(ingestXMLPath))
            {
                XmlDocument ingestxmlDoc = new XmlDocument();
                ingestxmlDoc.Load(ingestXMLPath);
                return GetIngestXMLType(ingestxmlDoc);
            }
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            string uploadFolder = systemConfig.GetConfigParam("FileIngestUploadDirectory");
            string sourceFilename = Path.Combine(uploadFolder, fi.Directory.Name, fi.Name);
            new FileMover(sourceFilename, "encoderUpload");
            XmlDocument ingestxmlDoc1 = new XmlDocument();
            ingestxmlDoc1.Load(ingestXMLPath);
            return GetIngestXMLType(ingestxmlDoc1);
        }

        public static XmlDocument LoadXML(String filePath)
        {
            XmlTextReader XmlTextReader = new XmlTextReader(filePath);
            XmlTextReader.XmlResolver = null;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ValidationType = ValidationType.Schema;
            readerSettings.DtdProcessing = DtdProcessing.Ignore;
            XmlReader XmlReader = XmlReader.Create(XmlTextReader, readerSettings);
            XmlDocument doc = new XmlDocument();
            doc.Load(XmlReader);
            XmlReader.Close();
            return doc;
        }

        public static IngestXMLType GetIngestXMLType(XmlDocument ingestXML)
        {

            IngestXMLType result = IngestXMLType.CableLabs_1_1; // default type
            // check if it's cablelabs xml
            XmlNode cableLabsVersionNode = ingestXML.SelectSingleNode("ADI/Metadata/App_Data[@Name='Metadata_Spec_Version']");
            if (cableLabsVersionNode != null)
            {
                String cableLabsVerion = ((XmlElement)cableLabsVersionNode).GetAttribute("Value");

                if (cableLabsVerion.Equals("CableLabsVOD1.0", StringComparison.OrdinalIgnoreCase))
                    return IngestXMLType.CableLabs_1_0;
                else if (cableLabsVerion.Equals("CableLabsVOD1.1", StringComparison.OrdinalIgnoreCase))
                    return IngestXMLType.CableLabs_1_1;
                else
                {
                    throw new Exception("CableLabsVersion " + cableLabsVerion + " is not valid");
                }
            }

            // check if it's channel xml
            XmlNode ChannelXmlNode = ingestXML.SelectSingleNode("Channel");
            if (ChannelXmlNode != null)
            {
                if (ChannelXmlNode.Attributes["xmlVersion"] == null || String.IsNullOrEmpty(ChannelXmlNode.Attributes["xmlVersion"].Value))
                {
                    throw new Exception("Channel xmlVersion is missing or empty");
                }
                if (ChannelXmlNode.Attributes["xmlVersion"].Value.Equals("1.0"))
                    return IngestXMLType.Channel_1_0;
                else
                {
                    throw new Exception("ChannelVersion " + ChannelXmlNode.Attributes["xmlVersion"].Value + " is not valid");
                }
            }

            return result;
        }

        public static String SerializeObject<T>(T obj)
        {

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.OmitXmlDeclaration = true;
            XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
            xmlSerializerNamespaces.Add("", "");
            XmlSerializer ser = new XmlSerializer(typeof(T));
            StringWriter stringWriter = new StringWriter();
            XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);
            ser.Serialize(xmlWriter, obj, xmlSerializerNamespaces);
            String xmlData = stringWriter.ToString();
            return xmlData;
        }

        public static String SerializeObject(Object obj)
        {

            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.OmitXmlDeclaration = true;
            XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
            xmlSerializerNamespaces.Add("", "");
            XmlSerializer ser = new XmlSerializer(obj.GetType());
            StringWriter stringWriter = new StringWriter();
            XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);
            ser.Serialize(xmlWriter, obj, xmlSerializerNamespaces);
            String xmlData = stringWriter.ToString();
            return xmlData;
        }
        /*
        public static T DeserializeXML<T>(string xmlData)
            where T : new()
        {
            if (string.IsNullOrEmpty(xmlData))
                return default(T);

            TextReader tr = new StringReader(xmlData);
            T DocItms = new T();
            XmlSerializer xms = new XmlSerializer(DocItms.GetType());
            DocItms = (T)xms.Deserialize(tr);

            return DocItms == null ? default(T) : DocItms;
        }

        public static string SeralizeObjectToXML<T>(T xmlObject)
        {
            StringBuilder sbTR = new StringBuilder();
            XmlSerializer xmsTR = new XmlSerializer(xmlObject.GetType());
            XmlWriterSettings xwsTR = new XmlWriterSettings();

            XmlWriter xmwTR = XmlWriter.Create(sbTR, xwsTR);
            xmsTR.Serialize(xmwTR, xmlObject);

            return sbTR.ToString();
        }

        public static T CloneObject<T>(T objClone)
            where T : new()
        {
            string GetString = CommonUtil.SeralizeObjectToXML<T>(objClone);

            return CommonUtil.DeserializeXML<T>(GetString);
        }
        */

        public static T DeSerializeObject<T>(String objStr) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlReader reader = XmlReader.Create(new StringReader(objStr));
            T obj = serializer.Deserialize(reader) as T;
            reader.Close();
            return obj;
        }

        public static T CloneObject<T>(T obj) where T : class
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(memoryStream, obj);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return serializer.Deserialize(memoryStream) as T;
        }

        public static void AddDefaultMetaDataValue(ContentData content, String propertyName, IngestConfig ingestConfig)
        {
            var categoryProperty = content.Properties.FirstOrDefault(p => p.Type.Equals(propertyName));
            if (categoryProperty == null && ingestConfig.MetaDataDefaultValues.ContainsKey(propertyName))
                content.Properties.Add(new Property(propertyName, ingestConfig.MetaDataDefaultValues[propertyName]));
        }

        public static void AddPublishInfoToContent(ContentData content, PublishState useState)
        {
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            List<ContentAgreement> agreements = mppWrapper.GetAllServicesForContent(content);
            // add publishinfos
            List<String> regions = new List<String>();
            foreach (ContentAgreement agreement in agreements)
            {
                foreach (MultipleContentService service in agreement.IncludedServices)
                {
                    List<ServiceViewMatchRule> matchrules = mppWrapper.GetServiceViewMatchRules(service);
                    foreach (ServiceViewMatchRule matchrule in matchrules)
                    {
                        if (!regions.Contains(matchrule.Region))
                            regions.Add(matchrule.Region);
                    }
                }
            }
            foreach (String region in regions)
            {
                PublishInfo publishInfo = new PublishInfo();
                publishInfo.DeliveryMethod = DeliveryMethod.Stream;
                publishInfo.PublishState = useState;
                publishInfo.Region = region;
                content.PublishInfos.Add(publishInfo);
            }
        }

        public static void AddEpgHasRecordingsProperty(ContentData content, bool haveRecordings)
        {
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            List<ContentAgreement> agreements = mppWrapper.GetAllServicesForContent(content);
            // add publishinfos

            foreach (ContentAgreement agreement in agreements)
            {
                foreach (MultipleContentService service in agreement.IncludedServices)
                {
                    var property = ConaxIntegrationHelper.SetServiceHasRecordingProperty(service.ObjectID.Value, content, haveRecordings);
                    //content.Properties.Add(property);
                }
            }

        }

        public static int GetEpgHistoryTimeInHours()
        {
            var managerConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            return managerConfig.EPGHistoryInHours;
        }

        private static String _myID;
        public static String GetMyID()
        {
            if (String.IsNullOrWhiteSpace(_myID))
            {
                var workflowConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager");
                if (workflowConfig.ConfigParams.ContainsKey("MyID") &&
                    !String.IsNullOrWhiteSpace(workflowConfig.GetConfigParam("MyID")))
                {
                    _myID = workflowConfig.GetConfigParam("MyID");
                }
                else
                {
                    var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                    var ip = (
                                from addr in hostEntry.AddressList
                                where addr.AddressFamily == AddressFamily.InterNetwork
                                select addr.ToString()
                        ).FirstOrDefault();
                    _myID = ip;
                }
            }
            return _myID;
        }

        public static StreamType GetStreamType(String url)
        {
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return StreamType.IP;
            if (url.StartsWith("dvb", StringComparison.OrdinalIgnoreCase))
                return StreamType.DVB;

            return StreamType.Unknown;
        }

        public static Asset GetAssetFromContentByISOAndDevice(ContentData content, String serviceViewLanugageISO, DeviceType deviceType, AssetType assetType)
        {
            var asset = content.Assets.FirstOrDefault(a => a.LanguageISO.Equals(serviceViewLanugageISO) &&
                                                           a.Properties.Count(p => p.Type.Equals(VODnLiveContentProperties.DeviceType) &&
                                                                                   p.Value.Equals(deviceType.ToString())) > 0 &&
                                                           a.Properties.Count(p => p.Type.Equals(VODnLiveContentProperties.AssetType) &&
                                                                                   p.Value.Equals(assetType.ToString())) > 0);

            return asset;
        }


        public static MovieRatingFormats GetSystemMovieRatingFormat()
        {
            var conaxConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

            var movieRatingFormat = MovieRatingFormats.MPAA;

            if (conaxConfig.ConfigParams.ContainsKey("MovieRatingFormat"))
            {
                if (!Enum.TryParse<MovieRatingFormats>(conaxConfig.GetConfigParam("MovieRatingFormat"),
                    out movieRatingFormat))
                {
                    string errorMessage = "Error parsing vodformat " + conaxConfig.GetConfigParam("MovieRatingFormat") +
                                          " from config. See enum MovieRatingFormat for available formats.";
                    log.Error(errorMessage);

                    throw new Exception(errorMessage);
                }
            }
            else
            {
                log.Debug("No MovieRatingFormat found in config, using default MPAA");
            }
            return movieRatingFormat;
        }

        public static TVRatingFormats GetSystemTVRatingFormat()
        {
            var conaxConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            var TVRatingFormat = TVRatingFormats.VCHIP;

            if (conaxConfig.ConfigParams.ContainsKey("TVRatingFormat"))
            {
                if (!Enum.TryParse<TVRatingFormats>(conaxConfig.GetConfigParam("TVRatingFormat"),
                    out TVRatingFormat))
                {
                    string errorMessage = "Error parsing TVRatingFormat " + conaxConfig.GetConfigParam("TVRatingFormat") +
                                         " from config. See enum TVRatingFormats for available formats.";
                    log.Error(errorMessage);

                    throw new Exception(errorMessage);
                }
            }
            else
            {
                log.Debug("No TVRatingFormat found in config, using default VChip");
            }
            return TVRatingFormat;
        }

        public static String GetRatingForContent(ContentData content, String ratingType)
        {
            var ratingProperty = content.Properties.SingleOrDefault(p => p.Type == ratingType);

            if (ratingProperty != null)
                return ratingProperty.Value;

            return null;

        }

        public static void SetRatingForContent(ContentData content, String rating, String ratingType)
        {
            var ratingProperty = content.Properties.SingleOrDefault(p => p.Type == ratingType);
            if (ratingProperty == null)
            {
                ratingProperty = new Property() { Type = ratingType, Value = rating };
                content.Properties.Add((ratingProperty));
            }
            else
            {
                ratingProperty.Value = rating;
            }
        }

        public static string UnZip(string compressedString)
        {

            try
            {
                if (String.IsNullOrEmpty(compressedString))
                    return compressedString;
                byte[] gzBuffer = Convert.FromBase64String(compressedString);
                using (MemoryStream ms = new MemoryStream())
                {
                    int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                    ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

                    byte[] buffer = new byte[msgLength];

                    ms.Position = 0;
                    using (System.IO.Compression.GZipStream zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
                    {
                        zip.Read(buffer, 0, buffer.Length);
                    }

                    return System.Text.Encoding.Unicode.GetString(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                log.Error("Failed unzipping compressedString= " + compressedString, ex);
                throw;
            }

        }

        public static string Zip(string text)
        {
            byte[] buffer = System.Text.Encoding.Unicode.GetBytes(text);
            MemoryStream ms = new MemoryStream();
            using (System.IO.Compression.GZipStream zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }

            ms.Position = 0;

            byte[] compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            ms.Close();
            return Convert.ToBase64String(gzBuffer);
        }
    }
}
