using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Net;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Unified;
using System.Collections.Specialized;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.Web.Script.Serialization;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    public class UnifiedServicesWrapper : IUnifiedServicesWrapper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private String archiveApiHost = "";
        private String archiveApiUser = "";
        private String archiveApiPassword = "";

        public UnifiedServicesWrapper() { 

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "Unified").SingleOrDefault();
            Uri url = new Uri(systemConfig.GetConfigParam("UnifiedPlayerAPI"));

            archiveApiHost = url.GetLeftPart(UriPartial.Authority);
            archiveApiUser = systemConfig.GetConfigParam("UnifiedPlayerAPIUser");
            archiveApiPassword = systemConfig.GetConfigParam("UnifiedPlayerAPIPassword");
        }

        public RecordResult RecordSmoothContent(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime minStart, DateTime maxEnd)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "Unified").SingleOrDefault();
            String unifiedPlayerAPI = systemConfig.GetConfigParam("UnifiedPlayerAPI");

            String apiUrl = unifiedPlayerAPI;
            //WebClient myClient = new WebClient();
            //myClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            //myClient.Headers.Add("Content-Type", "multipart/form-data");

            log.Debug("RecordSmoothContent for content " + content.Name);

            String url = "";
            try
            {
                //DateTime dtFrom = DateTime.UtcNow.AddMinutes(-5);
                //DateTime dtTo = DateTime.UtcNow.AddMinutes(-1);
                //DateTime dtFrom = content.EventPeriodFrom.Value.AddMinutes(-1 * preGuardInMin);
                //DateTime dtTo = content.EventPeriodTo.Value.AddMinutes(postGuardInMin);
                DateTime dtFrom = minStart;
                DateTime dtTo = maxEnd;

                TimeSpan vbegin = UnifiedHelper.GetServerTimeStamp(dtFrom);
                TimeSpan vend = UnifiedHelper.GetServerTimeStamp(dtTo);

                EPGChannel epgChannel = CatchupHelper.GetEPGChannel(content);
                String stream = epgChannel.ServiceEpgConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType).Stream;
                Int32 pos = stream.IndexOf(".isml");
                url = stream.Substring(0, pos + 5) + "/Manifest?";
                //url += "http://storage01.lab.conax.com/content/live/nrk1/nrk1.isml/Manifest?";
                url += "vbegin=" + ((UInt64)vbegin.TotalSeconds).ToString() + "&";
                url += "vend=" + ((UInt64)vend.TotalSeconds).ToString();

                //String output = "0101-" + dtFrom.ToString("yyyyMMddHHmmss");
                var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType, AssetType.NPVR);
                String output = epgChannel.NameInAlphanumeric.ToLower() + "/" + content.ID + "/" + asset.Name;

                log.Debug("output= " + output);
                log.Debug("url= " + url);
                ////PostData postData = new PostData();
                ////postData.PostDataParams.Add(new PostDataParam("url", url));
                ////postData.PostDataParams.Add(new PostDataParam("output", output));
                ////RecordResult result = MakeCall(apiUrl, RequestMethod.POST, postData);
                ////string response = result.Message;

                NameValueCollection keyvaluepairs = new NameValueCollection();
                keyvaluepairs.Add("url", url);
                keyvaluepairs.Add("output", output);
                NPVRAssetLoger.WriteLog(content, "url " + url + " output " + output);
                //keyvaluepairs.Add("force_download", "1");
                //log.Debug("url " + url);

                // check if the manifest is empty or not.
                //log.Debug("Checking manifest");
                //RecordResult checkRes = CheckSmoothManifest(url);
                //if (checkRes.ReturnCode != 0) {
                //    log.Warn("Check chunks size failed on " + url + ", " + checkRes.Message);
                //    return checkRes;
                //}
          
                RecordResult regRes = MakeCall(apiUrl, RequestMethod.POST, keyvaluepairs);
                if (regRes.ReturnCode == 500 || regRes.ReturnCode == 1)
                {
                    throw new Exception(regRes.Message);
                }
                if (regRes.ReturnCode != 201)
                {
                    if (regRes.ReturnCode == 202)
                    {
                        log.Debug("Content " + content.Name + ":" + serviceViewLanugageISO + ":" + deviceType.ToString() + " already had running recordingjob");
                    }

                    // poll status
                    do
                    {
                        Int32 pollTime = 60000;
                        System.Threading.Thread.Sleep(pollTime);
                        log.Debug("Checking status on content " + content.Name + ":" + serviceViewLanugageISO + ":" + deviceType.ToString());
                        regRes = MakeCall(apiUrl, RequestMethod.POST, keyvaluepairs);
                        log.Debug("Status on content " + content.Name + ":" + serviceViewLanugageISO + ":" + deviceType.ToString() + " is " + regRes.ReturnCode);
                        if (regRes.ReturnCode == 500 || regRes.ReturnCode == 1)
                        {
                            throw new Exception(regRes.Message);
                        }
                    } while (regRes.ReturnCode == 423 || regRes.ReturnCode == 202);
                }
                else
                {

                    log.Debug("Content " + content.Name + ":" + serviceViewLanugageISO + ":" + deviceType.ToString() + " already had finished recordingjob");
                }
                //Byte[] responseArray = myClient.UploadValues(apiUrl, "POST", keyvaluepairs);
                //String response = Encoding.ASCII.GetString(responseArray);
                //myClient.Dispose();
                log.Debug("archive response " + regRes.Message);

                //String[] lines = regRes.Message.Split(Environment.NewLine.ToCharArray(),
                //                                StringSplitOptions.RemoveEmptyEntries);
                //pos = lines[lines.Length - 1].IndexOf("{");
                //String jasonStr = lines[lines.Length - 1].Substring(pos);


                //var jss = new JavaScriptSerializer();
                //RecordAPIResult sData = jss.Deserialize<RecordAPIResult>(jasonStr);
                String jasonStr = regRes.Message.Trim();
                var jss = new JavaScriptSerializer();
                RecordAPIResult sData = null;
                try
                {
                    sData = jss.Deserialize<RecordAPIResult>(jasonStr);
                }
                catch (Exception)
                {
                    throw new Exception("Incorrect response data format: " + jasonStr);
                }


                Int32 state = sData.files.Count(f => !f.status.Equals("0"));
                if (state > 0)
                    throw new Exception("Failed to record vod asset, status was not ok the result was: " + jasonStr);
                
                Int32 size = sData.files.Count(f => f.size.Equals("0"));
                if (size > 0)
                    throw new Exception("Failed to record vod asset, asset size was 0 the result was: " + jasonStr);

                RecordResult res = new RecordResult();
                res.ReturnCode = 0;
                res.Message = jasonStr;
                return res;
            }
            catch (Exception ex)
            {
                log.Warn("Failed to Record Smooth asset " + url + " " + ex.Message, ex);
                RecordResult res = new RecordResult();
                res.ReturnCode = -1;
                res.Message = ex.Message;
                res.Exception = ex;
                return res;
            }
        }

        public RecordResult CheckSmoothManifest(String url)
        {
            try
            {
                RecordResult res = MakeCall(url, RequestMethod.GET, null);
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.LoadXml(res.Message);
                }
                catch (Exception ex)
                {
                    log.Error("Error when loading manifest from url " + url + ", encoder might be down", ex);
                    throw;
                }

                XmlElement audioNode = (XmlElement)doc.SelectSingleNode("SmoothStreamingMedia/StreamIndex[@Type='audio']");
                String audioChunks = audioNode.GetAttribute("Chunks");

                XmlElement videoNode = (XmlElement)doc.SelectSingleNode("SmoothStreamingMedia/StreamIndex[@Type='video']");
                String videoChunks = videoNode.GetAttribute("Chunks");
                

                RecordResult checkRes = new RecordResult();
                checkRes.ReturnCode = 0;
                if (UInt64.Parse(audioChunks) < 2) {
                    checkRes.ReturnCode = -2;
                    checkRes.Message = "Audio chunks size is " + audioChunks + " it seems like the manifest is emopty.";
                }
                if (UInt64.Parse(videoChunks) < 2)
                {
                    checkRes.ReturnCode = -2;
                    checkRes.Message = "Video chunks size is " + videoChunks + " it seems like the manifest is emopty.";
                }

                return checkRes;
            }
            catch (Exception ex) {
                RecordResult res = new RecordResult();
                res.ReturnCode = -1;
                res.Message = ex.Message;
                res.Exception = ex;
                return res;
            }
        }

        public RecordResult GetSmoothAssetStatus(String output)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "Unified").SingleOrDefault();
            String unifiedPlayerAPI = systemConfig.GetConfigParam("UnifiedPlayerAPI");

            
            if (!unifiedPlayerAPI.EndsWith("/"))
                unifiedPlayerAPI += "/";
            String apiUrl = unifiedPlayerAPI;

            try
            {
                apiUrl += output;
                log.Debug("Calling CodeShop on url " + apiUrl);
                RecordResult res = MakeCall(apiUrl, RequestMethod.GET, new NameValueCollection());

                return res;
            }
            catch (Exception ex)
            {
                log.Warn("Failed to get Smooth asset status " + apiUrl + " " + ex.Message, ex);
                RecordResult res = new RecordResult();
                res.ReturnCode = -1;
                res.Message = ex.Message;
                res.Exception = ex;
                return res;
            }
        }

        public RecordResult DeleteSmoothAsset(ContentData content, Asset assetToDelete)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "Unified").SingleOrDefault();
            String unifiedPlayerAPI = systemConfig.GetConfigParam("UnifiedPlayerAPI");


            if (!unifiedPlayerAPI.EndsWith("/"))
                unifiedPlayerAPI += "/";
            String apiUrl = unifiedPlayerAPI;

            try
            {
                EPGChannel epgChannel = CatchupHelper.GetEPGChannel(content);
                if (epgChannel == null)
                {
                    String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;
                    log.Warn("Channel with mppContentId " + channelId + " doesn't seem to exist in the epgChannelConfig any more, purge cant be performed!");
                    return new RecordResult() { Message = "Channel with mppContentId " + channelId + " doesn't seem to exist in the epgChannelConfig any more, purge cant be performed!", ReturnCode = 500};
                }
                apiUrl += epgChannel.NameInAlphanumeric.ToLower() + "/" + content.ID + "/" + assetToDelete.Name;

                RecordResult res = MakeDeleteCall(apiUrl, RequestMethod.DELETE);

                return res;
            }
            catch (Exception ex)
            {
                log.Warn("Failed to Delete Smooth asset status " + apiUrl + " " + ex.Message, ex);
                RecordResult res = new RecordResult();
                res.ReturnCode = -1;
                res.Message = ex.Message;
                res.Exception = ex;
                return res;
            }
        }

        private RecordResult MakeCall(String url, RequestMethod method, NameValueCollection postDate)
        {
            //WebClient myClient = new WebClient();
            using (WebClient myClient = new WebClient())
            {
                try
                {

                    myClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    //var credentialCache = new CredentialCache();
                    //credentialCache.Add(
                    //  new Uri(archiveApiHost), // request url's host
                    //  "Digest",  // authentication type 
                    //  new NetworkCredential(archiveApiUser, archiveApiPassword) // credentials 
                    //);
                    //myClient.Credentials = credentialCache;

                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(archiveApiUser + ":" + archiveApiPassword));
                    // Inject this string as the Authorization header
                    myClient.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", credentials);

                    String response = "";
                    RecordResult res = new RecordResult();
                    switch (method)
                    {
                        case RequestMethod.GET:
                            try
                            {
                                log.Debug("Open on " + url + " GET");
                                Stream myStream = myClient.OpenRead(url);
                                StreamReader sr = new StreamReader(myStream);
                                response = sr.ReadToEnd();
                                myStream.Close();
                                res.Message = response;
                                String statusDescription = "";
                                Int32 rc = GetStatusCode(myClient, out statusDescription);
                                res.ReturnCode = rc;
                                log.Debug("status " + rc + " Description " + statusDescription + " from " + url + " GET");
                            }
                            catch (WebException we)
                            {
                                if (we.Status == WebExceptionStatus.ProtocolError)
                                {
                                    HttpWebResponse wer = (HttpWebResponse)we.Response;
                                    res.Exception = we;
                                    res.Message = wer.StatusDescription;
                                    res.ReturnCode = (Int32)wer.StatusCode;
                                    log.Debug("status " + res.ReturnCode + " Description " + res.Message + " from " + url + " GET");
                                    break;
                                }
                                throw;
                            }
                            break;
                        case RequestMethod.POST:
                            log.Debug("Open on " + url + " POST");
                            Byte[] responseArray = myClient.UploadValues(url, method.ToString().ToUpper(), postDate);
                            response = Encoding.ASCII.GetString(responseArray);
                            String statusDescription2 = "";
                            Int32 rc2 = GetStatusCode(myClient, out statusDescription2);
                            log.Debug("status " + rc2 + " Description " + statusDescription2 + " from " + url + " POST");
                            res.Message = response;
                            res.ReturnCode = rc2;
                            break;
                    }
                    //myClient.Dispose();


                    return res;
                }
                catch (Exception ex)
                {

                    String dataStr = "";
                    if (postDate != null)
                    {
                        foreach (String key in postDate.Keys)
                        {
                            dataStr += key + " " + postDate[key] + " / ";
                        }
                    }

                    log.Warn("Failed to makee call to " + url + " " + method.ToString() + " " + dataStr + " " + ex.Message, ex);

                    RecordResult res = new RecordResult();
                    res.Message = ex.Message;
                    res.ReturnCode = -1;
                    res.Exception = ex;

                    return res;
                }
            }
        }

        private RecordResult MakeDeleteCall(String rul, RequestMethod method)
        {
            RecordResult res = new RecordResult();
            try
            {                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(rul);
                request.Method = method.ToString().ToUpper();

                //var credentialCache = new CredentialCache();
                //credentialCache.Add(
                //  new Uri(archiveApiHost), // request url's host
                //  "Digest",  // authentication type 
                //  new NetworkCredential(archiveApiUser, archiveApiPassword) // credentials 
                //);
                //request.Credentials = credentialCache;

                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(archiveApiUser + ":" + archiveApiPassword));
                // Inject this string as the Authorization header
                request.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", credentials);

                WebResponse r = request.GetResponse();
                Stream response = r.GetResponseStream();
                StreamReader sr = new StreamReader(response);
                String reply = sr.ReadToEnd();
                sr.Close();
                response.Close();
                
                res.ReturnCode = 0;
                res.Message = reply;               
            }
            catch (WebException we)
            {
                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse wer = (HttpWebResponse)we.Response;
                    if (wer.StatusCode.ToString() == "423")
                    {
                        res.Exception = we;
                        res.Message = wer.StatusDescription;
                        res.ReturnCode = 423;
                        return res;
                    }
                    else if (wer.StatusCode == HttpStatusCode.NotFound)
                    {
                        res.Exception = we;
                        res.Message = wer.StatusDescription;
                        res.ReturnCode = 404;
                        return res;
                    } else {
                        Match match = Regex.Match(we.Message,
                                                          @"40\d",
                                                          RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            res.Exception = we;
                            res.Message = we.Message;
                            res.ReturnCode = Int32.Parse(match.Value);
                            return res;
                        }
                    }
                }
                throw;
                //CallStatus status = new CallStatus();
                //status.Success = false;
                //if (ex.Response != null)
                //{
                //    string sResponse = "";
                //    Stream strm = ex.Response.GetResponseStream();
                //    byte[] bytes = new byte[strm.Length];
                //    strm.Read(bytes, 0, (int)strm.Length);
                //    sResponse = System.Text.Encoding.ASCII.GetString(bytes);
                //    status.Error = sResponse;
                //    status.Success = false;
                //}
                //return status;
            }
            catch (Exception ex)
            {
                res.ReturnCode = -1;
                res.Message = ex.Message;
                res.Exception = ex;
                return res;
            }
            return res;
        }

        private Int32 GetStatusCode(WebClient client, out string statusDescription)
        {
            FieldInfo responseField = client.GetType().GetField("m_WebResponse", BindingFlags.Instance | BindingFlags.NonPublic);

            if (responseField != null)
            {
                HttpWebResponse response = responseField.GetValue(client) as HttpWebResponse;

                if (response != null)
                {
                    statusDescription = response.StatusDescription;
                    return (Int32)response.StatusCode;
                }
            }

            statusDescription = null;
            return 0;
        }

        public void DeleteBuffer(String apiUrl)
        {
            //curl -X POST http://www.examples.com/stream1/stream1.isml/purge?t=02:00:00

            WFMWebClient myClient = new WFMWebClient(1800000);
            myClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");            
            try
            {
                log.Debug("start purge buffer");
                Byte[] responseArray = myClient.UploadValues(apiUrl, "POST", new NameValueCollection());
                String response = Encoding.ASCII.GetString(responseArray);
                myClient.Dispose();
                log.Debug("Done purge buffer");
                //Stream myStream = myClient.OpenRead(apiUrl);
                //StreamReader sr = new StreamReader(myStream);
                //String response = sr.ReadToEnd();
                //myStream.Close();               
            }
            catch (Exception ex)
            {
                log.Warn("Failed to delete buffer from " + apiUrl + " " + ex.Message, ex);
            }
        }
    }

    enum RequestMethod {
        GET,
        POST,
        PUT,
        DELETE
    }

    class RecordAPIResult
    {
        public List<RecordFiles> files { get; set; }
    }

    class RecordFiles
    {
        public String output {get; set;}
        public String status {get; set;}
        public String size { get; set; }
    }

    public class RecordResult {
        public Int32 ReturnCode { get; set; }
        public String Message { get; set; }
        public Exception Exception { get; set; }
    }

    class WFMWebClient : WebClient
    {
        //time in milliseconds
        private int timeout;
        public int Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }

        public WFMWebClient()
        {
            this.timeout = 60000;
        }

        public WFMWebClient(int timeout)
        {
            this.timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var result = base.GetWebRequest(address);
            result.Timeout = this.timeout;
            return result;
        }
    }
}
