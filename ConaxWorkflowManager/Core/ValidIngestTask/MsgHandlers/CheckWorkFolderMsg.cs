using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.PublishTask;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using NodaTime;
using MessageSender = MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.MsgHandlers.MessageSender;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.MsgHandlers
{
    public class CheckWorkFolderMsg
    {
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        private static string _workdirectory;
        private static string _foldersettingsFileName;
        private static ConaxWorkflowManagerConfig _systemConfig;
        private static ContentData ConaxVodContentData { get; set; }
        private static IngestXMLType IngestXmlType { get; set; }


        public CheckWorkFolderMsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            _dt = dt;
            _systemConfig =
                (ConaxWorkflowManagerConfig)
                    Config.GetConfig()
                        .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _workdirectory = _systemConfig.FileIngestWorkDirectory;
            _foldersettingsFileName = _systemConfig.FolderSettingsFileName;
            WorkFolderChecking();
        }
        public void WorkFolderChecking()
        {

            List<FileInfo> xmlFileInfos = FindXmlFilesInWorkDirectory();
            if (xmlFileInfos.Any())
            {
                foreach (var f in xmlFileInfos)
                {
                    if (CheckIfContentsArePresent(f))
                    {
                        Console.WriteLine("All contents present" + f.FullName);
                        Thread.Sleep(2000);
                        new CreateMppPublishingProfile(ConaxVodContentData);
                        if (IngestXmlType.ToString() == "Channel_1_0")
                        {
                            new MessageSender(null, "Publish Channel to MPP5", f, _brokeredMessage);
                        }
                        File.SetAttributes(f.FullName, FileAttributes.Hidden);
                    }
                    else
                    {
                        Console.WriteLine("contents absent");
                        File.SetAttributes(f.FullName, FileAttributes.Normal);
                    }
                }
            }
            else
            {
                Console.WriteLine("No valid new ingest xml found");
            }
        }
        private bool CheckIfContentsArePresent(FileInfo p)
        {

            IngestXMLType ingestXmlType = CommonUtil.GetIngestXMLType(p.FullName);
            IngestXmlType = ingestXmlType;
            var vmMediaInfo = new ValidateMediaInfo(p);
            bool IsPresent = false;
            if (ingestXmlType.ToString() == "Channel_1_0")
            {
                IsPresent = vmMediaInfo.checkChannelAsset();
            }
            else
            {
                IsPresent = vmMediaInfo.checkVodAsset(null);
            }

            if (IsPresent && ingestXmlType.ToString() != "Channel_1_0")
            {
                CreateVodInMpp(p.FullName);
            }
            else
            {

                GetConaxChannelContentData(p.FullName);
            }
            return IsPresent;
        }
        public List<FileInfo> FindXmlFilesInWorkDirectory()
        {
            var XmlFileInfosList = new List<FileInfo>();
            var SubDirectoryList = FindSubdirectoryWithFoldersettingFile();
            foreach (var f in SubDirectoryList)
            {
                List<FileInfo> XmlFileInfos = GetXmlFileInfoList(f);
                if (XmlFileInfos.Any())
                {
                    foreach (var fi in XmlFileInfos)
                    {
                        XmlFileInfosList.Add(fi);
                    }
                }
            }

            return XmlFileInfosList;
        }
        private List<FileInfo> GetXmlFileInfoList(string directoryPath)
        {
            string filetype = string.Format("*.xml", StringComparison.OrdinalIgnoreCase);
            List<string> fileInformations = Directory.GetFiles(directoryPath, filetype).ToList();
            var xmlfileinfos = new List<FileInfo>();

            foreach (var filepath in fileInformations)
            {
                var i = new FileInfo(filepath);
                bool isHidden = ((File.GetAttributes(i.FullName) & FileAttributes.Hidden) == FileAttributes.Hidden);
                if (!isHidden)
                {
                    xmlfileinfos.Add(i);
                }
            }
            return xmlfileinfos;
        }
        public List<string> FindSubdirectoryWithFoldersettingFile()
        {
            var ValidSubDirectoryNames = new List<string>();
            var subdirectoriesList = Directory.GetDirectories(_workdirectory).ToList();
            foreach (var d in subdirectoriesList)
            {
                if (File.Exists(Path.Combine(d, _foldersettingsFileName)))
                {
                    ValidSubDirectoryNames.Add(d);
                }
            }
            return ValidSubDirectoryNames;
        }
        public void CreateVodInMpp(string filename)
        {
            try
            {
                var contegoVoDmsg = new CreateContegoVODmsg(filename);

                //content data
                ConaxVodContentData = contegoVoDmsg.GetContentData();

                var assets = ConaxVodContentData.Assets.ToList();

                string s = null;
                var listaAssets = new List<Asset>();
                foreach (var v in assets)
                {
                    if (s != v.Name)
                    {
                        listaAssets.Add(v);
                    }
                    s = v.Name;
                }


                Thread.Sleep(2000);
                //Adding assets in MPP5
                AddAssetToMpp5(listaAssets);
            }
            catch (Exception ex)
            {
                Console.WriteLine("vod content can't be created in MPP5 for {0}",ex.InnerException);
            }

            //publish asset in MPP5


        }
        public void GetConaxChannelContentData(string filename)
        {

            var contegoVoDmsg = new CreateContegoVODmsg(filename);
            ConaxVodContentData = contegoVoDmsg.GetContentData();
        }
        private void AddAssetToMpp5(IEnumerable<Asset> listOfAsset)
        {
            var createMpp5Content = new CreateMpp5Content();

            if (!createMpp5Content.CheckIfVodIsCreated(ConaxVodContentData.Mpp5_Id)) return;
            foreach (var v in listOfAsset)
            {
                //string newAsset = v.Split('\\').Last();
                createMpp5Content.AddAssetsInMpp5(getAssetModel(v), ConaxVodContentData.Mpp5_Id);
            }
        }
        private AddVodAssetModel getAssetModel(Asset asset)
        {
            var mpp5Config =
                (Mpp5Configuration)
                    Config.GetConfig().SystemConfigs.FirstOrDefault(r => r.SystemName == SystemConfigNames.MPP5);
            var addVodAssetModel = new AddVodAssetModel();

            addVodAssetModel.Id = new Guid(ConaxVodContentData.Mpp5_Id);
            addVodAssetModel.AssetId = asset.Mpp5_Asset_Id;
            addVodAssetModel.CausationId = new Guid(_brokeredMessage.Properties["CausationId"].ToString());
            addVodAssetModel.CorrelationId = new Guid(_brokeredMessage.CorrelationId);
            addVodAssetModel.Encoded = false;
            addVodAssetModel.MessageId = new Guid(_brokeredMessage.MessageId);
            addVodAssetModel.Name = asset.Name.Split('\\').Last();
            addVodAssetModel.OwnerId = new Guid(mpp5Config.HolderID);
            addVodAssetModel.Timestamp = Instant.FromDateTimeUtc(DateTime.UtcNow);
            addVodAssetModel.WamsAccountId = new Guid("dd0bc9bd-3078-49cc-fdaf-baa97505dc5d");
            addVodAssetModel.WamsAssetId = Guid.NewGuid().ToString();

            return addVodAssetModel;
        }
    }

}

