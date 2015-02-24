using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums
{
    public class VODnLiveContentProperties : SystemContentProperties
    {

        #region Common

        public const String IngestIdentifier = "IngestIdentifier";   // ingest GUID.
        public const String MetadataMappingConfigurationFileName = "MetadataMappingConfigurationFileName";          // name for the metadatamapping file
        public const String IngestXMLFileName = "IngestXMLFileName";    // path of the ingest file.        
        public const String EnableQA = "EnableQA";              // if this content needs QA with ingest.

        #endregion

        #region Live Channel

        public const String CubiChannelId = "CubiChannelId";    // Channel ID in cubi
        public const String LCN = "LCN";                        // LCN number for this channel, used in cubi.
        public const String CubiCatchUpId = "CubiCatchUpId";    // catchup id in cubi.
        public const String EnableCatchUp = "EnableCatchUp";    // This channel is Enabled for Catchup
        public const String EnableNPVR = "EnableNPVR";          // This channel is Enabled for NPVR
        public const String UUID = "UUID";
        public const String RadioChannel = "RadioChannel";      // is radion channel
        public const String IsAdult = "IsAdult";

        // these are at asset leevel.
        public const String HighDefinition = "HighDefinition";  //  is HighDefinition, this is ofr asset proerpties, we might want to make an ew enums for asset properties later on.
        //public const String StreamType = "StreamType";          // stream type
        
        #endregion
    }
}
