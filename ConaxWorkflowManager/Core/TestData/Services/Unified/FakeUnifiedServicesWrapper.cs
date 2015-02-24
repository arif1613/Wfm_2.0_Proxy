using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.Unified
{
    public class FakeUnifiedServicesWrapper : IUnifiedServicesWrapper
    {
        public RecordResult RecordSmoothContent(ContentData content, ulong serviceObjId, string serviceViewLanugageISO, DeviceType deviceType, DateTime minStart, DateTime maxEnd)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "Unified").SingleOrDefault();
            String unifiedPlayerAPI = systemConfig.GetConfigParam("UnifiedPlayerAPI");

            String apiUrl = unifiedPlayerAPI;
           
            String url = "";
            try
            {
                DateTime dtFrom = minStart;
                DateTime dtTo = maxEnd;

                TimeSpan vbegin = UnifiedHelper.GetServerTimeStamp(dtFrom);
                TimeSpan vend = UnifiedHelper.GetServerTimeStamp(dtTo);

                EPGChannel epgChannel = CatchupHelper.GetEPGChannel(content);
                String stream = epgChannel.ServiceEPGConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType).Stream;
                Int32 pos = stream.IndexOf(".isml");
                url = stream.Substring(0, pos + 5) + "/Manifest?";
                //url += "http://storage01.lab.conax.com/content/live/nrk1/nrk1.isml/Manifest?";
                url += "vbegin=" + ((UInt64)vbegin.TotalSeconds).ToString() + "&";
                url += "vend=" + ((UInt64)vend.TotalSeconds).ToString();

                //String output = "0101-" + dtFrom.ToString("yyyyMMddHHmmss");
                var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType, AssetType.NPVR);
                String output = content.ID + "/" + asset.Name;
                
                NameValueCollection keyvaluepairs = new NameValueCollection();
                keyvaluepairs.Add("url", url);
                keyvaluepairs.Add("output", output);
                
                RecordResult res = new RecordResult();
                res.ReturnCode = 0;
                res.Message = "";
                return res;
            }
            catch (Exception ex)
            {
                RecordResult res = new RecordResult();
                res.ReturnCode = -1;
                res.Message = ex.Message;
                res.Exception = ex;
                return res;
            }
        }

        public ConaxWorkflowManager.Core.Communication.RecordResult CheckSmoothManifest(string url)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Communication.RecordResult GetSmoothAssetStatus(String output)
        {
            throw new NotImplementedException();
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
                apiUrl += content.ID + "/" + assetToDelete.Name;

                RecordResult res = new RecordResult();
                res.ReturnCode = 0;

                return res;
            }
            catch (Exception ex)
            {
                
                RecordResult res = new RecordResult();
                res.ReturnCode = -1;
                res.Message = ex.Message;
                res.Exception = ex;
                return res;
            }
        }

        public void DeleteBuffer(string apiUrl)
        {
            throw new NotImplementedException();
        }
    }
}
