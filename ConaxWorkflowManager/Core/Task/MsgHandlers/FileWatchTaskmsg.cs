using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using log4net;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.FileOperations;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers
{
    public class FileWatchTaskmsg
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly XmlDocument UploadFolderConfigDoc = new XmlDocument();
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        private readonly ConaxWorkflowManagerConfig _systemConfig;

        public FileWatchTaskmsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            String fileIngestUploadDirectoryConfig = systemConfig.FileIngestUploadDirectoryConfig;
            UploadFolderConfigDoc.Load(fileIngestUploadDirectoryConfig);
            FindCompleteCrudxml(GetUploadFolderPaths());
        }
        public void FindCompleteCrudxml(IEnumerable<string> uploadFolderPaths)
        {
            log.Debug("Find Complete CRUD XML");
            String folderSettingsFileName = _systemConfig.FolderSettingsFileName;
            var fileInfosList = new List<FileInfo>();
            foreach (var uploadFolderPath in uploadFolderPaths)
            {
                if (Directory.Exists(uploadFolderPath))
                {
                    string[] fileInformations = Directory.GetFiles(uploadFolderPath);
                    fileInfosList = fileInformations.Select(v => new FileInfo(v)).ToList();
                    var fileInfo =
                        fileInfosList.SingleOrDefault(
                            f =>
                                Path.GetFileName(f.Name)
                                    .Equals(folderSettingsFileName, StringComparison.OrdinalIgnoreCase));
                    if (fileInfo == null)
                    {
                        Console.WriteLine("Upload folder " + uploadFolderPath + " missing config file " + folderSettingsFileName);
                    }
                    else
                    {
                        new FileCopier(fileInfo.FullName, "work");
                    }
                }
                #region finding crud xml
                var priXmLs = fileInfosList.Where(f => f.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)).OrderBy(f => f.LastAccessTimeUtc).ToList();
                IList<FileInfo> normalFileInformations = (from i in priXmLs let isHidden = ((File.GetAttributes(i.FullName) & FileAttributes.Hidden) == FileAttributes.Hidden) where !isHidden select i).ToList();
                #endregion

                #region Validate_CrudXml
                //validate xml
                foreach (var v in normalFileInformations)
                {
                    // identify xml type
                    IngestXMLType ingestXmlType = CommonUtil.GetIngestXMLType(v.FullName);
                    log.Debug("ingest xml " + v.DirectoryName + " is type " + ingestXmlType.ToString());
                    // load ingest xml handler
                    var ingestXmlConfig = Config.GetConfig().IngestXMLConfigs.SingleOrDefault(i => i.IngestXMLType.Equals(ingestXmlType.ToString(),
                                        StringComparison.OrdinalIgnoreCase));
                    if (ingestXmlConfig == null)
                    {
                        Console.WriteLine("ingestXmlType " + ingestXmlType.ToString() +
                                            " is not defiend in the configuraiton xml.");
                    }
                    try
                    {
                        var vx = new ValidateXml(v, ingestXmlConfig.XSD);
                        string cmdMsg;
                        string errorMsg;
                        if (vx.Validate() == null)
                        {
                            var rmediainfo = new ValidateMediaInfo(v);
                            var li = rmediainfo.GetVodMediaFilesInUploadfolder();
                            if (ingestXmlConfig.IngestXMLType != "Channel_1_0")
                            {
                                if (rmediainfo.checkVodAsset(_brokeredMessage.Properties["CmdMessage"].ToString()))
                                {
                                    if (li != null)
                                    {
                                        foreach (var p in li)
                                        {
                                            new FileMover(p, "work");
                                            Thread.Sleep(1000);
                                        }
                                    }
                                    File.SetAttributes(v.FullName, FileAttributes.Hidden);
                                    Thread.Sleep(1000);
                                    new MessageSender("Valid VOD Ingest Found", "Move To Work Folder", v, _brokeredMessage);
                                }
                                else
                                {
                                    cmdMsg = "Move To Reject Folder";
                                    errorMsg =
                                        "moving VOD asset to reject folder. Not all assets are found in upload folder.";
                                    if (li != null)
                                    {
                                        foreach (var p in li)
                                        {
                                            new FileMover(p, "reject");
                                            Thread.Sleep(1000);
                                        }

                                    }
                                    File.SetAttributes(v.FullName, FileAttributes.Hidden);
                                    Thread.Sleep(1000);
                                    new MessageSender(errorMsg, cmdMsg, v, _brokeredMessage);
                                }
                            }
                            else
                            {
                                if (rmediainfo.checkChannelAsset())
                                {
                                    li = rmediainfo.ChannelMediaFiles();
                                    cmdMsg = "Move Channel Files To Work Folder";
                                    errorMsg = "Valid channel Ingest Found";
                                    if (li != null)
                                    {
                                        foreach (var p in li)
                                        {
                                            new FileMover(p, "work");
                                            Thread.Sleep(1000);
                                        }

                                    }
                                    File.SetAttributes(v.FullName, FileAttributes.Hidden);
                                    Thread.Sleep(1000);
                                    new MessageSender(errorMsg, cmdMsg, v, _brokeredMessage);

                                }
                                else
                                {
                                    li = rmediainfo.ChannelMediaFiles();
                                    cmdMsg = "Move To Reject Folder";
                                    errorMsg =
                                        "moving Channel assets to reject folder. Not all assets are found in upload folder.";
                                    if (li != null)
                                    {
                                        foreach (var p in li)
                                        {
                                            new FileMover(p, "reject");
                                            Thread.Sleep(1000);
                                        }
                                    }
                                    Thread.Sleep(1000);
                                    new MessageSender(errorMsg, cmdMsg, v, _brokeredMessage);
                                    File.SetAttributes(v.FullName, FileAttributes.Hidden);

                                }
                                //making xml as hidden
                            }
                        }
                        else
                        {
                            var li = new List<string>();
                            var rmediainfo = new ValidateMediaInfo(v);
                            if (ingestXmlConfig.IngestXMLType != "Channel_1_0")
                            {
                                li = rmediainfo.ChannelMediaFiles();

                                File.SetAttributes(v.FullName, FileAttributes.Hidden);
                                errorMsg = vx.Validate();
                                cmdMsg = "Move To Reject Folder";
                                if (!li.Any())
                                {
                                    foreach (var x in li)
                                    {
                                        new FileMover(x, "reject");
                                        Thread.Sleep(1000);
                                    }
                                }
                                Thread.Sleep(1000);
                                new MessageSender(errorMsg, cmdMsg, v, _brokeredMessage);

                            }
                            else
                            {
                                li = rmediainfo.GetMediaFiles();
                                File.SetAttributes(v.FullName, FileAttributes.Hidden);
                                errorMsg = vx.Validate();
                                cmdMsg = "Move To Reject Folder";
                                if (!li.Any())
                                {
                                    foreach (var x in li)
                                    {
                                        new FileMover(x, "reject");
                                        Thread.Sleep(1000);
                                    }
                                }
                                Thread.Sleep(1000);
                                new MessageSender(errorMsg, cmdMsg, v, _brokeredMessage);

                            }
                        }
                    }
                    catch (XmlSchemaValidationException xmlEx)
                    {
                        Console.WriteLine(xmlEx.Message + ":" + v.DirectoryName);
                    }

                }

            }
                #endregion
        }
        private IEnumerable<string> GetUploadFolderPaths()
        {
            var folderPaths = new List<string>();
            var folderPathsToIgnore = new List<string>();

            XmlNodeList croNodes = UploadFolderConfigDoc.SelectNodes("UploadFolderConfig/ContentRightsOwner");

            foreach (XmlNode croNode in croNodes)
            {
                XmlNodeList uploadFolderNodes = croNode.SelectNodes("UploadFolders/UploadFolder");

                if (uploadFolderNodes != null)
                    log.Debug("CRO " + ((XmlElement)croNode).GetAttribute("name") + " have " + uploadFolderNodes.Count +
                              " upload folders.");

                foreach (XmlNode uploadFolderNode in uploadFolderNodes)
                {

                    String folderPath = uploadFolderNode.InnerText;
                    if (folderPaths.Contains(folderPath))
                    {
                        log.Warn("The upload folder path '" + folderPath +
                                 "' occurs more than once in the FileIngestUploadDirectoryConfig. This upload folder will be ignored.");
                        if (!folderPathsToIgnore.Contains(folderPath))
                            folderPathsToIgnore.Add(folderPath);
                    }
                    else
                        //getting ingest folder path
                        folderPaths.Add(folderPath);
                }
            }

            foreach (var folderPathToIgnore in folderPathsToIgnore)
            {
                folderPaths.RemoveAll(x => x == folderPathToIgnore);
            }

            return folderPaths;
        }
    }
}
