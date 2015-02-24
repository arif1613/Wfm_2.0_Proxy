using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.XmlOperation
{
    class MppProfileXmlTranslatorVOD
    {
        private static string _XmlFileName { get; set; }
        private static XmlDocument _xmlDocument;

        public MppProfileXmlTranslatorVOD(string xmlFilename)
        {
            _XmlFileName = xmlFilename;
            _xmlDocument=new XmlDocument();
            _xmlDocument.Load(xmlFilename);
        }

        public string ExternalId()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Identification");
            if (elementName != null)
            {
                return elementName.GetAttribute(string.Format("externalId", CultureInfo.InvariantCulture));
            }
            else
            {
                return null;
            }
        }

        public string VodName()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Identification");
            if (elementName != null)
            {
                return elementName.GetAttribute(string.Format("name", CultureInfo.InvariantCulture));
            }
            else
            {
                return null;
            }
        }
        public string EventPeriodTo()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/EventPeriod");
            if (elementName != null)
            {
                return elementName.GetAttribute(string.Format("to", CultureInfo.InvariantCulture));
            }
            else
            {
                return null;
            }
        }
        public string EventPeriodFrom()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/EventPeriod");
            if (elementName != null)
            {
                return elementName.GetAttribute(string.Format("from", CultureInfo.InvariantCulture));
            }
            else
            {
                return null;
            }
        }
        public string ProductionYear()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/ProductionYear");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string RunningTime()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/RunningTime");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string IngestXMLFileName()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='IngestXMLFileName']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string EnableQA()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='EnableQA']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string IngestSource()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='IngestSource']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return "CableLabs";
            }
        }
        public string URIProfile()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='URIProfile']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string EpisodeName()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='EpisodeName']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string Country()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='Country']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string Cast()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='Cast']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string Director()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='Director']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string Producer()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='Producer']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string Category()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='Category']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string Genre()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='Genre']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string ContentType()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='ContentType']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string MovieRating()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='MovieRating']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string ConaxContegoContentID()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/Property[@type='ConaxContegoContentID']");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string PublishState()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/PublishState");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string IngestIdentifier()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/IngestIdentifier");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public Dictionary<string,string> LanguageInfo()
        {
            Dictionary<string, string> DicLanguageIfos = new Dictionary<string,string>();
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/LanguageInfo");
            DicLanguageIfos.Add("ISO", elementName != null ? elementName.GetAttribute("ISO") : string.Empty);


            var Title = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/LanguageInfo/Title");
            DicLanguageIfos.Add("Title", Title != null ? Title.InnerXml : string.Empty);

            var SortName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/LanguageInfo/SortName");
            DicLanguageIfos.Add("SortName", SortName != null ? SortName.InnerXml : string.Empty);

            var ShortDescription = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/LanguageInfo/ShortDescription");
            DicLanguageIfos.Add("ShortDescription", ShortDescription != null ? ShortDescription.InnerXml : string.Empty);

            var LongDescription = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/LanguageInfo/LongDescription");
            DicLanguageIfos.Add("LongDescription", LongDescription != null ? LongDescription.InnerXml : string.Empty);

            var ImageClassification = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/LanguageInfo/Image");
            DicLanguageIfos.Add("ImageClassification", ImageClassification != null ? ImageClassification.GetAttribute("Classification") : string.Empty);

            var ImageClientGUIname = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/LanguageInfo/Image");
            DicLanguageIfos.Add("ImageClientGUIname", ImageClientGUIname != null ? ImageClientGUIname.GetAttribute("ClientGUIName") : string.Empty);

            var ImageFileName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/Metadata/LanguageInfo/Image");
            DicLanguageIfos.Add("ImageFileName", ImageFileName != null ? ImageFileName.InnerXml : string.Empty);

            return DicLanguageIfos;
        }
        public List<VideoAsset> VideoAssetList()
        {
            var videoAssetsList=new List<VideoAsset>();
            var videoAsset=new VideoAsset();

            foreach (XmlElement adNode in _xmlDocument.SelectSingleNode("MediaContent/Assets"))
            {
                XmlElement typeNode = (XmlElement)adNode.SelectSingleNode("VideoAsset");
                if (typeNode != null)
                {
                    videoAsset.Name = typeNode.GetAttribute("name");
                    if (typeNode.GetAttribute("trailer").Equals("true"))
                    {
                        videoAsset.IsTrailer = true;
                    }
                    else
                    {
                        videoAsset.IsTrailer = false;
                    }
                    videoAsset.FileSize = UInt64.Parse(typeNode.GetAttribute("filesize"));
                    videoAsset.Codec = typeNode.GetAttribute("codec");
                    videoAsset.StreamPublishingPoint = typeNode.GetAttribute("streamPublishingPoint");
                    videoAsset.Bitrate = UInt32.Parse(typeNode.GetAttribute("bitrate"));
                    var dm = typeNode.GetAttribute("deliveryMethod");
                    switch (dm)
                    {
                        case "Physical":
                            videoAsset.DeliveryMethod = DeliveryMethod.Physical;
                            break;
                        case "Stream":
                            videoAsset.DeliveryMethod = DeliveryMethod.Stream;
                            break;
                        case "Download":
                            videoAsset.DeliveryMethod = DeliveryMethod.Download;
                            break;
                        default:
                            videoAsset.DeliveryMethod = DeliveryMethod.NotSpecified;
                            break;
                    }
                    foreach (XmlElement propertyNode in (XmlElement)adNode.SelectSingleNode("Property"))
                    {
                        string propertyType = propertyNode.GetAttribute("type");
                        string propertyValue = propertyNode.InnerXml;
                        Property property = new Property();
                        property.Type = propertyType;
                        property.Value = propertyValue;
                        videoAsset.Properties.Add(property);
                    }
                    videoAssetsList.Add(videoAsset);
                }
            }
            return videoAssetsList;
        }
        public string MppContentHostID()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/MPPContentContext");
            if (elementName != null)
            {
                return elementName.GetAttribute(string.Format("hostId", CultureInfo.InvariantCulture));
            }
            else
            {
                return null;
            }
        }
        public string ContentRightsOwner()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/MPPContentContext/ContentRightsOwner");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
        public string ContentAgreement()
        {
            var elementName = (XmlElement)_xmlDocument.SelectSingleNode("MediaContent/MPPContentContext/ContentAgreement");
            if (elementName != null)
            {
                return elementName.InnerXml;
            }
            else
            {
                return null;
            }
        }
    }
    public class VideoAsset
    {
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
    }
}
