using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using File = System.IO.File;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.FileOperations
{
    public class FileMover
    {
        private static FileInfo _sourceFileInfo;
        private static string _destinationConfigFolder;
        private static FileInfo _newfileInfo { get; set; }

        public FileMover(string DestinationconfigFolder)
        {
            _destinationConfigFolder = DestinationconfigFolder;
        }
        public FileMover(string sourceFilePath, string DestinationconfigFolder)
        {
            if (!string.IsNullOrEmpty(sourceFilePath))
            {
                var sourcefileinfo = new FileInfo(sourceFilePath);
                _destinationConfigFolder = DestinationconfigFolder;
                _sourceFileInfo = sourcefileinfo;
            }
            try
            {
                CreateDir();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException);
            }
        }

        public void CreateDir()
        {
            string dirname = null;

            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            var encoderConfig =Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ElementalEncoder).SingleOrDefault();
            if (_destinationConfigFolder == "work")
            {
                dirname = Path.Combine(systemConfig.FileIngestWorkDirectory, _sourceFileInfo.Directory.Name);
                if (!Directory.Exists(dirname))
                {
                    Directory.CreateDirectory(dirname);
                }
                CreateFile(Path.Combine(dirname, _sourceFileInfo.Name));
            }
            if (_destinationConfigFolder == "reject")
            {
                dirname = Path.Combine(systemConfig.FileIngestRejectDirectory, _sourceFileInfo.Directory.Name);
                if (!Directory.Exists(dirname))
                {
                    Directory.CreateDirectory(dirname);
                }
                CreateFile(Path.Combine(dirname, _sourceFileInfo.Name));
            }
            if (_destinationConfigFolder == "encoderUpload")
            {
                dirname = Path.Combine(encoderConfig.GetConfigParam("EncoderUploadFolder"), _sourceFileInfo.Directory.Name);
                if (!Directory.Exists(dirname))
                {
                    Directory.CreateDirectory(dirname);
                }
                CreateFileInEncoderFolder(Path.Combine(dirname, _sourceFileInfo.Name));
            }
          
        }

        private void CreateFileInEncoderFolder(string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            _sourceFileInfo.MoveTo(filename);
            _newfileInfo = new FileInfo(filename);
        }

        public void CreateFile(string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            if (_sourceFileInfo.Extension.Equals(".xml",StringComparison.OrdinalIgnoreCase))
            {
                IngestXMLType ingestXmlType = CommonUtil.GetIngestXMLType(_sourceFileInfo.FullName);
                var ingestXmlConfig = Config.GetConfig().IngestXMLConfigs.SingleOrDefault(i => i.IngestXMLType.Equals(ingestXmlType.ToString(),
                                        StringComparison.OrdinalIgnoreCase));

                if (ingestXmlConfig.IngestXMLType!=null)
                {
                    if (ingestXmlConfig.IngestXMLType != "Channel_1_0")
                    {
                        addNewIdNodeinVod(new FileInfo(filename));
                    }
                    else
                    {
                        addNewIdNodeinChannel(new FileInfo(filename));
                    }
                    
                }
                else
                {
                    if (ingestXmlType.ToString() != "Channel_1_0")
                    {
                        addNewIdNodeinVod(new FileInfo(filename));
                    }
                    else
                    {
                        addNewIdNodeinChannel(new FileInfo(filename));
                    }
                    
                }
               
                _newfileInfo = new FileInfo(filename);
            }
            else
            {
                _sourceFileInfo.MoveTo(filename);
                _newfileInfo = new FileInfo(filename);
            }
            
        }



        private void addNewIdNodeinVod(FileInfo v)
        {
            XmlDocument originalXml = new XmlDocument();
            originalXml.Load(_sourceFileInfo.FullName);

            //adding VOD id node
            XmlNode adiNode = originalXml.SelectSingleNode("ADI");
            XmlNode mpp5IdNode = originalXml.CreateNode(XmlNodeType.Element, "MPP5_ID", null);
            mpp5IdNode.InnerXml = Guid.NewGuid().ToString();
            adiNode.AppendChild(mpp5IdNode);

            //adding mpp5 id for assets
            XmlNodeList assetNodeList = originalXml.SelectNodes("ADI/Asset/Asset/Metadata");

            foreach (XmlNode assetnode in assetNodeList)
            {
                XmlNode assetid = originalXml.CreateNode(XmlNodeType.Element, "MPP5_Asset_ID", null);
                assetid.InnerXml = Guid.NewGuid().ToString();
                assetnode.AppendChild(assetid);
            }

            Thread.Sleep(5000);

            originalXml.Save(v.FullName);
            _newfileInfo = v;
            File.SetAttributes(v.FullName, FileAttributes.Normal);
            _sourceFileInfo.Delete();
        }
        private void addNewIdNodeinChannel(FileInfo fi)
        {
            XmlDocument originalXml = new XmlDocument();
            originalXml.Load(_sourceFileInfo.FullName);

            XmlNode adiNode = originalXml.SelectSingleNode("Channel");
            XmlNode mpp5IdNode = originalXml.CreateNode(XmlNodeType.Element, "MPP5_ID", null);
            mpp5IdNode.InnerXml = Guid.NewGuid().ToString();
            adiNode.AppendChild(mpp5IdNode);

            //adding mpp5 id for assets
            XmlNodeList assetNodeList = originalXml.SelectNodes("Channel/DefaultConfiguration/Assets/Asset");

            foreach (XmlNode assetnode in assetNodeList)
            {
                XmlAttribute assetAttribute = originalXml.CreateAttribute("MPP5_Asset_ID");
                assetAttribute.Value = Guid.NewGuid().ToString();
                assetnode.Attributes.Append(assetAttribute);
            }
            Thread.Sleep(5000);
            //string new_file_in_work_folder=Path.Combine(_systemConfig.GetConfigParam(""))

            originalXml.Save(fi.FullName);
            _newfileInfo = fi;
            File.SetAttributes(fi.FullName, FileAttributes.Normal);
            _sourceFileInfo.Delete();
        }
        public FileInfo getNewFileInfo()
        {
            return _newfileInfo;
        }
    }
}
