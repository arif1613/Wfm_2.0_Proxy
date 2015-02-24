using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Repository;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    class AssetDataExtractor
    {
        private List<AssetData> assetDatas = new List<AssetData>(); 
        internal void Load(List<Asset> assets)
        {
            foreach (Asset asset in assets)
            {
                AssetData assetData = AssetData.Parse(asset);
                assetDatas.Add(assetData);
            }
        }

        internal String GetAllAssetDataAsString()
        {
            String assetDataString = "";
            foreach (AssetData assetData in assetDatas)
            {
                assetDataString += assetData.AssetName + ":" + assetData.AssetFormatType + ":" + assetData.DeviceType + ":" + assetData.Lang + ";";
            }
            return assetDataString;
        }

        internal List<Asset> GetAssetsFromAssetDataProperty(ContentData content, String proeprtyType)
        {
            Property assetDataProperty =
                content.Properties.SingleOrDefault(
                    p => p.Type.Equals(proeprtyType, StringComparison.OrdinalIgnoreCase));

            AssetType assetType = AssetType.Catchup;
            if (CatchupContentProperties.NPVRAssetData == proeprtyType)
                assetType = AssetType.NPVR;

            foreach (String assetDataString in assetDataProperty.Value.Split(';'))
            {
                assetDatas.Add(AssetData.FromString(assetDataString, assetType));
            }
            return CreateAssets();
        }

        private List<Asset> CreateAssets()
        {
            List<Asset> assets = new List<Asset>();
            foreach (AssetData assetData in assetDatas)
            {
                Asset asset = new Asset();
                asset.Name = assetData.AssetName;
                asset.SetProperty(CatchupContentProperties.AssetFormatType, assetData.AssetFormatType);
                asset.SetProperty(CatchupContentProperties.DeviceType, assetData.DeviceType);
                asset.SetProperty(CatchupContentProperties.AssetType, assetData.AssetType.ToString());
                asset.LanguageISO = assetData.Lang;
                assets.Add(asset);
            }
            return assets;
        }
    }
}
