using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums
{
    public class SystemContentProperties
    {
        #region System

        public const String ContentType = "ContentType";                        // Type of the content.
        public const String ConaxContegoContentID = "ConaxContegoContentID";    // Reference id in Conax contego 
        public const String ServiceExtContentID = "ServiceExtContentID";        // Reference id in External system, like cubi.
        public const String PublishedToService = "PublishedToService";          // flags is the content is published to service.

        public const String URIProfile = "URIProfile";      // DRM profile to use.        
        public const String DeviceType = "DeviceType";      // Device type for asset
        public const String AssetType = "AssetType";        // type of the asset

        //public const String CallSuccess = "CallSuccess";     

        #endregion

        #region MetaData

        public const String Genre = "Genre";
        public const String Producer = "Producer";
        public const String Cast = "Cast";
        public const String Director = "Director";
        public const String ScreenPlay = "ScreenPlay";
        public const String ReleaseDate = "ReleaseDate";
        public const String Category = "Category";
        public const String EpisodeName = "EpisodeName";
        public const String Country = "Country";
        public const String MovieRating = "MovieRating";
        public const String TVRating = "TVRating";

        #endregion

        public const String NPVRAssetData = "NPVRAssetData";
        public const String CatchupAssetData = "CatchupAssetData";

    }
}
