using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums
{
    public class CatchupContentProperties : SystemContentProperties
    {
        #region Catchup/NPVR

        public const String CubiEpgId = "CubiEpgId";            // Cubis EPG object Id, cubis representaiton of the EPG item
        public const String EpgId = "EpgId";                    // Channel ID in the EPG feed
        public const String ChannelId = "ChannelId";            // content id of the channel in MPP.
        public const String CubiChannelId = "CubiChannelId";    // Channel ID in cubi
        public const String EpgProgrammeId = "EpgProgrammeId";  // 
        public const String EpgMetadataHash = "EpgMetadataHash"; // Hash to compare epg objects

        public const String EpgIsSynked = "EpgIsSynked";    // Channel ID in cubi
        public const String NoOfEpgSynkRetries = "NoOfEpgSynkRetries";    //         

        public const String NPVRAssetArchiveState = "NPVRAssetArchiveState";            // sate for the NPVR asset archvie state.
        public const String AssetFormatType = "AssetFormatType";            // format type of the asset        

        public const String NPVRRecordingsstState = "NPVRRecordingsstState";            // NPVR recordings state in all servcies
        public const String ServiceNPVRRecordingsstState = "ServiceNPVRRecordingsstState";            // NPVR recordings state, in specific servcie

        public const String NPVRArchiveTimes = "NPVRArchiveTimes";                      // logs when generate npvr was executed.


        public const String EnableCatchUp = "EnableCatchUp";    // This content is Enabled for Catchup
        public const String EnableNPVR = "EnableNPVR";          // This content is Enabled for NPVR
        public const String FeedTimezone = "FeedTimezone";      // Time zone id for the EPG feed this content comes from.

        public const String CatchUpDevices = "CatchUpDevices";  // Right filter, available devices for this catchup content
        public const String CatchUpHours = "CatchUpHours";      // Right filter, available hours for this catchup content

        public const String EPGIEpisodeInformation = "EPGIEpisodeInformation";      // Episode Information, used for episode recording        

        public const String EPGHasRecordingInService = "EPGHasRecordingInService";      // Property for keeping track of if EPG have recordings in a service

        public const String ReadyToNPVRPurge = "ReadyToNPVRPurge";      // Property marks that the content is ready to be NPVR purge or not.

        public const String LastAttemptStateInService = "LastAttemptStateInService";      // sate for servcie call at last attempt.


        //public const String FailedToFetchRecordingsFromService = "FailedToFetchRecordingsFromService";
        public const String DoneGettingRecordingsFromTenants = "DoneGettingRecordingsFromTenants";

        public const String cxid = "cxid";

        #endregion

        #region Catchup/NPVR Asset

        public const String NPVRAssetStarttime = "NPVRAssetStarttime";                  // sets which start time assets archived with
        public const String NPVRAssetEndtime = "NPVRAssetEndtime";                      // sets which end time assets archived with

        #endregion

        #region Metadata

        public const String Channel = "Channel";  // Channel id from the EPG feed
        public const String Category = "Category";  // Category of the content.

        #endregion
    }
}
