using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Harmonic;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Carbon;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.Harmonic
{
    public class FakeHarmonicOriginWrapper : IHarmonicOriginWrapper
    {
        public CallStatus DeleteSmoothContent(ContentData content, Asset assetToDelete)
        {
            String resource = "llcu/ss/asset";


            //CallStatus status = restApi.MakeDeleteCall(resource, assetToDelete.Name);

            
            CallStatus status = new CallStatus();
            status.Success = true;

            return status;
        }

        public CallStatus RecordSmoothContent(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime startTime, DateTime endTime)
        {
            EPGChannel channel = CatchupHelper.GetEPGChannel(content);
            String SSStreamName = CarbonEncoderHelper.GetStreamName(channel.ServiceEPGConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType).Stream);

            String resource = "llcu/ss/asset";            

            String xml = "";
            xml += "<Content xmlns=\"urn:eventis:cpi:1.0\">";
            xml += "<SourceUri>" + SecurityElement.Escape(SSStreamName) + "</SourceUri>";
            xml += "<IngestStartTime>" + startTime.ToString("yyyy-MM-ddTHH:mm:ssZ") + "</IngestStartTime>";
            xml += "<IngestEndTime>" + endTime.ToString("yyyy-MM-ddTHH:mm:ssZ") + "</IngestEndTime>";
            xml += "</Content>";
            XmlDataDocument doc = new XmlDataDocument();
            doc.LoadXml(xml);

            var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, serviceViewLanugageISO, deviceType, AssetType.NPVR);

//            String assetName = content.Assets.
            //CallStatus status = restApi.MakeUpdateCall(resource, asset.Name, doc);
            CallStatus status = new CallStatus();
            status.Success = true;

            return status;
        }

        public void GetAllSmoothNPVRAssets()
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.HarmonicOrigin.OriginVODAsset GetAssetData(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content, ConaxWorkflowManager.Core.Util.ValueObjects.Asset asset)
        {
            throw new NotImplementedException();
        }

        public bool DeleteAssetsFromOrigin(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public bool SetAdminState(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content, ConaxWorkflowManager.Core.Util.ValueObjects.HarmonicOrigin.OriginState state, bool trailer)
        {
            throw new NotImplementedException();
        }

        public string GetOriginAssetID(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content, bool trailer)
        {
            throw new NotImplementedException();
        }
    }
}
