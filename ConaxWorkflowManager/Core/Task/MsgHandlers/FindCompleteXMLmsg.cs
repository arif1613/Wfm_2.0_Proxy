using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using log4net;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers
{
    public class FindCompleteXMLmsg
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly XmlDocument UploadFolderConfigDoc = new XmlDocument();
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        private readonly ConaxWorkflowManagerConfig _systemConfig;

        public FindCompleteXMLmsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            var systemConfig =
                (ConaxWorkflowManagerConfig)
                    Config.GetConfig()
                        .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            String fileIngestUploadDirectoryConfig = systemConfig.FileIngestUploadDirectoryConfig;
            UploadFolderConfigDoc.Load(fileIngestUploadDirectoryConfig);
        }

        public void findCrudXmlFromUploadDirectory()
        {
            List<FileInfo> fileInfosList = new List<FileInfo>();
            string uploadFolderPath = _brokeredMessage.Properties["UploadFolderPath"].ToString();
            string[] fileInformations = Directory.GetFiles(uploadFolderPath);
            fileInfosList = fileInformations.Select(v => new FileInfo(v)).ToList();
            var priXmLs =
                fileInfosList.Where(f => f.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f.LastAccessTimeUtc)
                    .ToList();
            IList<FileInfo> normalXmlFileInformations = (from i in priXmLs
                                                         let isHidden = ((File.GetAttributes(i.FullName) & FileAttributes.Hidden) == FileAttributes.Hidden)
                                                         where !isHidden
                                                         select i).ToList();
            foreach (var v in normalXmlFileInformations)
            {
                IngestXMLType ingestXmlType = CommonUtil.GetIngestXMLType(v.FullName);
                var ingestXmlConfig =
                    Config.GetConfig()
                        .IngestXMLConfigs.SingleOrDefault(i => i.IngestXMLType.Equals(ingestXmlType.ToString(),
                            StringComparison.OrdinalIgnoreCase));
                if (ingestXmlConfig == null)
                {
                    Console.WriteLine("ingestXmlType " + ingestXmlType.ToString() +
                                      " is not defiend in the configuraiton xml.");
                }
                else
                {
                    var vx = new ValidateXml(v, ingestXmlConfig.XSD);
                    string cmdMsg;
                    string errorMsg;
                    if (vx.Validate() == null)
                    {
                        cmdMsg = "Move To Work Folder";
                        errorMsg = "Valid VOD Ingest Found";
                        new MessageSender(errorMsg, cmdMsg, v, _brokeredMessage);
                    }
                    else
                    {
                        cmdMsg = "Move To Reject Folder";
                        errorMsg =
                            "moving VOD asset to reject folder. Validation failed.";
                        new MessageSender(errorMsg, cmdMsg, v, _brokeredMessage);
                    }
                }
            }
        }
    }
}
