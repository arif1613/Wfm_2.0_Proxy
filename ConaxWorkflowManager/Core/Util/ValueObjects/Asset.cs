using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class Asset
    {
        public Asset() {
            Properties = new List<Property>();
        }

        public UInt64? ObjectID { get; set; }
        public String Name { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public UInt64 FileSize { get; set; }
        public String Codec { get; set; }
        public UInt32 Bitrate { get; set; }
        public Boolean IsTrailer { get; set; }
        public String LanguageISO { get; set; }
        public String StreamPublishingPoint { get; set; }
        //**//public String contentAssetServerName { get; set; }
        public List<Property> Properties { get; set; }
        public Guid Mpp5_Asset_Id { get; set; }

        public void SetProperty(String name, String value)
        {
            Property property = Properties.FirstOrDefault<Property>(p => p.Type.Equals(name));
            if (property == null)
            {
                property = new Property() { Type = name, Value = value };
                Properties.Add(property);
            }
            else
            {
                property.Value = value;
            }

        }
    }
}
