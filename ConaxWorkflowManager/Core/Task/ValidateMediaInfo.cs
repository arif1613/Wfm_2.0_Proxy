using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using WFMProxy.Controllers;
using WFMProxy.Models;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class ValidateMediaInfo
    {
        private readonly FileInfo _xmlFileInfo;

        public ValidateMediaInfo(FileInfo xmlFileInfo)
        {
            _xmlFileInfo = xmlFileInfo;
        }
        public bool checkVodAsset(string msgType)
        {
            var assetlist = new List<string>();
            if (msgType == "File Watch Task")
            {
                assetlist = GetVodMediaFilesInUploadfolder();
            }
            else
            {
                assetlist = GetMediaFiles();

            }

            if (assetlist.Any())
            {
                var finaList = new List<string>();
                foreach (var s in assetlist)
                {
                    if (File.Exists(s))
                    {
                        finaList.Add(s);
                    }
                }
                if (finaList.Count() == assetlist.Count())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public bool checkChannelAsset()
        {
            var assetlist = ChannelMediaFiles();
            if (assetlist.Any())
            {
                var finaList = new List<string>();
                foreach (var s in assetlist)
                {
                    if (File.Exists(s))
                    {
                        finaList.Add(s);
                    }
                }
                if (finaList.Count() == assetlist.Count())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public List<string> GetMediaFiles()
        {
            var rm = new ReadMediaInfo(_xmlFileInfo.FullName);
            List<MediaInfos> mediaInfoses = rm.Getmediainfos();
            var encoderConfig =
                Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ElementalEncoder").SingleOrDefault();
            string encoderUploadDirectory = encoderConfig.GetConfigParam("EncoderUploadFolder");
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);

            var fileNames = new List<string>();

            foreach (var p in mediaInfoses)
            {
                if (p.FileType.Equals("movie", StringComparison.OrdinalIgnoreCase) || p.FileType.Equals("preview", StringComparison.OrdinalIgnoreCase))
                {
                    fileNames.Add(Path.Combine(encoderUploadDirectory, _xmlFileInfo.Directory.Name, p.FileName));
                }
                else
                {
                    if (!string.IsNullOrEmpty(p.FileName))
                    {
                        fileNames.Add(Path.Combine(_xmlFileInfo.DirectoryName, p.FileName));
                    }
                    else
                    {
                        string defaultimagefilename = systemConfig.DefaultVodCoverImageFileName;
                        fileNames.Add(Path.Combine(_xmlFileInfo.DirectoryName, defaultimagefilename));
                    }
                }
            }
            return fileNames;
        }
        public List<string> GetVodMediaFilesInUploadfolder()
        {
            var rm = new ReadMediaInfo(_xmlFileInfo.FullName);
            List<MediaInfos> mediaInfoses = rm.Getmediainfos();
            var fileNames = new List<string>();

            foreach (var p in mediaInfoses)
            {
                fileNames.Add(Path.Combine(_xmlFileInfo.DirectoryName, p.FileName));
            }
            //check if files exist
            return fileNames;
        }
        public List<string> ChannelMediaFiles()
        {
            XmlDocument xd = new XmlDocument();
            xd.Load(_xmlFileInfo.FullName);
            string directoryName = _xmlFileInfo.DirectoryName;
            var fileNames = new List<string>();
            var xmlReader = new XmlTextReader(_xmlFileInfo.FullName);
            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == "BoxCover")
                        {
                            fileNames.Add(Path.Combine(directoryName, xmlReader.ReadInnerXml()));
                        }
                        break;
                }
            }
            if (fileNames.Any())
            {
                return fileNames;
            }
            else
            {
                fileNames.Add(Path.Combine(directoryName, "default.jpg"));
                return fileNames;
            }
        }
    }
}
