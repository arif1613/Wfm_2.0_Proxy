using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Harmonic;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Net;
using System.IO;
using System.Xml;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.HarmonicOrigin;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.Text.RegularExpressions;
using System.Security;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Carbon;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    public class HarmonicOriginWrapper : IHarmonicOriginWrapper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private SystemConfig systemConfig = Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "HarmonicOrigin");

        private HarmonicOriginRestApi restApi;

        public HarmonicOriginWrapper()
        {
            String endpoint = systemConfig.GetConfigParam("Endpoint");
            restApi = new HarmonicOriginRestApi(endpoint); 
        }

        public CallStatus DeleteSmoothContent(ContentData content, Asset assetToDelete)
        {

            String resource = "llcu/ss/asset";

            CallStatus status = restApi.MakeDeleteCall(resource, assetToDelete.Name);
            return status;
        }

        public CallStatus RecordSmoothContent(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime startTime, DateTime endTime)
        {

            EPGChannel channel = CatchupHelper.GetEPGChannel(content);
            String SSStreamName = CarbonEncoderHelper.GetStreamName(channel.ServiceEpgConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType).Stream);

            String resource = "llcu/ss/asset";            

            //DateTime startTime = DateTime.UtcNow.AddMinutes(-15);
            //DateTime endTime = DateTime.UtcNow.AddMinutes(-5);
            //DateTime startTime = content.EventPeriodFrom.Value;
            //DateTime endTime = content.EventPeriodTo.Value;

            String xml = "";
            xml += "<Content xmlns=\"urn:eventis:cpi:1.0\">";
            xml += "<SourceUri>" + SecurityElement.Escape( SSStreamName) + "</SourceUri>";
            xml += "<IngestStartTime>" + startTime.ToString("yyyy-MM-ddTHH:mm:ssZ") + "</IngestStartTime>";
            xml += "<IngestEndTime>" + endTime.ToString("yyyy-MM-ddTHH:mm:ssZ") + "</IngestEndTime>";
            xml += "</Content>";
            XmlDataDocument doc = new XmlDataDocument();
            doc.LoadXml(xml);

            var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType, AssetType.NPVR);

            CallStatus status = restApi.MakeUpdateCall(resource, asset.Name, doc);
            return status;
        }

        public void GetAllSmoothNPVRAssets() {
            String resource = "llcu/ss/llcuassetlist";

            CallStatus status = restApi.MakeGetCall(resource, String.Empty);
        }

        public OriginVODAsset GetAssetData(ContentData content, Asset asset)
        {
            String assetID = GetOriginAssetID(content, asset.IsTrailer);
            AssetFormatType assetType = ConaxIntegrationHelper.GetAssetFormatTypeFromAsset(asset);
            String resource = "";
            if (assetType == AssetFormatType.HTTPLiveStreaming)
                resource = "vod/HLS/asset";
            else if (assetType == AssetFormatType.SmoothStreaming)
                resource = "vod/SS/asset";
            CallStatus status = restApi.MakeGetCall(resource, assetID);
            if (!status.Success)
                throw new Exception("Error fetching path for asset with AssetID = " + assetID);
            OriginVODAsset originAsset = ParseVodAssetReply(status.Data);
            return originAsset;
        }

        private OriginVODAsset ParseVodAssetReply(string data)
        {
            log.Debug("Parsing assetReply, data = " + data);
            log.Debug("Removing xsd data from reply");
            data = "<?xml version=\"1.0\" encoding=\"utf-8\"?><vodAsset>" + data.Substring(199);
            log.Debug("data = " + data);
            OriginVODAsset asset = new OriginVODAsset();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            XmlNode dataNode = doc.SelectSingleNode("vodAsset/assetname");
            if (dataNode != null)
                asset.AssetName = dataNode.InnerText;
            dataNode = doc.SelectSingleNode("vodAsset/assetPath");
            if (dataNode != null)
            {
                asset.AssetPath = dataNode.InnerText;
                log.Debug("AssetPath = " + asset.AssetPath);
            }
            dataNode = doc.SelectSingleNode("vodAsset/opState");
            if (dataNode != null)
                asset.OpState = dataNode.InnerText;
            dataNode = doc.SelectSingleNode("vodAsset/adminState");
            if (dataNode != null)
                asset.AdminState = dataNode.InnerText;
            dataNode = doc.SelectSingleNode("vodAsset/size");
            if (dataNode != null)
                asset.Size = dataNode.InnerText;
            return asset;
        }

        public bool DeleteAssetsFromOrigin(ContentData content)
        {
            List<String> assetIDs = GetAssetIDs(content);
            List<AssetFormatType> assetTypes = ConaxIntegrationHelper.GetEncodingTypes(content, false); // both trailer and nontrailer will have same types
            foreach (String assetID in assetIDs)
            {
                log.Debug("Deleting asset with assetID " + assetID);
                foreach (AssetFormatType assetFormatType in assetTypes)
                {
                    String url = "vod/SS/Asset";
                    if (assetFormatType == AssetFormatType.HTTPLiveStreaming)
                        url = "vod/HLS/Asset";
                    CallStatus status = restApi.MakeDeleteCall(url, assetID);
                    if (!status.Success)
                    {
                        log.Warn("Error when deleting asset for type " + assetFormatType.ToString());
                    }
                }
            }
            return true;
        }

        public bool SetAdminState(ContentData content, OriginState state, bool trailer)
        {
            String assetID = GetAssetID(content, trailer);
            List<AssetFormatType> assetTypes = ConaxIntegrationHelper.GetEncodingTypes(content, false); // both trailer and nontrailer will have same types

            log.Debug("Changing state on asset with assetID " + assetID + " for content with id " + content.ID.ToString() + " to " + state.ToString());
            foreach (AssetFormatType assetFormatType in assetTypes)
            {
                String url = "vod/SS/asset";
                if (assetFormatType == AssetFormatType.HTTPLiveStreaming)
                    url = "vod/HLS/asset";
                XmlDocument doc = new XmlDocument();
                if (state == OriginState.Online)
                {
                    doc.LoadXml(SetOnlineStatusXml);
                }
                else
                {
                    doc.LoadXml(SetOfflineStatusXml);
                }
                //XmlNode node = doc.SelectSingleNode("vodAsset/adminState");
                //node.InnerText = state.ToString();
                log.Debug("Calling restApi on url " + url);
                CallStatus status = restApi.MakeUpdateCall(url, assetID, doc);
                if (!status.Success)
                {
                    throw new Exception("Error updating asset with ID " + assetID + " for content with ID " + content.ID.ToString(), status.Exception);
                }
                log.Debug("Call done");
            }

            return true;
        }

        public String GetOriginAssetID(ContentData content, bool trailer)
        {
            String postfix = "";
            String ret = content.ID.ToString();
            if (trailer)
                ret += "_trailer";
            if (systemConfig.ConfigParams.ContainsKey("AssetIdPostfix") &&
                !String.IsNullOrEmpty((systemConfig.GetConfigParam("AssetIdPostfix"))))
                postfix = systemConfig.GetConfigParam("AssetIdPostfix");
            ret += postfix;
            return  ret;
        }

        private List<String> GetAssetIDs(ContentData content)
        {
            List<String> ret = new List<string>();
            ret.Add(GetOriginAssetID(content, false)); // always atleast a non trailer
            Asset asset = content.Assets.FirstOrDefault<Asset>(a => a.IsTrailer);
            if (asset != null)
                ret.Add(GetOriginAssetID(content, true));
            return ret;
        }

        private String GetAssetID(ContentData content, bool trailer)
        {
            String ret = GetOriginAssetID(content, trailer);
            return ret;
        }

        private static String SetOnlineStatusXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><vodAsset xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://www.harmonicinc.com/REST_OS_AMI/v1_0\"><adminState>Online</adminState></vodAsset>";

        private static String SetOfflineStatusXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><vodAsset xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://www.harmonicinc.com/REST_OS_AMI/v1_0\"><adminState>Offline</adminState></vodAsset>";
    }

    public class HarmonicOriginRestApi
    {

        private String _baseURL;

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HarmonicOriginRestApi(String baseURL)
        {
            _baseURL = baseURL;
        }

        /// <summary>
        /// This method fetches a object from the rest apy
        /// </summary>
        /// <param name="objectToHandle">The object type to handle, ie products, services</param>
        /// <param name="id">The id of the object to fetch data from, if "" is sent all will be fetched.</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeGetCall(String objectToHandle, String id)
        {
            String address ="";
            try
            {
                String parameter = "";
                if (!String.IsNullOrEmpty(id))
                {
                    parameter = "/" + id;
                }
                address = _baseURL + "/ami/" + objectToHandle + parameter;
                log.Debug("Fetching assetInformation on url " + address);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                request.Method = "GET";
                request.ProtocolVersion = HttpVersion.Version11;
                request.UserAgent = "XTendManager 1.0";
                request.Headers.Add("X-REST-OS_AMI-Version", "X-REST-OS_AMI-Version:1.0");

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
                    status = XmlReplyParser.ParseReply(sResponse);
                    status.Error = (!String.IsNullOrEmpty(sResponse)) ? sResponse : ex.Message ;
                }
                return status;
            }
            catch (Exception e)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Error = "Failed on " + address + " " + e.Message;
                status.Exception = e;
                return status;
            }
        }

        /// <summary>
        /// Creates a object of the specified type.
        /// </summary>
        /// <param name="objectToHandle">The object type to create, ie products, services</param>
        /// <param name="data">The data to use when creating the object</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeAddCall(String objectToHandle, XmlDocument data)
        {
            string address = "";
            try
            {
                address = _baseURL + "/ami/" + objectToHandle;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version11;
                request.UserAgent = "XTendManager 1.0";
                request.Headers.Add("X-REST-OS_AMI-Version", "X-REST-OS_AMI-Version:1.0");

                //String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><vod><name>Matrix special</name><cover-id>2767</cover-id><cast></cast><director></director><producer></producer><screenplay></screenplay><runtime>3465466</runtime><description>Nice movie!</description><extended-description>More about this movie!</extended-description><release-date type=\"date\"></release-date><country></country><mpaa-rating-id></mpaa-rating-id><vchip-rating-id></vchip-rating-id><content-rating-ids type=\"array\"><content-rating-id></content-rating-id></content-rating-ids><category-ids type=\"array\"><category-id></category-id></category-ids><genre-ids type=\"array\"><genre-id></genre-id></genre-ids><contents-attributes type='array'><content><profile-id>1</profile-id><trailer>http://video.server.com/content/354632</trailer><source>http://video.server.com/content/967323</source></content></contents-attributes></vod>";
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(xmlString);
                byte[] d = Encoding.UTF8.GetBytes(data.InnerXml);

                if (!objectToHandle.Equals("covers"))
                {
                    log.Debug("<------------------------------------------------>");
                    log.Debug("Adding " + address + " to Origin using xml= " + data.InnerXml);
                    log.Debug("<------------------------------------------------>");
                }

                request.MediaType = "application/xml";

                request.ContentType = "application/xml;charset=UTF-8";
                request.ContentLength = d.Length;

                Stream requestStream = request.GetRequestStream();

                // Send the request
                requestStream.Write(d, 0, d.Length);
                requestStream.Close();

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
                status.Error = "Failed on " + address + " " + e.Message;
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
        public CallStatus MakeUpdateCall(String objectToHandle, String id, XmlDocument data)
        {
            String address = "";
            try
            {
                address = _baseURL + "/ami/" + objectToHandle;
                if (!String.IsNullOrWhiteSpace(id))
                    address += "/" + id;
                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

                request.Method = "PUT";
                request.ProtocolVersion = HttpVersion.Version11;
                request.UserAgent = "XTendManager 1.0";
                request.Headers.Add("X-REST-OS_AMI-Version", "X-REST-OS_AMI-Version:1.0");

               // String xmlString = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Rental-Offer><Name>Buy Matrix</Name><Price>16.5</Price><Rental-Period>01:00:00</Rental-Period><Cover-Id>1</Cover-Id><Vod-Ids type=\"array\"><Vod-Id>475</Vod-Id></Vod-Ids></Rental-Offer>";
                //XmlDocument doc = new XmlDocument();
                //doc.LoadXml(xmlString);
                byte[] d = Encoding.UTF8.GetBytes(data.InnerXml);

                log.Debug("<------------------------------------------------>");
                log.Debug("Update " + address + " to Origin using xml= " + data.InnerXml);
                log.Debug("<------------------------------------------------>");

                request.MediaType = "application/xml";

                request.ContentType = "application/xml;charset=UTF-8";

                request.ContentLength = d.Length;

                Stream requestStream = request.GetRequestStream();

                // Send the request
                requestStream.Write(d, 0, d.Length);
                requestStream.Close();

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
                log.Error("Error when calling update with xml = " + data.InnerXml, ex);
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Exception = ex;
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
                status.Error = "Failed on " + address + " " + e.Message;
                status.Exception = e;
                return status;
            }
        }

        /// <summary>
        /// Deletes an object.
        /// </summary>
        /// <param name="objectToHandle">The object type to delete, ie products, services</param>
        /// <param name="id">The id of the object to delete</param>
        /// <returns>CallStatus containing data if the call was successfull and if it wasnt it contains error code.</returns>
        public CallStatus MakeDeleteCall(String objectToHandle, String id)
        {
            String address = "";
            try
            {
                address = _baseURL + "/ami/" + objectToHandle + "/" + id;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                log.Debug("adress for deletecall " + address);
              
                request.Method = "DELETE";
                request.ProtocolVersion = HttpVersion.Version11;
                request.UserAgent = "XTendManager 1.0";
                request.Headers.Add("X-REST-OS_AMI-Version", "X-REST-OS_AMI-Version:1.0");

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
            catch (WebException we)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Exception = we;
                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse wer = (HttpWebResponse)we.Response;
                    if (wer.StatusCode.ToString() == "423")
                    {
                        status.Exception = we;
                        status.Error = wer.StatusDescription;
                        status.ErrorCode = 423;
                        return status;
                    }
                    else if (wer.StatusCode == HttpStatusCode.NotFound)
                    {
                        status.Exception = we;
                        status.Error = wer.StatusDescription;
                        status.ErrorCode = 404;
                        return status;
                    }
                    else
                    {
                        Match match = Regex.Match(we.Message,
                                                          @"40\d",
                                                          RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            status.Exception = we;
                            status.Error = we.Message;
                            status.ErrorCode = Int32.Parse(match.Value);
                            return status;
                        }
                    }
                }
                throw;                
            }
            catch (Exception e)
            {
                CallStatus status = new CallStatus();
                status.Success = false;
                status.Error = "Failed on " + address + " " + e.Message;
                status.Exception = e;
                return status;
            }
        }
    }
}
