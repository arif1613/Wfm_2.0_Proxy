using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.IO;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public class EncoderFileSystemHandler : IEncoderFileHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region IEncoderFileHandler Members

        public FileInfo MoveFile(string copyFromFullNameAndPath, string copyToFullNameAndPath)
        {
            try
            {
                FileInfo file = new FileInfo(copyFromFullNameAndPath);
                if (file == null)
                {
                    throw new Exception("Error when loading file, no file found!");
                }
                log.Debug("Started copying file " + copyFromFullNameAndPath + " to " + copyToFullNameAndPath);
                file.CopyTo(copyToFullNameAndPath, true);
                FileInfo ret = new FileInfo(copyToFullNameAndPath);
                return ret;
            }
            catch (Exception e)
            {
                log.Error("Error when trying to move file " + copyFromFullNameAndPath, e);
                return null;
            }
        }


        public bool RemoveFileFromIngestFolder(string fullNameAndPatch)
        {
            throw new NotImplementedException();
        }

        public bool RemoveOriginalFileFromEncoderFolder(string fullNameAndPath)
        {
            throw new NotImplementedException();
        }

        #endregion

        internal FileInfo MoveFile(FileInfo file, string fileAreaRoot)
        {
            FileInfo copiedFile = null;
            if (!fileAreaRoot.EndsWith("/"))
            {
                fileAreaRoot += "/";
            }
            try
            {
                copiedFile = file.CopyTo(fileAreaRoot + file.Name);
            }
            catch (Exception e)
            {
                log.Error("Error copying file to fileArea", e);
                return null;
            }
            return copiedFile;
        }

        internal bool RemoveCopiedFiles(List<FileInfo> files)
        {
            try
            {
                foreach (FileInfo file in files)
                {
                    file.Delete();
                }
            }
            catch (Exception e)
            {

            }
            return true;
        }

        internal List<FileInfo> GetHLSFiles(String folder)
        {
            List<FileInfo> files = new List<FileInfo>();
            List<String> allPlayListNames = new List<string>();
            List<String> allFileNames = new List<string>();
            FileInfo indexFile = new FileInfo(folder + "\\index.m3u8");
            if (indexFile == null)
            {
                throw new Exception("No index file found for HLS");
            }
            try
            {
                files.Add(indexFile);
                using (StreamReader sr = new StreamReader(indexFile.FullName))
                {
                    String line;
                    // Read and display lines from the file until the end of
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("#"))
                            continue;
                        allPlayListNames.Add(line);
                    }

                }
            }
            catch (Exception e)
            {
                throw;
            }

            foreach (String indexFileName in allPlayListNames)
            {
                try
                {
                    FileInfo file = new FileInfo(folder + "\\" + indexFileName);
                    files.Add(file);
                    using (StreamReader sr = new StreamReader(folder + "\\" + indexFileName))
                    {
                        String line;
                        // Read and display lines from the file until the end of
                        // the file is reached.
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.StartsWith("#"))
                                continue;
                            allFileNames.Add(line);
                        }

                    }
                }
                catch (Exception e)
                {

                }
            }

            foreach (String fileName in allFileNames)
            {
                FileInfo file = new FileInfo(folder + "\\" + fileName);
                if (file == null)
                {
                    throw new Exception("Could not load file with name " + fileName + " when fetching all files in index.m3u8");
                }
                files.Add(file);
            }

            return files;
        }
    }
}
