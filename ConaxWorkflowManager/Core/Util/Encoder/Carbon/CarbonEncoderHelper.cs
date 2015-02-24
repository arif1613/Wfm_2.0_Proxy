using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Encoder;
using log4net;
using System.Reflection;
using System.Data.OleDb;
using System.Data;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.IO;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using System.Text.RegularExpressions;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Carbon
{
    public class CarbonEncoderHelper : EncoderHelper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static String GetProfileID(String encoderName, ContentData content, Boolean trailer)
        {
            log.Debug("In getProfileID for content with name " + content.Name + " trailer= " + trailer.ToString() + " encodertype= " + encoderName);
            List<ProfileValues> matches = GetProfilesFromBasicMatches(encoderName, content, trailer);
            ProfileValues profileMatch = DoEncoderSpecificMatch(matches, content, trailer);
            return profileMatch.ID;
        }

        protected static ProfileValues DoEncoderSpecificMatch(List<ProfileValues> profiles, ContentData content, Boolean trailer)
        {
            log.Debug("Found matches before languagecheck= " + profiles.Count().ToString());
            Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer == trailer);
            List<String> languages = ConaxIntegrationHelper.GetAudioTrackLanguages(asset);

            ProfileValues profileMatch = null;
            foreach (ProfileValues profile in profiles)
            {
                log.Debug("Checking languages for profile " + profile.Name);
                if (profile.HaveSameLanguagesCount(languages))
                {
                    log.Debug("Found a profile with same audio count");
                    profileMatch = profile;
                    break;
                }
            }
            if (profileMatch != null)
                return profileMatch;
            else
                throw new Exception("No profile match with correct amount of audio tracks was found");
        }

        public static List<IncludedWorkFlow> GetEncoderJobWorkFlowOrder(String encoderJobFlowGuid)
        {
            List<EncoderWorkFlow> availableWorkFlows = LoadAndParseWorkFlowList();

            EncoderWorkFlow workFlow = availableWorkFlows.SingleOrDefault<EncoderWorkFlow>(w => w.WorkFlowGuid.Equals(encoderJobFlowGuid));

            if (workFlow == null)
                throw new Exception("No encoder workFlow found for workflowGuid " + encoderJobFlowGuid);

            return workFlow.WorkFlow;
        }

        protected static List<EncoderWorkFlow> LoadAndParseWorkFlowList()
        {
            var managerConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String fileName = "";
            if (managerConfig.ConfigParams.ContainsKey("EncoderWorkFlowFile"))
            {
                fileName = managerConfig.GetConfigParam("EncoderWorkFlowFile");
            }
            else
            {
                throw new Exception("No EncoderWorkFlowFile file was found in configuration");
            }
            log.Debug("<---------------------------------------- workflow handeling ----------------------------------------------------->");
            List<EncoderWorkFlow> ret = new List<EncoderWorkFlow>();
           
            System.Data.OleDb.OleDbConnection connection;

            connection = new System.Data.OleDb.OleDbConnection("provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + fileName + "';Extended Properties=Excel 12.0 Xml;");
            log.Debug("connection created");
            try
            {
                //After connecting to the Excel sheet here we are selecting the data 
                //using select statement from the Excel sheet
                OleDbCommand ocmd = new OleDbCommand("select distinct WORKFLOW_ID from [Sheet1$]", connection);
                connection.Open();  //Here [Sheet1$] is the name of the sheet 
                log.Debug("Connection opened");
                //in the Excel file where the data is present
                OleDbDataReader reader = ocmd.ExecuteReader();
                List<String> workFlowGuids = GetAllWorkFlowGuids(reader);
                foreach (String workFlowGuid in workFlowGuids)
                {
                    log.Debug("Adding workFlowGuid " + workFlowGuid);
                    ret.Add(BuildWorkFlow(workFlowGuid, connection));
                }

            }
            catch (Exception ex)
            {
                log.Error("Error loading profiles from excel file", ex);
                throw;
            }
            finally
            {
                connection.Close();
            }
            return ret;
        
        }

        private static List<string> GetAllWorkFlowGuids(OleDbDataReader reader)
        {
            List<String> workflowGuids = new List<string>();
            while (reader.Read())
            {
                object o = reader["WORKFLOW_ID"];
                if (o != null && !String.IsNullOrEmpty(o.ToString()))
                    workflowGuids.Add(o.ToString());
            }
            return workflowGuids;
        }

        private static EncoderWorkFlow BuildWorkFlow(string workFlowGuid, OleDbConnection connection)
        {
            EncoderWorkFlow ret = new EncoderWorkFlow();
            try
            {
                log.Debug("Building workflow for id " + workFlowGuid);
                OleDbCommand ocmd = new OleDbCommand("select * from [Sheet1$] where WORKFLOW_ID='" + workFlowGuid + "'", connection);
                OleDbDataReader reader = ocmd.ExecuteReader();
                reader.Read();
                log.Debug("Done executing query");

                String value = "";
                ret.WorkFlowGuid = workFlowGuid;

                while (true)
                {

                    value = GetValue(reader, "INCLUDED_WORKFLOW_GUID");
                    if (!String.IsNullOrEmpty(value))
                    {
                        IncludedWorkFlow workFlow = new IncludedWorkFlow();
                        workFlow.WorkFlowGuid = value;

                        value = GetValue(reader, "INCLUDED_WORKFLOW_NAME");
                        workFlow.Name = value;

                        value = GetValue(reader, "USE_TEMPFOLDER_FOR_OUTPUT");
                        bool useTempOutPut;
                        bool.TryParse(value, out useTempOutPut);
                        workFlow.UseTempFolderForOutput = useTempOutPut;

                        value = GetValue(reader, "USE_PARAMETERS_FROM_PREVIOUS_JOB");
                        bool useParametersFromPreviousJob;
                        bool.TryParse(value, out useParametersFromPreviousJob);
                        workFlow.UseParametersFromPreviousJob = useParametersFromPreviousJob;
                        value = GetValue(reader, "JOB_ORDER");
                        workFlow.JobOrder = int.Parse(value);
                        ret.WorkFlow.Add(workFlow);
                        if (!reader.Read())
                            break;
                    }
                }
                ret.WorkFlow.OrderBy(p => p.JobOrder);
            }
            catch (DataException ex)
            {
                log.Error("Error building profile", ex);
            }
           
            return ret;
        }



        internal static string FetchJobConfig(ContentData content, bool trailerJob, IncludedWorkFlow includedWorkFlow, String inputFile, String serverConfigName, String mezzanineName)
        {
            XmlDocument jobDoc = new XmlDocument();
            String conaxContentID = ConaxIntegrationHelper.GetConaxContegoContentID(content);
            log.Debug("Fetching jobConfig for workflow " + includedWorkFlow.WorkFlowGuid + ", UseParametersFromPreviousJob = " + includedWorkFlow.UseParametersFromPreviousJob.ToString());
            
            if (includedWorkFlow.UseParametersFromPreviousJob)
            {
                jobDoc.LoadXml(FetchJobConfigFromServer(includedWorkFlow, serverConfigName));
                if (!trailerJob)
                {
                    SetResourceID(jobDoc, conaxContentID);
                }
                inputFile = Path.Combine(includedWorkFlow.TempFolder, mezzanineName);
                log.Debug("Using inputfile from tempfolder, path = " + inputFile);
            }
            else
            {
                if (!trailerJob)
                {
                    jobDoc.LoadXml(templeEx);
                }
                else
                {
                    jobDoc.LoadXml(templeExWithEncryptionParams);
                    SetResourceID(jobDoc, conaxContentID);
                }
                //if (!includedWorkFlow.UseParametersFromPreviousJob)
                //{
                    XmlNodeList inputNodes = jobDoc.SelectNodes("TemplateExXML/WorkflowParams/Source/FilePathList/UNCPath");
                    foreach (XmlNode inputNode in inputNodes)
                        inputNode.InnerText = inputFile;
               // }
            }
            return jobDoc.OuterXml;
        }

        private String SetOutputFolderForEncoding(String templateConfigString, String outputFolder)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(templateConfigString);
            XmlNodeList outputFolderNodes = doc.SelectNodes("WorkflowTasks/TransformTaskSet/TranscodeTargetSet/Target/Path");
            foreach (XmlNode pathNode in outputFolderNodes)
            {
                pathNode.InnerText = outputFolder;
            }
            XmlNodeList packageNodes = doc.SelectNodes("WorkflowTasks/TransformTaskSet/PackageTargetSet/PackageTarget/Path");
            foreach (XmlNode node in packageNodes)
                node.InnerText = outputFolder;

            XmlNode reportNode = doc.SelectSingleNode("WorkflowTasks/JobEndTaskSet/CompletionTaskSet/ReportTask/ReportOutputPath");
            if (reportNode != null)
                reportNode.InnerText = outputFolder;

            return doc.OuterXml;
        }

        private static void SetResourceID(XmlDocument jobDoc, String conaxContentID)
        {
            XmlNode encryptionDataNode = jobDoc.SelectSingleNode("TemplateExXML/WorkflowParams/TaskParamsList/TaskParams[@Type='Encryption']");
            if (encryptionDataNode == null)
            {
                AddEncryptionNode(jobDoc);
            }
            encryptionDataNode = jobDoc.SelectSingleNode("TemplateExXML/WorkflowParams/TaskParamsList/TaskParams[@Type='Encryption']");
            encryptionDataNode = encryptionDataNode.SelectSingleNode("EncryptionParams/ResourceID");
            encryptionDataNode.InnerText = conaxContentID;
        }

        private static void AddEncryptionNode(XmlDocument jobDoc)
        {
            XmlNode node = jobDoc.SelectSingleNode("TemplateExXML/WorkflowParams");
            XmlDocumentFragment xfrag = jobDoc.CreateDocumentFragment();
            xfrag.InnerXml = "<TaskParamsList><TaskParams Type=\"Encryption\"><EncryptionParams><ResourceID></ResourceID></EncryptionParams></TaskParams></TaskParamsList>";
            node.AppendChild(xfrag);
        }

        private static String templeExWithEncryptionParams = "<TemplateExXML><WorkflowParams version=\"1.0\"><Source Type=\"Any\"><FilePathList><UNCPath></UNCPath></FilePathList></Source><TaskParamsList><TaskParams Type=\"Encryption\"><EncryptionParams><ResourceID></ResourceID></EncryptionParams></TaskParams></TaskParamsList></WorkflowParams></TemplateExXML>";

        private static String templeEx = "<TemplateExXML><WorkflowParams version=\"1.0\"><Source Type=\"Any\"><FilePathList><UNCPath></UNCPath></FilePathList></Source></WorkflowParams></TemplateExXML>";

        internal static string GetTempFolder(string encoderRoot, ContentData content)
        {
            return Path.Combine(encoderRoot, content.ObjectID.ToString() + "_temp");
        }

        internal static string FetchJobConfigFromServer(IncludedWorkFlow includedJob, string serverConfigName)
        {
            String configFolder = includedJob.TempFolder;
            String fullFileName = Path.Combine(configFolder, serverConfigName + ".xml");
            XmlDocument doc = new XmlDocument();
            doc.Load(fullFileName);
            return doc.OuterXml;
        }

        public static Int64 GetEpochTime(DateTime dt)
        {
            // round up the micro seconds.
            Int64 epoch = ((dt.Ticks - 621355968000000000) / 10000000) * 10000000;

            return epoch;
        }

        public static String GetStreamName(String url)
        {
            String streamName = String.Empty;
            String streamNamePattern = "Channel(.*).isml";

            Match match = Regex.Match(url,
                                      streamNamePattern,
                                      RegexOptions.IgnoreCase);
            if (match.Success)
                streamName = match.Value.Substring(8, match.Value.Length - streamNamePattern.Length + 2);

            return streamName;
        }
    }
}
