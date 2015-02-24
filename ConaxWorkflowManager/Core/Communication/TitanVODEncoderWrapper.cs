using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.Net;
using System.IO;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    public class TitanVODEncoderWrapper
    {
          private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private TitanRestApiCaller restAPI;

        String jobUrl = "";

        String segmentUrl = "";

        AssetFormatType outFormat;

        private SystemConfig systemConfig;

        public TitanVODEncoderWrapper()
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

            restAPI = new TitanRestApiCaller(baseURL, authentication, key);
            
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
                    String jobID = GetJobID(jobXML);
                    log.Debug("jobID= " + jobID);
                    return jobID;
                }
                else
                {
                    log.Error("Something went wrong when trying to  create job");
                    throw new Exception("Something went wrong creating job!", reply.Exception);
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong when trying to  create job", ex);
                throw;
            }
        }

        private XmlDocument GetJobXml(string profileName, List<JobParameter> parameters, bool isTrailer, ContentData content)
        {
            throw new NotImplementedException();
        }

        private string GetJobID(XmlDocument jobXML)
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

        public TitanJobInfo GetJobStatus(String jobUrl, String jobName)
        {
            TitanJobInfo jobStatus = new TitanJobInfo();
            CallStatus status = null;
            if (!String.IsNullOrEmpty(jobUrl))
            {
                status = restAPI.GetStatus(jobUrl);
            }
            else
            {
                status = restAPI.MakeGetCall("jobs", "", "?name=" + jobName);
            }
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

        private TitanJobInfo TranslateReply(string reply)
        {
            TitanJobInfo jobInfo = new TitanJobInfo();
            TitanJobStatus jobStatus = TitanJobStatus.invalid;
            XmlDocument doc = new XmlDocument();
            try
            {
                log.Debug("In TranslateReply reply= " + reply);
                doc.LoadXml(reply);
                XmlNode statusNode = doc.SelectSingleNode("job/state");
                jobStatus = (TitanJobStatus)Enum.Parse(typeof(TitanJobStatus), statusNode.InnerText);
                jobInfo.Status = jobStatus;
                if (jobStatus == TitanJobStatus.invalid)
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
        }
    }

    public class TitanRestApiCaller
    {
        private String _baseURL;

        private bool _useAuthentication;

        private String _key;

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public TitanRestApiCaller(String baseURL, bool useAuthentication, String key)
        {
            _baseURL = baseURL;
            _useAuthentication = useAuthentication;
            _key = key;
        }

        public CallStatus MakeAddCall(String objectToHandle, XmlDocument data)
        {

            try
            {
                string address = "http://10.4.1.43/restapi/" + objectToHandle;
                //string address = "http://elemental01.lab.conax.com/" + objectToHandle;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                // String key = Convert.ToBase64String(Encoding.UTF8.GetBytes("2VCBNOGDXMuKjWF4YyIA:"));
                // request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);

                request.Method = "POST";

                request.ProtocolVersion = HttpVersion.Version11;



                byte[] d = Encoding.UTF8.GetBytes(data.InnerXml);



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
                status.Success = response.StatusCode == HttpStatusCode.NoContent;// success
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                // log.Error("Something went wrong calling rest api for add call", ex);
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
                    // log.Error("Error response= " + sResponse);
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

        public CallStatus MakeGetCall(String objectToHandle, String id, String query)
        {
            try
            {
                String parameter = "";
                if (!String.IsNullOrEmpty(id))
                {
                    parameter = "/" + id;
                }
                else if (!String.IsNullOrEmpty(query))
                {
                    parameter = query;
                }
                string address = "http://10.4.1.43/restapi/" + objectToHandle + parameter;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                // String key = Convert.ToBase64String(Encoding.UTF8.GetBytes("De58KDloGYMePE5OqzFA:"));
                // request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                request.Method = "GET";
                request.Accept = "application/xml";
                WebResponse r = request.GetResponse();
                Stream response = r.GetResponseStream();
                StreamReader sr = new StreamReader(response);
                String reply = sr.ReadToEnd();
                sr.Close();
                response.Close();
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
                    //status = XmlReplyParser.ParseReply(sResponse);
                    //status.Error = sResponse;
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
        /// Updates the specified object.
        /// </summary>
        /// <param name="objectToHandle">The object type to updatae, ie products, services</param>
        /// <param name="id">The id of the object to update</param>
        /// <param name="data">The data to update with, ie content data if creating or updating content</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeUpdateCall(String objectToHandle, String id, XmlDocument data, String subData)
        {
            try
            {
                string address = "http://10.4.1.43/restapi/" + objectToHandle + "/" + id + "/" + subData;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                //String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                // request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                request.Method = "POST";

                request.ProtocolVersion = HttpVersion.Version11;

                // String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Rental-Offer><Name>Buy Matrix</Name><Price>16.5</Price><Rental-Period>01:00:00</Rental-Period><Cover-Id>1</Cover-Id><Vod-Ids type=\"array\"><Vod-Id>475</Vod-Id></Vod-Ids></Rental-Offer>";
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(xmlString);


                request.MediaType = "application/xml";

                request.ContentType = "application/xml;charset=UTF-8";

                if (data != null)
                {
                    byte[] d = Encoding.UTF8.GetBytes(data.InnerXml);
                    request.ContentLength = d.Length;

                    Stream requestStream = request.GetRequestStream();

                    // Send the request
                    requestStream.Write(d, 0, d.Length);
                    requestStream.Close();
                }
                else
                {
                    request.ContentLength = 0;
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                String reply = sr.ReadToEnd();
                sr.Close();
                response.Close();

                CallStatus status = new CallStatus();
                status.Success = response.StatusCode == HttpStatusCode.NoContent;// success
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                //log.Error("Error when calling update package offer with xml = " + data.InnerXml, ex);
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

        public CallStatus StartJob(String url, String queryString)
        {
            try
            {
                string address = url + "/" + queryString;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                //String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                // request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                request.Method = "POST";

                request.ProtocolVersion = HttpVersion.Version11;

                // String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Rental-Offer><Name>Buy Matrix</Name><Price>16.5</Price><Rental-Period>01:00:00</Rental-Period><Cover-Id>1</Cover-Id><Vod-Ids type=\"array\"><Vod-Id>475</Vod-Id></Vod-Ids></Rental-Offer>";
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(xmlString);


                request.MediaType = "application/xml";

                request.ContentType = "application/xml;charset=UTF-8";

                // request.ServicePoint.Expect100Continue = false;

                request.ContentLength = 0;


                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                String reply = sr.ReadToEnd();
                sr.Close();
                response.Close();

                CallStatus status = new CallStatus();
                status.Success = response.StatusCode == HttpStatusCode.NoContent;
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                //log.Error("Error when calling update package offer with xml = " + data.InnerXml, ex);
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

        public CallStatus GetStatus(String url)
        {
            try
            {
                string address = url;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                //String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                // request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                request.Method = "GET";

                request.ProtocolVersion = HttpVersion.Version11;

                // String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Rental-Offer><Name>Buy Matrix</Name><Price>16.5</Price><Rental-Period>01:00:00</Rental-Period><Cover-Id>1</Cover-Id><Vod-Ids type=\"array\"><Vod-Id>475</Vod-Id></Vod-Ids></Rental-Offer>";
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(xmlString);


                request.MediaType = "application/xml";

                request.ContentType = "application/xml;charset=UTF-8";


                // request.ContentLength = 0;


                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                String reply = sr.ReadToEnd();
                sr.Close();
                response.Close();

                CallStatus status = new CallStatus();
                status.Success = true;
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                //log.Error("Error when calling update package offer with xml = " + data.InnerXml, ex);
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

        /// <summary>
        /// Updates the specified object.
        /// </summary>
        /// <param name="url">The url of the resource to update</param>
        /// <param name="data">The data to update with, ie content data if creating or updating content, null can be sent if no data is needed.</param>
        /// <param name="objectToModify">The object to modify, ie preset etc.</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus AddDataCall(String url, XmlDocument data, String objectToModify)
        {
            try
            {
                string address = url + "/" + objectToModify;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                //String key = Convert.ToBase64String(Encoding.UTF8.GetBytes(_key + ":"));
                // request.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", key);
                request.Method = "POST";

                request.ProtocolVersion = HttpVersion.Version11;

                // String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Rental-Offer><Name>Buy Matrix</Name><Price>16.5</Price><Rental-Period>01:00:00</Rental-Period><Cover-Id>1</Cover-Id><Vod-Ids type=\"array\"><Vod-Id>475</Vod-Id></Vod-Ids></Rental-Offer>";
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(xmlString);


                request.MediaType = "application/xml";

                request.ContentType = "application/xml;charset=UTF-8";

                if (data != null)
                {
                    byte[] d = Encoding.UTF8.GetBytes(data.InnerXml);
                    request.ContentLength = d.Length;

                    Stream requestStream = request.GetRequestStream();

                    // Send the request
                    requestStream.Write(d, 0, d.Length);
                    requestStream.Close();
                }
                else
                {
                    request.ContentLength = 0;
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                String reply = sr.ReadToEnd();
                sr.Close();
                response.Close();

                CallStatus status = new CallStatus();
                status.Success = response.StatusCode == HttpStatusCode.NoContent;// success
                status.Data = reply;
                return status;
            }
            catch (WebException ex)
            {
                //log.Error("Error when calling update package offer with xml = " + data.InnerXml, ex);
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

    public class TitanJobInfo
    {

        public TitanJobStatus Status;

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
