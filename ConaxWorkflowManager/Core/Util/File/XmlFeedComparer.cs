using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public class XmlFeedComparer
    {
        private static XElement _newFeed;

        private static String _feedName;

        private static String _extraLoggingFolder;

        private static DateTime _executeTime;



        public static void SaveFeedIfDifferent(XElement newFeed, String feedName, DateTime executeTime)
        {
            try
            {
                var systemConfig =
                    Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == "ConaxWorkflowManager");
                if (systemConfig.ConfigParams.ContainsKey("FolderToSaveChangedEpgFeedsTo"))
                {
                    _extraLoggingFolder = systemConfig.GetConfigParam("FolderToSaveChangedEpgFeedsTo");
                    if (!Directory.Exists(_extraLoggingFolder))
                        Directory.CreateDirectory(_extraLoggingFolder);
                    _newFeed = newFeed;
                    _feedName = feedName;
                    _executeTime = executeTime;
                    FileInfo latestLoggedFile = GetLatestLoggedEpgFile();
                    XElement latestSavedFeed = null;
                    if (latestLoggedFile != null)
                        latestSavedFeed = XElement.Load(latestLoggedFile.FullName);
                    if (latestLoggedFile == null || !latestSavedFeed.Value.Equals(_newFeed.Value))
                    {
                        FileInfo fi = new FileInfo(_feedName);
                        String fileName = Path.Combine(_extraLoggingFolder,
                            fi.Name.Replace(".xml", "_") + _executeTime.ToString("yyyyMMdd_HHmm") + ".xml");
                        _newFeed.Save(fileName);

                    }
                }
            }
            catch (Exception exc)
            {

            }
        }

        private static FileInfo GetLatestLoggedEpgFile()
        {
            List<FileInfo> files = GetFilesMatchingName();
            if (!files.Any())
                return null;
            files = files.OrderByDescending(f => f.LastWriteTimeUtc).ToList();
            return files[0];
        }

        private static List<FileInfo> GetFilesMatchingName()
        {
            List<FileInfo> allMatchingFiles = new List<FileInfo>();
            FileInfo fi = new FileInfo(_feedName);

            string[] fileNames = Directory.GetFiles(_extraLoggingFolder, fi.Name.Replace(".xml", "") + "*.*");
            foreach (string fileName in fileNames)
            {
                FileInfo fileInfo = null;
                try
                {
                    fileInfo = new FileInfo(fileName);
                }
                catch (Exception)
                {
                    continue;
                }
                allMatchingFiles.Add(fileInfo);
            }
            return allMatchingFiles;
        }


    }

}
