using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Unified
{
    public interface IUnifiedServicesWrapper
    {
        RecordResult RecordSmoothContent(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO,
                                         DeviceType deviceType, DateTime minStart, DateTime maxEnd);


        RecordResult CheckSmoothManifest(String url);


        RecordResult GetSmoothAssetStatus(String output);


        RecordResult DeleteSmoothAsset(ContentData content, Asset assetToDelete);

        void DeleteBuffer(String apiUrl);

    }
}
