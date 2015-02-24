using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    class AssetData
    {

        public String AssetName { get; set; }

        public String DeviceType { get; set; }

        public String Lang { get; set; }

        public String AssetFormatType { get; set; }

        public AssetType AssetType { get; set; }

        internal static AssetData Parse(Asset asset)
        {
            AssetData assetData = new AssetData();
            assetData.AssetName = asset.Name;
            Property assetFormatType =
                    asset.Properties.Single(p => p.Type.Equals(CatchupContentProperties.AssetFormatType, StringComparison.OrdinalIgnoreCase));
            assetData.AssetFormatType = assetFormatType.Value;
            Property deviceType =
                asset.Properties.Single(p => p.Type.Equals(CatchupContentProperties.DeviceType, StringComparison.OrdinalIgnoreCase));
            assetData.DeviceType = deviceType.Value;          

            Property assetType =
                asset.Properties.Single(p => p.Type.Equals(CatchupContentProperties.AssetType, StringComparison.OrdinalIgnoreCase));
            assetData.AssetType = (AssetType)Enum.Parse(typeof(AssetType), assetType.Value, true);

            assetData.Lang = asset.LanguageISO;

            return assetData;
        }

        internal static AssetData FromString(string assetDataString, AssetType assetType)
        {
           AssetData assetData = new AssetData();
            String[] datas = assetDataString.Split(':');
            if (datas.Count() < 3)
                return assetData;
            assetData.AssetName = datas[0];
            assetData.AssetFormatType = datas[1];
            assetData.DeviceType = datas[2];
            if (datas.Length > 3)
                assetData.Lang = datas[3];
            assetData.AssetType = assetType;
            return assetData;
        }
    }
}
