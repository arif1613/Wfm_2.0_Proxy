using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.JobXmlConfig;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.JobXmlFile;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration
{
    public class CreateMpp5Content
    {
        private static BrokeredMessage _brokeredMessage;
        private static DateTime _dt;
        private static Mpp5Configuration _Mpp5Config;
        private static Mpp5IdentityModel _mpp5IdentityModel { get; set; }
        private static ContentData _contentData { get; set; }
        private static VodModel _vodModel { get; set; }
        private static LiveEventModel _liveEventModel { get; set; }
        private static bool vodCreationStatus { get; set; }
        private static IngestConfig _ingestConfig { get; set; }
        private readonly ConaxWorkflowManagerConfig _systemConfig;

        //VODs

        public CreateMpp5Content()
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
        public CreateMpp5Content(BrokeredMessage br, DateTime dt)
        {
            string filename = br.Properties["FileName"].ToString();
            var fi = new FileInfo(filename);
            _brokeredMessage = br;
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
            var createContegoVoDmsg = new CreateContegoVODmsg(filename);
            var systemConfig =
              (ConaxWorkflowManagerConfig)
                  Config.GetConfig()
                      .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            string workDirectory = _systemConfig.FileIngestWorkDirectory;
            string folderSettingsFile = Path.Combine(workDirectory, fi.Directory.Name, _systemConfig.FolderSettingsFileName);
            _ingestConfig = createContegoVoDmsg.GetIngestConfig(new FileInfo(folderSettingsFile));
            _contentData = createContegoVoDmsg.GetContentData();
            CreateMppChannelContent();
        }
        public CreateMpp5Content(ContentData cd, BrokeredMessage br)
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
            _brokeredMessage = br;
            _contentData = cd;
            CreateMppVod();
        }
        public void CreateMppVod()
        {
            var createVodModel = new VodModel();
            createVodModel.ActiveDirectoryRestricted = false;
            createVodModel.Name = _contentData.Name + ": " + DateTime.UtcNow.ToString("d/M/yyyy HH:mm:ss");
            createVodModel.Id = new Guid(_contentData.Mpp5_Id);
            
            if (_contentData.PublishInfos.Any())
            {
                createVodModel.Status = _contentData.PublishInfos.FirstOrDefault().PublishState.ToString();
            }
            else
            {
                createVodModel.Status = "Created";
            }

            createVodModel.CreatedDate = Instant.FromDateTimeUtc(DateTime.UtcNow);
            createVodModel.Deleted = false;
            createVodModel.HandledMessages = new List<Guid>()
            {
                new Guid(_brokeredMessage.MessageId),
                new Guid(_brokeredMessage.CorrelationId)
            };

            createVodModel.ImagePath = null;
            createVodModel.LastChangeTime = Instant.FromDateTimeUtc(DateTime.UtcNow);

            #region content property

            createVodModel.MetaData = new Dictionary<string, Dictionary<CultureInfo, string>>
            {
             
                {
                    "EventPeriodTo", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, _contentData.EventPeriodTo.ToString()}
                    }
                },
                {
                    "EventPeriodFrom", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, _contentData.EventPeriodFrom.ToString()}
                    }
                },
                {
                    "ProductionYear", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, _contentData.ProductionYear.ToString()}
                    }
                },
                {
                    "RunningTime", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, _contentData.RunningTime.ToString()}
                    }
                },
                {
                    "CreationDate", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture,_brokeredMessage.Properties["TimeStamp"].ToString()}
                    }
                },
                {
                    "TemporaryUnavailable", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, _contentData.TemporaryUnavailable.ToString()}
                    }
                },
                 {
                    "ContentRightsOwner", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture, _contentData.ContentRightsOwner.Name}
                    }
                }
            };

            #endregion

            #region Language info property
            var tempDictionary = new Dictionary<string, Dictionary<CultureInfo, string>>();

            foreach (Property property in _contentData.Properties)
            {
                tempDictionary.Add(property.Type, new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture,property.Value}
                    });
            }

            createVodModel.MetaData = createVodModel.MetaData.Concat(tempDictionary).ToDictionary(x => x.Key, x => x.Value);

            var lanInfoDic = new Dictionary<string, Dictionary<CultureInfo, string>>();
            
            for (int i = 0; i < _contentData.LanguageInfos.Count; i++)
            {
                lanInfoDic = new Dictionary<string, Dictionary<CultureInfo, string>>
                {
                    {
                        string.Format("Language_ISO[{0}]", i), new Dictionary<CultureInfo, string>
                        {
                            {CultureInfo.CurrentUICulture, _contentData.LanguageInfos[i].ISO}
                        }
                    },
                    {
                        string.Format("Language_Title[{0}]", i), new Dictionary<CultureInfo, string>
                        {
                            {CultureInfo.CurrentUICulture, _contentData.LanguageInfos[i].Title}
                        }
                    },
                    {
                        string.Format("Language_SortName[{0}]", i), new Dictionary<CultureInfo, string>
                        {
                            {CultureInfo.CurrentUICulture, _contentData.LanguageInfos[i].SortName}
                        }
                    },
                    {
                        string.Format("Language_ShortDescription[{0}]", i), new Dictionary<CultureInfo, string>
                        {
                            {CultureInfo.CurrentUICulture, _contentData.LanguageInfos[i].ShortDescription}
                        }
                    },
                    {
                        string.Format("Language_LongDescription[{0}]", i), new Dictionary<CultureInfo, string>
                        {
                            {CultureInfo.CurrentUICulture, _contentData.LanguageInfos[i].LongDescription}
                        }
                    },
                    {
                        string.Format("Language_SubtitleUrl[{0}]", i), new Dictionary<CultureInfo, string>
                        {
                            {CultureInfo.CurrentUICulture, _contentData.LanguageInfos[i].SubtitleURL}
                        }
                    }
                };
            }

            #endregion


            createVodModel.MetaData=createVodModel.MetaData.Concat(lanInfoDic).ToDictionary(x => x.Key, x => x.Value);

            foreach (var v1 in _contentData.Assets.Select(r=>r.Name).Distinct())
            {
                var v = _contentData.Assets.Where(r => r.Name == v1).FirstOrDefault();
                var createEncoderJobXml = new CreateEncoderJobXml(v, _contentData);
                XmlDocument xd = createEncoderJobXml.GetEncoderJobXmlDocument();
                GetAssetOutputName getAssetOutputName=new GetAssetOutputName(xd,v.Name);
                List<string> assetUrls = getAssetOutputName.GetAssetListForMPP();

                foreach (var x in assetUrls)
                {
                    createVodModel.UploadAssets.Add(Guid.NewGuid(),x);
                }
            }
            _vodModel = createVodModel;
        }
        public bool createA_VodinMPP()
        {
            var apiBaseUrl = _Mpp5Config.RestApiUrl;
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
                ser.Serialize(sw, _vodModel);
                sw.Flush();
                ms.Position = 0;
                request.Content = new StreamContent(ms);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    vodCreationStatus = true;
                }
                else
                {
                    vodCreationStatus = false;
                }
            }
            return vodCreationStatus;
        }
        public bool CheckIfVodIsCreated(string vodID)
        {
            var apiBaseUrl = _Mpp5Config.RestApiUrl;
            string id = vodID;
            var resource = "/" + _mpp5IdentityModel.HolderId + "/vods/" + id;
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string signature = gs.Build(HttpMethodType.GET, resource);

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiBaseUrl + resource);
                request.Headers.Add("Authorization", "MAC " + signature);
                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {

                    return true;

                }
                return false;
            }
        }
        public void DeleteAVod(string vodid)
        {
            var apiBaseUrl = _Mpp5Config.RestApiUrl;
            var resource = "/" + _mpp5IdentityModel.HolderId + "/vods/"+vodid;
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string signature = gs.Build(HttpMethodType.DELETE, resource);

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, apiBaseUrl + resource);
                request.Headers.Add("Authorization", "MAC " + signature);
          HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    vodCreationStatus = true;
                    Console.WriteLine(vodCreationStatus);
                }
                else
                {
                    vodCreationStatus = false;
                    Console.WriteLine(vodCreationStatus);

                }
            }

        }
        public void AddAssetsInMpp5(AddVodAssetModel addVodAssetModel,string vodId)
        {
            var apiBaseUrl = _Mpp5Config.RestApiUrl;
            // Console.WriteLine(apiBaseUrl);

            var resource = "/" + addVodAssetModel.OwnerId + "/vods/" + vodId + "/addVodAsset";
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
                ser.Serialize(sw, addVodAssetModel);
                sw.Flush();
                ms.Position = 0;
                request.Content = new StreamContent(ms);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {

                    Console.WriteLine(response.IsSuccessStatusCode.ToString());
                }
                else
                {
                    Console.WriteLine(response.IsSuccessStatusCode.ToString());
                }
            }
        }
        public void PublishAssetInMpp5(string vodId,string assetID)
        {
            var apiBaseUrl = _Mpp5Config.RestApiUrl;
            var resource = "/" + _mpp5IdentityModel.HolderId + "/vods/" + vodId + "/assets/"+assetID+"/publish";
            var gs = new GenerateSignature(_mpp5IdentityModel);
            string signature = gs.Build(HttpMethodType.POST, resource);
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, apiBaseUrl + resource);
                request.Headers.Add("Authorization", "MAC " + signature);
                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine(response.IsSuccessStatusCode.ToString());
                }
                else
                {
                    Console.WriteLine(response.IsSuccessStatusCode.ToString());
                }
            }
        }
        
        
        //Channels
        
        public void CreateMppChannelContent()
        {
            var createLiveEventContent = new LiveEventModel();
            createLiveEventContent.Id = new Guid(_contentData.Mpp5_Id);
            createLiveEventContent.ActiveDirectoryRestricted = false;
            createLiveEventContent.Name = _contentData.Name +": "+ DateTime.UtcNow.ToString("d/M/yyyy HH:mm:ss");

            if (!string.IsNullOrEmpty(_contentData.EventPeriodFrom.ToString()))
            {
                createLiveEventContent.StartTime = _contentData.EventPeriodFrom;
            }
            else
            {
                var ts=new TimeSpan(0,30,0);
                createLiveEventContent.StartTime = DateTime.UtcNow.ToLocalTime()+ts;
            }
            if (!string.IsNullOrEmpty(_contentData.EventPeriodTo.ToString()))
            {
                createLiveEventContent.EndTime = _contentData.EventPeriodTo;
            }
            else
            {
                var ts = new TimeSpan(2, 30, 0);
                createLiveEventContent.EndTime = DateTime.UtcNow.ToLocalTime()+ts;
            }
            createLiveEventContent.AesEncryptionEnabled = false;
            createLiveEventContent.CreatedUser = _mpp5IdentityModel.UserName;
            createLiveEventContent.HiveAccelerated = false;
            //createLiveEventContent.InputIpRangeList.Add(new IpRangeInfo
            //{
            //    Address = "168.0.0.1",
            //    Name = "mps",
            //    SubnetPrefixLength = 1
            //});

            //createLiveEventContent.PreviewIpRangeList.Add(new IpRangeInfo
            //{
            //    Address = "168.0.0.2",
            //    Name = "preview",
            //    SubnetPrefixLength = 2
            //});

            createLiveEventContent.ProvisioningTime = createLiveEventContent.StartTime - new TimeSpan(0, 10, 0);
            createLiveEventContent.WamsAccountId = new Guid("dd0bc9bd-3078-49cc-fdaf-baa97505dc5d");

       
            createLiveEventContent.MetaData = new Dictionary<string, Dictionary<CultureInfo, string>>
            {

                {
                    "ContentRightsOwner", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture,_contentData.ContentRightsOwner.Name.ToString()}
                    }
                },
                {
                    "ProductionYear", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture,_contentData.ProductionYear.ToString()}
                    }
                },
                {
                    "ContentAgreements", new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture,_contentData.ContentAgreements.FirstOrDefault().Name.ToString()}
                    }
                }
            };

            var tempDictionary = new Dictionary<string, Dictionary<CultureInfo, string>>();
          
                foreach (Property property in _contentData.Properties)
                {
                    tempDictionary.Add(property.Type, new Dictionary<CultureInfo, string>
                    {
                        {CultureInfo.CurrentUICulture,property.Value}
                    });
                }


            createLiveEventContent.MetaData=createLiveEventContent.MetaData.Concat(tempDictionary).ToDictionary(x => x.Key, x => x.Value);

            _liveEventModel = createLiveEventContent;
        }
        public void createA_LiveEventinMPP()
        {
            var apiBaseUrl = _Mpp5Config.RestLiveApiUrl;
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
                HttpResponseMessage response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    vodCreationStatus = true;
                    Console.WriteLine("Successful Live event created for id: " + _liveEventModel.Id);
                }
                else
                {
                    Console.WriteLine("Failed to create live event. ");
                    vodCreationStatus = false;
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
