using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums
{
    public enum WorkFlowType
    {
        NoAction,
        AddVODContent,
        UpdateVODContent,
        DeleteVODContent,
        PublishVODContent,
        UpdatePublishedVODContent,
        DeletePublishedVODContent,
        PublishVODContentToSeaChange,
        AddChannelContent,
        UpdateChannelContent,
        DeleteChannelContent,
        PublishChannelContent,
        AddLiveContent,
        UpdateLiveContent,
        DeleteLiveContent,
        PublishLiveContent,
        UpdateServicePrice,
        AddCatchUpContent,
        UpdatePublishedServicePrice,
    }
}
