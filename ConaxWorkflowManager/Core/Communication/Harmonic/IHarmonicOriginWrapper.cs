using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.HarmonicOrigin;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Harmonic
{
    public interface IHarmonicOriginWrapper
    {

        CallStatus DeleteSmoothContent(ContentData content, Asset assetToDelete);

        CallStatus RecordSmoothContent(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType,
                                       DateTime startTime, DateTime endTime);


        void GetAllSmoothNPVRAssets();

        OriginVODAsset GetAssetData(ContentData content, Asset asset);


        bool DeleteAssetsFromOrigin(ContentData content);

        bool SetAdminState(ContentData content, OriginState state, bool trailer);


        String GetOriginAssetID(ContentData content, bool trailer);
              
    }
}
