using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Net;
using log4net;
using System.Reflection;
using System.IO;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest.SFA
{
    public class SFAFileHelper
    {
        private string _downloadfolder;
        private string _decryptfolder;
        private string _uploadfolder;

        public SFAFileHelper()
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SFAnytime").SingleOrDefault();
            _downloadfolder = systemConfig.GetConfigParam("DownloadFolder");
            _decryptfolder = systemConfig.GetConfigParam("DecryptFolder");
            _uploadfolder = systemConfig.GetConfigParam("UploadFolder");
        }

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void DecryptFiles()
        {
            PGPDecryptor.DecryptAllGPGInFolder(_decryptfolder);
            DeleteGpgFilesInDecryptfolder();
        }

        public void DecryptFile(int mediaId)
        {
            string filePath = Directory.GetFiles(_decryptfolder).Where(x => x.EndsWith(".gpg")).FirstOrDefault(x => x.Contains(GetIdentifier(mediaId)));
            string fileName = filePath.Substring(filePath.LastIndexOf('\\') + 1);

            PGPDecryptor.DecryptFile(_decryptfolder, fileName);

            System.IO.File.Delete(filePath);
        }

        public void MovePreparedFilesToUpload(string identifier)
        {
            // move files in specific order to avoid errors
            // 1. movies 2. jpg 3. xml

            IEnumerable<string> files = Directory.GetFiles(_decryptfolder).Where(x => x.Contains(identifier));
            if (files.Count() != 4)
            {
                log.Warn("Files are missing in decryptfolder for media " + identifier);
            }

            foreach (var item in files.Where(x => x.EndsWith(".ts")))
            {
                MoveFile(item, _uploadfolder);
            }

            string imagePath = files.SingleOrDefault(x => x.EndsWith(".jpg"));
            MoveFile(imagePath, _uploadfolder);

            string xmlPath = files.SingleOrDefault(x => x.EndsWith(".xml"));
            MoveFile(xmlPath, _uploadfolder);
        }

        private void MoveFile(string filePath, string destFolder)
        {
            string fileName = Path.GetFileName(filePath);
            string dest = Path.Combine(destFolder, fileName);
            try
            {
                System.IO.File.Move(filePath, dest);
            }
            catch (System.IO.IOException ioe)
            {
                if (ioe.Message == "Cannot create a file when that file already exists.")
                {
                    System.IO.File.Delete(filePath);
                }
                else
                {
                    throw ioe;
                }
            }
        }

        private void DeleteGpgFilesInDecryptfolder()
        {
            foreach (var item in Directory.GetFiles(_decryptfolder).Where(x => x.EndsWith(".gpg")))
            {
                System.IO.File.Delete(item);
            }
        }

        public List<string> GetIdentifiersForEncryptedFiles()
        {
            List<string> toReturn = new List<string>();
            foreach (var item in Directory.GetFiles(_decryptfolder).Where(x => x.EndsWith(".gpg")))
            {
                string s = item.Substring(item.LastIndexOf('\\') + 1);

                // BM_XXX_h264.ts.gpg
                int startindex = s.IndexOf('_'); // = 2
                string endStr = s.Substring(startindex + 1); // = XXX_h264.ts.gpg
                int stopIndex = endStr.IndexOf('_');
                s = s.Substring(startindex, stopIndex + 2);

                toReturn.Add(s);
            }
            return toReturn;
        }

        public bool MoveFilesToDecrypt(int id)
        {
            foreach (string s in Directory.GetFiles(_downloadfolder).Where(x => x.Contains(GetIdentifier(id))))
            {
                string fileName = Path.GetFileName(s);
                string dest = Path.Combine(_decryptfolder, fileName);
                try
                {
                    System.IO.File.Move(s, dest);
                }
                catch (IOException ioe)
                {
                    if (ioe.Message.Contains("Cannot create"))
                    {
                        log.Warn("File already exists in folder, cannot move " + s);
                        File.Delete(s);
                    }
                }
            }
            return true;
        }

        public void ClearDownloadFolder()
        {
            foreach (string fileName in Directory.GetFiles(_downloadfolder))
            {
                System.IO.File.Delete(fileName);
            }
        }

        public bool GetCoverImage(SFAMedia m, int id)
        {
            try
            {
                WebClient client = new WebClient();
                byte[] data = client.DownloadData(m.ImageUrl);

                System.IO.File.WriteAllBytes(_downloadfolder + m.ImageFileName, data);
            }
            catch (Exception e)
            {
                log.Warn(e);
                return false;
            }
            return true;
        }

        public bool WriteXmlInDownloadDir(XmlDocument doc, string fileName)
        {
            try
            {
                XmlTextWriter writer = new XmlTextWriter(_downloadfolder + fileName + ".xml", Encoding.UTF8);
                doc.Save(writer);
                writer.Close();
            }
            catch (Exception e)
            {
                log.Warn(e);
                return false;
            }
            return true;
        }

        public string GetIdentifier(int id)
        {
            return "_" + id.ToString() + "_";
        }
    }
}
