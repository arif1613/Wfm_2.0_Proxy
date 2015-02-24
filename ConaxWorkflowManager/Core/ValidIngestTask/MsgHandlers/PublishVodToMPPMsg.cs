using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.XmlOperation;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.MsgHandlers
{
    public class PublishVodToMPPMsg
    {
        private static BrokeredMessage _brokeredMessage;
        private static DateTime _dt;
        private static Mpp5Configuration _Mpp5Config;
        private static Mpp5IdentityModel _mpp5IdentityModel { get; set; }
        private static LiveEventModel _liveEventModel { get; set; }

        public PublishVodToMPPMsg()
        {
            _Mpp5Config = (Mpp5Configuration)
                Config.GetConfig()
                    .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.MPP5);
            _mpp5IdentityModel = new Mpp5IdentityModel()
            {
                ClientID = _Mpp5Config.ClientID,
                HolderId = _Mpp5Config.HolderID,
                Password = _Mpp5Config.Password,
                PrivateKey = _Mpp5Config.PrivateKey,
                UserName = _Mpp5Config.UserName
            };
        }
        public PublishVodToMPPMsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            _Mpp5Config = (Mpp5Configuration)
                Config.GetConfig()
                    .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.MPP5);
            _mpp5IdentityModel = new Mpp5IdentityModel()
            {
                ClientID = _Mpp5Config.ClientID,
                HolderId = _Mpp5Config.HolderID,
                Password = _Mpp5Config.Password,
                PrivateKey = _Mpp5Config.PrivateKey,
                UserName = _Mpp5Config.UserName
            };
        }

        public void getAllVods()
        {
            var apiBaseUrl = _Mpp5Config.RestApiUrl;
            var resource = "/" + _mpp5IdentityModel.HolderId + "/vods";
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string AuthenticationMac = gs.Build(HttpMethodType.GET, resource);
            using (var client = new HttpClient())
            {
                string url = apiBaseUrl + resource;
                Console.WriteLine(url);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", "MAC " + AuthenticationMac);
                // Start request
                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                }
            }
        }
        public void getAVod(string Id)
        {
            var apiBaseUrl = _Mpp5Config.RestApiUrl;
            string id = Id;
            var resource = "/" + _mpp5IdentityModel.HolderId + "/vods/" + id;
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string AuthenticationMac = gs.Build(HttpMethodType.GET, resource);
            string url = apiBaseUrl + resource;
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            //webReq.ContentType = "application/xml";
            webReq.Headers.Add("Authorization", "MAC " + AuthenticationMac);
            webReq.Method = "GET";
            webReq.ContentType = "application/json";
            byte[] byteData = Encoding.ASCII.GetBytes("");
            webReq.ContentLength = byteData.Length;

            using (HttpWebResponse resp = webReq.GetResponse() as HttpWebResponse)
            {
                using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string value = reader.ReadToEnd();
                    Console.WriteLine(value);
                }
            }

        }

        public void getAVod_byname(string name)
        {
            var apiBaseUrl = _Mpp5Config.RestApiUrl;
            var resource = "/" + _mpp5IdentityModel.HolderId + "/vods/sortby/createddate/sortdirection/desc/from/0/to/3";
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string AuthenticationMac = gs.Build(HttpMethodType.GET, resource);
            string url = apiBaseUrl + resource;
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            //webReq.ContentType = "application/xml";
            webReq.Headers.Add("Authorization", "MAC " + AuthenticationMac);
            webReq.Method = "GET";
            webReq.ContentType = "application/json";
            byte[] byteData = Encoding.ASCII.GetBytes("");
            webReq.ContentLength = byteData.Length;

            using (HttpWebResponse resp = webReq.GetResponse() as HttpWebResponse)
            {
                using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string value = reader.ReadToEnd();
                    Console.WriteLine(value.Length);
                }
            }
            //https://vod.mpp5.devmpp.com/6559faa5cbee47149bbb751afd3cbd13/vods/sortby/createddate/sortdirection/desc/from/0/to/100
        }

        public void CreateAVodInMpp5()
        {
            string mppPublishingProfileFileName = _brokeredMessage.Properties["FileName"].ToString();
            //const string mppPublishingProfileFileName =
            //    @"C:\MPS\conax\Ingest\VodPublishingDir\NeedsQA\HBO\HBO All Regions\Conax Test 1.xml";
            var mppProfileXmlTranslatorVod = new MppProfileXmlTranslatorVOD(mppPublishingProfileFileName);
            var createVodModel = new VodModel();
            createVodModel.ActiveDirectoryRestricted = false;
            createVodModel.Id = Guid.NewGuid();
            createVodModel.Name = "without description";
            //createVodModel.WamsAccountId = "15a1bbaf00b64f2fe1a03a150c30f6ef";
            createVodModel.Status = mppProfileXmlTranslatorVod.PublishState();

            #region content property

            createVodModel.MetaData = new Dictionary<string, Dictionary<CultureInfo, string>>
            {
             
                {
                    "EventPeriodTo", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.EventPeriodTo()}
                    }
                },
                {
                    "EventPeriodFrom", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.EventPeriodFrom()}
                    }
                },
                {
                    "ProductionYear", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.ProductionYear()}
                    }
                },
                {
                    "RunningTime", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.RunningTime()}
                    }
                },
                {
                    "EnableQA", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.EnableQA()}
                    }
                },
                {
                    "IngestSource", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.IngestSource()}
                    }
                },
                {
                    "UriProfile", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.URIProfile()}
                    }
                },
                {
                    "EoisodeName", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.EpisodeName()}
                    }
                },
                {
                    "Country", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.Country()}
                    }
                },
                {
                    "Director", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.Director()}
                    }
                },
                {
                    "Producer", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.Producer()}
                    }
                },
                {
                    "Category", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.Category()}
                    }
                },
                {
                    "Genre", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.Genre()}
                    }
                },
                {
                    "ContentType", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.ContentType()}
                    }
                },
                {
                    "MovieRating", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.MovieRating()}
                    }
                },
                {
                    "ConaxContegoId", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.ConaxContegoContentID()}
                    }
                },
                {
                    "PublishState", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.PublishState()}
                    }
                },
                {
                    "IngestIdentifier", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, mppProfileXmlTranslatorVod.IngestIdentifier()}
                    }
                },
                {
                    "LanguageInfo_ISO", new Dictionary<CultureInfo, string>
                    {
                        {
                            CultureInfo.CurrentUICulture,
                            mppProfileXmlTranslatorVod.LanguageInfo().ContainsKey("ISO").ToString()
                        }
                    }
                },
                {
                    "LanguageInfo_Title", new Dictionary<CultureInfo, string>
                    {
                        {
                            CultureInfo.CurrentUICulture,
                            mppProfileXmlTranslatorVod.LanguageInfo().ContainsKey("Title").ToString()
                        }
                    }
                },
                {
                    "LanguageInfo_SortName", new Dictionary<CultureInfo, string>
                    {
                        {
                            CultureInfo.CurrentUICulture,
                            mppProfileXmlTranslatorVod.LanguageInfo().ContainsKey("SortName").ToString()
                        }
                    }
                },
                {
                    "LanguageInfo_ShortDescription", new Dictionary<CultureInfo, string>
                    {
                        {
                            CultureInfo.CurrentUICulture,
                            mppProfileXmlTranslatorVod.LanguageInfo().ContainsKey("ShortDescription").ToString()
                        }
                    }
                },
                {
                    "description", new Dictionary<CultureInfo, string>
                    {
                        {
                            CultureInfo.CurrentUICulture,
                            mppProfileXmlTranslatorVod.LanguageInfo().ContainsKey("LongDescription").ToString()
                        }
                    }
                },
                {
                    "LanguageInfo_ImageClassification", new Dictionary<CultureInfo, string>
                    {
                        {
                            CultureInfo.CurrentUICulture,
                            mppProfileXmlTranslatorVod.LanguageInfo().ContainsKey("ImageClassification").ToString()
                        }
                    }
                },
                {
                    "LanguageInfo_ImageClientGUIname", new Dictionary<CultureInfo, string>
                    {
                        {
                            CultureInfo.CurrentUICulture,
                            mppProfileXmlTranslatorVod.LanguageInfo().ContainsKey("ImageClientGUIname").ToString()
                        }
                    }
                },
                {
                    "LanguageInfo_ImageFileName", new Dictionary<CultureInfo, string>
                    {
                        {
                            CultureInfo.CurrentUICulture,
                            mppProfileXmlTranslatorVod.LanguageInfo().ContainsKey("ImageFileName").ToString()
                        }
                    }
                },

            };

            #endregion




            //json serialization
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                ContractResolver = new UnderscoreMappingResolver()
            });
            /////
            var apiBaseUrl = _Mpp5Config.RestApiUrl;
            Console.WriteLine(apiBaseUrl);

            var resource = "/" + _mpp5IdentityModel.HolderId + "/vods/";
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string signature = gs.Build(HttpMethodType.POST, resource);

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, apiBaseUrl + resource);
                request.Headers.Add("Authorization", "MAC " + signature);
                var ms = new MemoryStream();
                var sw = new StreamWriter(ms);
                var ser = JsonSerializer.Create(new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                ContractResolver = new UnderscoreMappingResolver()
            });
                ser.Serialize(sw, createVodModel);
                sw.Flush();
                ms.Position = 0;
                request.Content = new StreamContent(ms);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    var stream = response.Content.ReadAsStreamAsync().Result;
                    Console.WriteLine("Successful API Call. for " + createVodModel.Id);
                }
                else
                {
                    Console.WriteLine("Failed to call API.");
                }

            }
            Thread.Sleep(15000);
            //getAVod(createVodModel.Id.ToString());

        }

        //public void createMppChannelContent()
        //{
        //    var createLiveEventContent = new LiveEventModel();
        //    createLiveEventContent.Id = Guid.NewGuid();
        //    createLiveEventContent.Name = "LiveEventconax";
        //    createLiveEventContent.Archived = false;
        //    createLiveEventContent.HiveAccelerated = false;
        //    createLiveEventContent.WamsAccountId = "68c53ef5-576d-45ec-c12a-26e185683dec";
        //    createLiveEventContent.MetaData = new Dictionary<string, Dictionary<CultureInfo, string>>
        //    {
        //        {
        //            "description", new Dictionary<CultureInfo, string>
        //            {
        //                {CultureInfo.CurrentUICulture,"Live event testing for conax"}
        //            }
        //        }
        //    };

        //    createLiveEventContent.HandledMessages.Add(new Guid());
        //    _liveEventModel = createLiveEventContent;
        //    createA_LiveEventinMPP();
        //}
        public void createA_LiveEventinMPP()
        {
            var apiBaseUrl = _Mpp5Config.RestLiveApiUrl;
            // Console.WriteLine(apiBaseUrl);
            var resource = "/" + _mpp5IdentityModel.HolderId + "/live-events/";
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string signature = gs.Build(HttpMethodType.POST, resource);

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, apiBaseUrl + resource);
                request.Headers.Add("Authorization", "MAC " + signature);
                var ms = new MemoryStream();
                var sw = new StreamWriter(ms);
                var ser = JsonSerializer.Create(new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    ContractResolver = new UnderscoreMappingResolver()
                });
                ser.Serialize(sw, _liveEventModel);
                sw.Flush();
                ms.Position = 0;
                request.Content = new StreamContent(ms);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                try
                {
                    HttpResponseMessage response = client.SendAsync(request).Result;
                    Console.WriteLine(response.StatusCode);

                    if (response.IsSuccessStatusCode)
                    {
                        //var stream = response.Content.ReadAsStreamAsync().Result;
                        Console.WriteLine("Successful live event created." + response.StatusCode);
                    }
                    else
                    {
                        Console.WriteLine("Failed to create live event.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.InnerException);
                }

            }

            Thread.Sleep(10000);
            getALiveEvent(_liveEventModel.Id.ToString());
        }
        public void getALiveEvent(string Id)
        {
            var apiBaseUrl = _Mpp5Config.RestLiveApiUrl;
            string id = Id;
            var resource = "/" + _mpp5IdentityModel.HolderId + "/live-events/" + id;
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string AuthenticationMac = gs.Build(HttpMethodType.GET, resource);
            string url = apiBaseUrl + resource;
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            //webReq.ContentType = "application/xml";
            webReq.Headers.Add("Authorization", "MAC " + AuthenticationMac);
            webReq.Method = "GET";
            webReq.ContentType = "application/json";
            byte[] byteData = Encoding.ASCII.GetBytes("");
            webReq.ContentLength = byteData.Length;

            using (var client = new HttpClient())
            {
                Console.WriteLine(url);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", "MAC " + AuthenticationMac);
                // Start request
                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                }
            }
        }
        public void getAllLive()
        {
            var apiBaseUrl = _Mpp5Config.RestLiveApiUrl;
            var resource = "/" + _mpp5IdentityModel.HolderId + "/live-events";
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string AuthenticationMac = gs.Build(HttpMethodType.GET, resource);
            using (var client = new HttpClient())
            {
                string url = apiBaseUrl + resource;
                Console.WriteLine(url);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", "MAC " + AuthenticationMac);
                // Start request
                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                }
            }
        }
    }
    public class UnderscoreMappingResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                propertyName, @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4").ToLower();
        }
    }
}
