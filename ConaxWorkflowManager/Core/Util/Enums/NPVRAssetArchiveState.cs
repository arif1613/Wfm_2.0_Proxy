

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums
{
    public enum NPVRAssetArchiveState
    {
        NotSpecified,
        Unknown,        // Initial state.
        Pending,        // user recordngs found but not ready to start recording yet 
        NotArchived,     // No user initiated a recording 
        Recording,      // The recording is in progress 
        Failed,         // failed to archive asset 
        Archived,       // Asset archived successfully 
        Purged          // Archived asset removed from origin 
    }
}
