using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest.SFA
{
    public class SFAFtpHelper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static string _baseAddress;
        private static string _username;
        private static string _password;
        private static string _folder;

        public SFAFtpHelper()
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SFAnytime").SingleOrDefault();

            _baseAddress = systemConfig.GetConfigParam("FtpAddress");
            _username = systemConfig.GetConfigParam("FtpUsername");
            _password = systemConfig.GetConfigParam("FtpPassword");
            _folder = systemConfig.GetConfigParam("DownloadFolder");
        }

        public bool DownloadFilesForMedia(SFAMedia media)
        {
            try
            {
                DownloadFileToWorkDir(media.TrailerUrl, media.TrailerFilename, media.TrailerPath, media.NewTrailerFilename);
                DownloadFileToWorkDir(media.VideoUrl, media.EncryptedFileName, media.videoPath, media.EncryptedFileName);
            }
            catch (Exception e)
            {
                log.Warn(e);
                return false;
            }
            
            return true;
        }

        private void DownloadFileToWorkDir(string fullPath, string fileName, string filePath, string newFileName)
        {
            if (!string.IsNullOrEmpty(_baseAddress) && !fullPath.StartsWith(_baseAddress))
            {
                fullPath = _baseAddress + "/" + filePath + "/" + fileName;
                log.Debug("Using custom ftp, get file at: " + fullPath);
            }
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fullPath);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            request.Credentials = new NetworkCredential(_username, _password);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();
            FileStream file = System.IO.File.Create(_folder + newFileName);
            byte[] buffer = new byte[32 * 1024];
            int read;

            try
            {
                while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    file.Write(buffer, 0, read);
                }
            }
            catch (Exception e)
            {
                file.Close();
                throw e;
            }

            file.Close();
            responseStream.Close();
            response.Close();
        }


        public bool TEST__DownloadFilesForMedia(SFAMedia media, int id)
        {
            string filename = "BM_" + id + "_" + "jadda.ts.gpg";
            string trailer = "BM_" + id + "_tr_" + "jadda.ts";
            System.IO.File.Copy(@"C:\MPS\conax\SFA\test.txt", _folder + filename);
            System.IO.File.Copy(@"C:\MPS\conax\SFA\test.txt", _folder + trailer);
            return true;
        }
    }
}
