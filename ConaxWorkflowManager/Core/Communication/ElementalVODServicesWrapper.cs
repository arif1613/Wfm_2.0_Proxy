using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using log4net;
using System.Reflection;
using System.Net;
using System.IO;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.Collections;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Policy
{
    public class ElementalVODServicesWrapper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ElementalRestApiCaller restAPI;

        private SystemConfig systemConfig;

        private bool useNewVersion = true;

        public ElementalVODServicesWrapper()
        {
            systemConfig = Config.GetConfig().SystemConfigs.Where<SystemConfig>(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
            String key = systemConfig.GetConfigParam("UserHash");
            String baseURL = systemConfig.GetConfigParam("Endpoint");
            if (!baseURL.EndsWith("/"))
            {
                baseURL += "/";
            }

            String useAuthentication = systemConfig.GetConfigParam("UseAuthentication");
            bool authentication = false;
            bool.TryParse(useAuthentication, out authentication);


            if (systemConfig.ConfigParams.ContainsKey("UseNewVersion"))
            {
                String useNewVersionString = systemConfig.GetConfigParam("UseNewVersion");
                Boolean.TryParse(useNewVersionString, out useNewVersion);
            }

            restAPI = new ElementalRestApiCaller(baseURL, authentication, key);

        }

        public String LaunchJob(String profileName, List<JobParameter> parameters, bool isTrailer, ContentData content)
        {
            try
            {
                log.Debug("In LaunchJob with profileID " + profileName);
                XmlDocument doc = GetJobXml(profileName, parameters, isTrailer, content);
                CallStatus reply = restAPI.MakeAddCall("jobs", doc);
                if (reply.Success)
                {
                    log.Debug("add call successfull");
                    XmlDocument jobXML = new XmlDocument();
                    jobXML.LoadXml(reply.Data);
                    File.Create(@"C:\jobxml.xml");
                    FileInfo fileInfo = new FileInfo(@"C:\jobxml.xml");
                    XmlDocument SavedJobXML = new XmlDocument();
                    SavedJobXML.Load(@"C:\jobxml.xml");
                    SavedJobXML.LoadXml(reply.Data);
                    SavedJobXML.Save(reply.Data);


                    String jobID = GetJobID(jobXML);
                    log.Debug("jobID= " + jobID);
                    return jobID;
                }
                else
                {
                    Console.WriteLine("Something went wrong when trying to  create job");
                    return null;
                    //throw new Exception("Something went wrong creating job!", reply.Exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public string GetJobID(XmlDocument jobXML)
        {
            try
            {
                String jobIDString = jobXML.SelectSingleNode("job").Attributes["href"].InnerText;
                jobIDString = jobIDString.Substring(jobIDString.LastIndexOf("/") + 1);
                return jobIDString;
            }
            catch (Exception ex)
            {
                log.Error("Error fetching jobID", ex);
                throw;
            }
        }

        public ElementalJobInfo GetJobStatus(String jobID)
        {
            ElementalJobInfo jobStatus = new ElementalJobInfo();
            CallStatus status = restAPI.MakeGetCall("jobs", jobID, "/status");
            if (status.Success)
            {
                jobStatus = TranslateReply(status.Data);
            }
            else
            {
                throw new Exception("Something went wrong calling rest api!", status.Exception);
            }
            return jobStatus;
        }

        private ElementalJobInfo TranslateReply(string reply)
        {
            var jobInfo = new ElementalJobInfo();
            var jobStatus = ElementalJobStatus.error;
            var doc = new XmlDocument();
            try
            {
                log.Debug("In TranslateReply reply= " + reply);
                doc.LoadXml(reply);
                XmlNode statusNode = doc.SelectSingleNode("job/status");
                jobStatus = (ElementalJobStatus)Enum.Parse(typeof(ElementalJobStatus), statusNode.InnerText);
                jobInfo.status = jobStatus;
                XmlNode progressNode = doc.SelectSingleNode("job/pct_complete");
                jobInfo.Progress = progressNode.InnerText;
                if (jobStatus == ElementalJobStatus.error)
                {
                    log.Debug("The job failed");
                    List<JobError> errors = new List<JobError>();
                    XmlNodeList errorList = doc.SelectNodes("job/errors");
                    foreach (XmlNode error in errorList)
                    {
                        JobError e = new JobError();
                        e.Code = error.SelectSingleNode("code").InnerText;
                        e.ErrorDescription = error.SelectSingleNode("message").InnerText;
                        errors.Add(e);
                        log.Debug("Added error = " + e.ToString());
                    }
                    jobInfo.Errors = errors;
                }
                return jobInfo;
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong parsing reply from encoder", ex);
                throw;
            }
            return jobInfo;
        }

        public List<JobParameter> CreateDefaultParameters(String inputFile, String outputFilePath, String conaxContentID)
        {
            List<JobParameter> ret = new List<JobParameter>();
            ret.Add(new JobParameter() { Name = "InputFile", Value = inputFile });
            ret.Add(new JobParameter() { Name = "OutputFile", Value = outputFilePath });
            ret.Add(new JobParameter() { Name = "ConaxContentID", Value = conaxContentID });
            return ret;
        }

        public XmlDocument GetJobXml(String profileName, List<JobParameter> parameters, bool isTrailer, ContentData content)
        {
            XmlDocument ret = GetJobXMLFromProfileName(profileName);

            var inputParameter = parameters.SingleOrDefault<JobParameter>(p => p.Name.Equals("InputFile"));
            if (inputParameter == null)
            {
                throw new Exception("No inputParameter was specified!");
            }
            var outputParameter = parameters.SingleOrDefault<JobParameter>(p => p.Name.Equals("OutputFile"));
            if (inputParameter == null)
            {
                throw new Exception("No outputParameter was specified!");
            }

            SetInputAndOutput(ret, inputParameter.Value, outputParameter.Value);
            if (!isTrailer)
            {
                log.Debug("Not trailer, adding conax content_id");
                var conaxContentIDParameter = parameters.SingleOrDefault<JobParameter>(p => p.Name.Equals("ConaxContentID"));
                if (conaxContentIDParameter == null)
                {
                    throw new Exception("No Conax contentID was was found! this is needed for encryption of non trailers");
                }

                String conaxContentID = conaxContentIDParameter.Value;
                SetContentIDOnFiles(ret, conaxContentID);

            }
            Asset asset = content.Assets.Where(r => r.IsTrailer == isTrailer).FirstOrDefault();

            try
            {
                ModifyAudioTracks(ret, asset);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                log.Debug("translated xml= " + ret.InnerXml);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
            return ret;
        }

        private void ModifyAudioTracks(XmlDocument ret, Asset asset)
        {
            List<String> audioPids = ConaxIntegrationHelper.GetAudioTrackLanguageWithPids(asset);
            XmlNodeList pidNodes = ret.SelectNodes("job/input/audio_selector");
            if (pidNodes.Count != 0)
            {
                int nodePos = 0;
                Hashtable mappedPids = new Hashtable();
                foreach (String pid in audioPids)
                {
                    XmlNode node = pidNodes[nodePos];
                    XmlNode nameNode = node.SelectSingleNode("name");
                    XmlNode pidNode = node.SelectSingleNode("pid");
                    String name = "";
                    if (pidNode != null)
                    {
                        log.Debug("Setting pid to " + pid);
                        String[] lan = pid.Split(':');
                        pidNode.InnerText = lan[1];
                        name = nameNode.InnerText.Replace("input_1_", "");
                        name = name.Replace("_", "");
                        mappedPids.Add(name.ToLower(), pid);
                        nodePos++;
                    }
                    else
                    {
                        log.Debug("Found a node missing pid");
                    }

                }
                XmlNodeList outputNodes = ret.SelectNodes("job/medium/audio_description");

                foreach (XmlNode mediumNode in outputNodes)
                {
                    ParseAndSetLanguageOnMedium(mediumNode, mappedPids);
                }
            }

        }

        private void ParseAndSetLanguageOnMedium(XmlNode mediumNode, Hashtable mappedPids)
        {
            XmlNode audioNode = mediumNode.SelectSingleNode("audio_source_name");
            log.Debug("setting languages on medium");
            if (audioNode != null)
            {
                String key = audioNode.InnerText.ToLower().Replace(" ", "");
                String pid = mappedPids[key] as String;
                String[] lan = pid.Split(':');
                String languageISO = lan[0];
                XmlNode languageNode = mediumNode.SelectSingleNode("language_code");
                if (languageNode != null)
                    languageNode.InnerText = languageISO;

                languageNode = mediumNode.SelectSingleNode("stream_name");
                if (languageNode != null)
                    languageNode.InnerText = languageISO;
            }
            else
            {

            }
        }

        private void SetInputAndOutput(XmlDocument profileXML, String inputFile, String outputPath)
        {
            XmlNode profile = profileXML.SelectSingleNode("job");
            XmlNode inputNode = profile.SelectSingleNode("input");
            if (inputNode == null)
            {
                log.Debug("creating inputnode");
                XmlElement inputElement = profileXML.CreateElement("input");
                profile.AppendChild(inputElement);
                XmlElement fileinput = profileXML.CreateElement("file_input");
                inputElement.AppendChild(fileinput);
                XmlElement uri = profileXML.CreateElement("uri");

                fileinput.AppendChild(uri);
            }
            XmlNode uriNode = profile.SelectSingleNode("input/file_input/uri");
            uriNode.InnerText = inputFile;

            if (useNewVersion)
            {
                foreach (XmlNode node in profile.SelectNodes("output_group/ms_smooth_group_settings/destination/uri"))
                {
                    node.InnerText = outputPath;
                }
                foreach (XmlNode node in profile.SelectNodes("output_group/apple_live_group_settings/destination/uri"))
                {
                    node.InnerText = outputPath;
                }
            }
            else
            {
                XmlNode output = profile.SelectSingleNode("destination");
                if (output == null)
                {
                    log.Debug("creating outputnode");
                    XmlElement destinationElement = profileXML.CreateElement("destination");
                    profile.AppendChild(destinationElement);
                    XmlElement uri = profileXML.CreateElement("uri");
                    destinationElement.AppendChild(uri);
                }
                XmlNode destinationUriNode = profile.SelectSingleNode("destination/uri");
                destinationUriNode.InnerText = outputPath;
            }
        }

        private void SetContentIDOnFiles(XmlDocument ret, string conaxContentID)
        {
            if (useNewVersion)
            {
                foreach (XmlNode file in ret.SelectNodes("job/output_group/ms_smooth_group_settings/conax_settings/content_id"))
                {

                    file.InnerText = conaxContentID;

                }
                foreach (XmlNode file in ret.SelectNodes("job/output_group/apple_live_group_settings/conax_settings/content_id"))
                {

                    file.InnerText = conaxContentID;

                }
            }
            else
            {
                foreach (XmlNode file in ret.SelectNodes("job/medium"))
                {
                    String contentIDNodeName = "output_group/ms_smooth_group_settings/conax_settings/content_id";
                    XmlNode groupNode = file.SelectSingleNode("output_group/name");
                    if (groupNode != null)
                    {
                        String fileType = groupNode.InnerText;
                        if (fileType.Equals("apple_live_stream"))
                        {
                            contentIDNodeName = "output_group/apple_live_group_settings/conax_settings/content_id";
                        }
                        XmlNode contentIDNode = file.SelectSingleNode(contentIDNodeName);
                        if (contentIDNode != null)
                        {
                            contentIDNode.InnerText = conaxContentID;
                        }
                    }
                }
            }
        }

        public XmlDocument GetJobXMLFromProfileName(String profileName)
        {
            XmlDocument doc = new XmlDocument();
            log.Debug("Fetching xml from profileName, name= " + profileName);
            try
            {
                String objectToHandle = "job_profiles";
                String nodeName = "job_profile";
                if (!useNewVersion)
                {
                    objectToHandle = "profiles";
                    nodeName = "profile";
                }
                CallStatus status = restAPI.MakeGetCall(objectToHandle, profileName, "?clean=true");
                if (status.Success)
                {
                    doc.LoadXml(status.Data);
                    XmlNode node = doc.SelectSingleNode(nodeName);
                    if (node != null)
                    {
                        log.Debug("Renaming node from profile to job");
                        RenameNode(node, "job");
                    }



                    RemoveNodesNotNeeded(doc);
                    return doc;
                }
                else
                {
                    Console.WriteLine("encoder error1:" + status.Error);
                    return null;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("encoder error2:" + ex.Message);
                return null;

            }
        }

        public static XmlNode RenameNode(XmlNode node, string qualifiedName)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement oldElement = (XmlElement)node;
                XmlElement newElement =
                node.OwnerDocument.CreateElement(qualifiedName);

                while (oldElement.HasAttributes)
                {
                    newElement.SetAttributeNode(oldElement.RemoveAttributeNode(oldElement.Attributes[0]));
                }

                while (oldElement.HasChildNodes)
                {
                    newElement.AppendChild(oldElement.FirstChild);
                }

                if (oldElement.ParentNode != null)
                {
                    oldElement.ParentNode.ReplaceChild(newElement, oldElement);
                }

                 

                return newElement;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Removes nodes that can't be in xml to create job
        /// </summary>
        /// <param name="doc">Document to remove nodes from</param>
        private void RemoveNodesNotNeeded(XmlDocument doc)
        {
            XmlNode jobNode = doc.SelectSingleNode("job");
            XmlNode nodeToRemove = doc.SelectSingleNode("job/name");
            if (nodeToRemove != null)
            {
                log.Debug("Removing name node");
                jobNode.RemoveChild(nodeToRemove);
            }
            nodeToRemove = doc.SelectSingleNode("job/permalink");
            if (nodeToRemove != null)
            {
                log.Debug("Removing permalink node");
                jobNode.RemoveChild(nodeToRemove);
            }
            nodeToRemove = doc.SelectSingleNode("job/description");
            if (nodeToRemove != null)
            {
                log.Debug("Removing description node");
                jobNode.RemoveChild(nodeToRemove);
            }
        }

    }

    public class ElementalRestApiCaller
    {
        private String _baseURL;

        private bool _useAuthentication;

        private String _key;


        public ElementalRestApiCaller(String baseURL, bool useAuthentication, String key)
        {
            _baseURL = baseURL;
            _useAuthentication = useAuthentication;
            _key = key;
        }

        /// <summary>
        /// This method fetches a object from the rest apy
        /// </summary>
        /// <param name="objectToHandle">The object type to handle, ie products, services</param>
        /// <param name="id">The id of the object to fetch data from, if "" is sent all will be fetched.</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeGetCall(String objectToHandle, String id, String extraParameter)
        {
            try
            {
                String parameter = "";
                if (!String.IsNullOrEmpty(id))
                {
                    parameter = "/" + id;
                }
                string address = _baseURL + objectToHandle + parameter + extraParameter;
                HttpWebRequest request = null;
                try
                {
                    request = (HttpWebRequest)WebRequest.Create(address);
                    Console.WriteLine("web request from MakeGetCall is successful");

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.InnerException);
                }

                if (_useAuthentication)
                {
                    String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                    request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                }
                request.Method = "GET";

                request.Accept = "application/xml";

                WebResponse r = request.GetResponse();
                Stream response = r.GetResponseStream();
                StreamReader sr = new StreamReader(response);
                String reply = sr.ReadToEnd();
                CallStatus status = new CallStatus();

                status.Success = true;
                status.Data = reply;
                Console.WriteLine("Asset file will be encoded using elemental encoder......");
                return status;
            }
            catch (WebException ex)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                if (ex.Response != null)
                {
                    string sResponse = "";
                    Stream strm = ex.Response.GetResponseStream();
                    byte[] bytes = new byte[strm.Length];
                    strm.Read(bytes, 0, (int)strm.Length);
                    sResponse = System.Text.Encoding.ASCII.GetString(bytes);
                    status = XmlReplyParser.ParseReply(sResponse);
                    status.Error = sResponse;
                    Console.WriteLine("Error response= " + sResponse);

                }
                return status;
            }
            catch (Exception e)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Exception = e;
                return status;
            }
        }

        /// <summary>
        /// Creates a object of the specified type, for example a job
        /// </summary>
        /// <param name="objectToHandle">The object type to create, ie jobs</param>
        /// <param name="data">The data to use when creating the object</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeAddCall(String objectToHandle, XmlDocument data)
        {
            try
            {
                string address = _baseURL + objectToHandle;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                if (_useAuthentication)
                {
                    String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                    request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                }
                request.Method = "POST";

                request.ProtocolVersion = HttpVersion.Version11;
                request.Timeout = 10000;


                byte[] d = Encoding.UTF8.GetBytes(data.InnerXml);
                //byte[] d = System.Text.Encoding.ASCII.GetBytes(data.InnerXml);



                request.MediaType = "application/xml";
                request.Accept = "application/xml";
                request.ContentType = "application/xml;charset=UTF-8";
                request.ContentLength = d.Length;

                Stream requestStream = request.GetRequestStream();
                // Send the request
                requestStream.Write(d, 0, d.Length);
                requestStream.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                String reply = new StreamReader(response.GetResponseStream()).ReadToEnd();
                CallStatus status = new CallStatus();
                status.Success = true;
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                if (ex.Response != null)
                {
                    string sResponse = "";
                    Stream strm = ex.Response.GetResponseStream();
                    byte[] bytes = new byte[strm.Length];
                    strm.Read(bytes, 0, (int)strm.Length);
                    sResponse = System.Text.Encoding.ASCII.GetString(bytes);
                    status.Error = sResponse;
                }
                return status;
            }
            catch (Exception e)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Exception = e;
                return status;
            }
        }
    }

    public class ElementalJobInfo
    {

        public ElementalJobStatus status;

        public String Progress;

        public List<JobError> Errors;
    }

    public class JobError
    {
        public String Code;

        public String ErrorDescription;

        public override string ToString()
        {
            return "Code= " + Code + ":ErrorDescription= " + ErrorDescription;
        }
    }
}
