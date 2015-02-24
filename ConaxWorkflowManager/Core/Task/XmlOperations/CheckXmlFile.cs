using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.XmlOperations
{
    public class CheckXmlFile
    {
        private static FileInfo _xmlFileInfo;

        public CheckXmlFile(FileInfo xmlfileinfo)
        {
            _xmlFileInfo = xmlfileinfo;
        }

        public List<string> totalAssestsinXml()
        {
            IngestXMLType ingestXmlType = CommonUtil.GetIngestXMLType(_xmlFileInfo.FullName);
            var validateMediaInfo=new ValidateMediaInfo(_xmlFileInfo);
            var assetnames=new List<string>();
            
            if (ingestXmlType.ToString()== "Channel_1_0")
            {
                assetnames = validateMediaInfo.ChannelMediaFiles();
            }
            else
            {
                assetnames = validateMediaInfo.GetVodMediaFilesInUploadfolder();
            }           
            return assetnames;
        }
    }
}
