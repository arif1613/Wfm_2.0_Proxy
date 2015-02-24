using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class ContentData
    {
        public ContentData() {
            PublishInfos = new List<PublishInfo>();
            Properties = new List<Property>();
            LanguageInfos = new List<LanguageInfo>();
            Assets = new List<Asset>();
            ContentAgreements = new List<ContentAgreement>();
        }

        public String Name { get; set; }
        public UInt64? ID { get; set; }
        public UInt64? ObjectID { get; set; }
        public String HostID { get; set; }
        public String ExternalID { get; set; }
        public DateTime? EventPeriodFrom { get; set; }
        public DateTime? EventPeriodTo { get; set; }
        public DateTime? Created { get; set; }
        public List<PublishInfo> PublishInfos { get; set; }
        public UInt32? ProductionYear { get; set; }
        public TimeSpan? RunningTime { get; set; }
        public Boolean? TemporaryUnavailable { get; set; }
        public List<Property> Properties { get; set; }
        public List<LanguageInfo> LanguageInfos { get; set; }
        public List<Asset> Assets { get; set; }
        public ContentRightsOwner ContentRightsOwner { get; set; }
        public List<ContentAgreement> ContentAgreements { get; set; }
        public string Mpp5_Id { get; set; }

        public String GetPropertyValue(String propertyName)
        {
            Property property = Properties.FirstOrDefault<Property>(p => p.Type.Equals(propertyName));
            String ret = "";
            if (property != null)
                ret = property.Value;
            return ret;
        }

        public String GetPropertyValueIgnoreCase(String propertyName)
        {
            Property property = Properties.FirstOrDefault<Property>(p => p.Type.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            String ret = "";
            if (property != null)
                ret = property.Value;
            return ret;
        }

        public List<String> GetPropertyValues(String propertyName)
        {
            List<String> ret = new List<string>();
            var properties = Properties.Where<Property>(p => p.Type.Equals(propertyName));

            foreach (Property property in properties)
            {
                if (!String.IsNullOrEmpty(property.Value))
                    ret.Add(property.Value);
            }
            return ret;
        }

        public void AddPropertyValue(String propertyName, String value)
        {
            Property property = new Property();
            property.Type = propertyName;
            property.Value = value;
            Properties.Add(property);
        }

        public void SlimDownAndExtraxtAssetInfoToProperty()
        {
            try
            {
                AssetDataExtractor assetDataExtractor = new AssetDataExtractor();

                List<Asset> npvrAssets = ConaxIntegrationHelper.GetAllNPVRAsset(this);
                if (npvrAssets.Count > 0) {
                    assetDataExtractor.Load(npvrAssets);
                    AddPropertyValue( SystemContentProperties.NPVRAssetData, assetDataExtractor.GetAllAssetDataAsString());
                }

                assetDataExtractor = new AssetDataExtractor();

                List<Asset> catchupAssets = ConaxIntegrationHelper.GetAllCatchupAsset(this);
                if (catchupAssets.Count > 0)
                {
                    assetDataExtractor.Load(catchupAssets);
                    AddPropertyValue(SystemContentProperties.CatchupAssetData, assetDataExtractor.GetAllAssetDataAsString());
                }
                Assets.Clear();
            }
            catch (Exception exc)
            {
                // will be saved nonslimmed
            }
        }

        internal void AddAssetsFromAssetDataProperty(String proeprtyType)
        {
            AssetDataExtractor extractor = new AssetDataExtractor();
            if (Assets == null)
                Assets = new List<Asset>(); 
            Assets.AddRange(extractor.GetAssetsFromAssetDataProperty(this, proeprtyType));
        }


    }
}
