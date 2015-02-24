using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler
{
    public class ProgressStateFileHandler : IFileHandler
    {
        #region IFileHandler Members

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public FileInformation[] ListDirectory(string path, bool includeDirectories)
        {
            DirectoryInfo dir = null;
            if (Directory.Exists(path))
            {
                dir = new DirectoryInfo(path);
            }
            else if (System.IO.File.Exists(path))
            {
                FileInfo file = new FileInfo(path);
                dir = file.Directory;
            }
            if (dir == null)
                return new FileInformation[] { };
            FileInfo[] files = dir.GetFiles();
            List<FileInformation> infos = new List<FileInformation>();
            foreach (FileInfo fi in files)
            {
                FileInformation info = new FileInformation();
                info.Path = fi.FullName;
                info.Status = FileStatus.Exists;
                info.Size = fi.Length;
                info.lastAccess = fi.LastAccessTime;
                infos.Add(info);
            }
            if (includeDirectories)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo di in dirs)
                {
                    FileInformation info = new FileInformation();
                    info.Path = di.FullName;
                    info.Status = FileStatus.Exists;
                    info.IsDirectory = true;
                    infos.Add(info);
                }
            }
            return infos.ToArray();
        }

        public bool IsFileExclusive(string path)
        {
            if (!System.IO.File.Exists(path))
                return false;
            FileAttributes attr = System.IO.File.GetAttributes(path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                return false;

            bool ret = true;
            FileInfo f = new FileInfo(path);
            //Make sure metadatafile is not being written....
            FileStream dummy = null;
            try
            {
                dummy = f.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (IOException ioe)
            {
                ret = false;
                f = null;
            }
            finally
            {
                try { dummy.Close(); }
                catch (Exception e) { }
            }
            return ret;
        }

        public void CopyTo(string fromPath, string toPath)
        {
            log.Debug("Copying using ProgressStateFileHandler");
            int CopyBufferSize = 128*1024;
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            if (systemConfig.ConfigParams.ContainsKey("CopyBufferSize") &&
                !String.IsNullOrEmpty(systemConfig.GetConfigParam("CopyBufferSize")))
            {
                if (int.TryParse(systemConfig.GetConfigParam("CopyBufferSize"), out CopyBufferSize))
                {
                    CopyBufferSize = CopyBufferSize*1024;
                }
                else
                {
                    log.Warn("Could'nt parse buffertSize, using default");
                    CopyBufferSize = 128 * 1024;
                }

            }
            log.Debug("Using BufferSize " + CopyBufferSize);
            if (!Directory.Exists(Path.GetDirectoryName(toPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(toPath));

            try
            {
                using (var outputFile = System.IO.File.Create(toPath))
                {
                    using (var inputFile = System.IO.File.OpenRead(fromPath))
                    {
                        long totalSize = inputFile.Length;
                        // we need two buffers so we can ping-pong
                        var buffer1 = new byte[CopyBufferSize];
                        var buffer2 = new byte[CopyBufferSize];
                        var inputBuffer = buffer1;
                        int bytesRead;
                        long totalRead = 0;
                        decimal percentDone = 0;
                        IAsyncResult writeResult = null;
                        int i = 0;
                        while ((bytesRead = inputFile.Read(inputBuffer, 0, CopyBufferSize)) != 0)
                        {
                            i++;
                            // Wait for pending write
                            if (writeResult != null)
                            {
                                writeResult.AsyncWaitHandle.WaitOne();
                                outputFile.EndWrite(writeResult);
                                writeResult = null;
                            }
                            // Assign the output buffer
                            var outputBuffer = inputBuffer;
                            // and swap input buffers
                            inputBuffer = (inputBuffer == buffer1) ? buffer2 : buffer1;
                            // begin asynchronous write
                            writeResult = outputFile.BeginWrite(outputBuffer, 0, bytesRead, null, null);
                            totalRead += bytesRead;
                            if (i > 100)
                            {
                                percentDone = (decimal) totalRead/(decimal) totalSize*100;
                                String s = string.Format("{0:N2}%", percentDone);
                                log.Debug("Copying file to " + toPath + ", " + s + "% done");
                                i = 0;
                            }
                        }
                        if (writeResult != null)
                        {
                            writeResult.AsyncWaitHandle.WaitOne();
                            outputFile.EndWrite(writeResult);
                        }
                        inputFile.Close();
                    }
                    outputFile.Close();
                }
            }
            catch (Exception exc)
            {
                log.Error("Error moving file", exc);
                throw;
            }
            log.Debug("Done copying file");
        }

        public void MoveTo(string fromPath, string toPath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(toPath)))
            {
                log.Debug("Directory " + Path.GetDirectoryName(toPath) + " didn't exist, creating");
                Directory.CreateDirectory(Path.GetDirectoryName(toPath));
            }
            else if (System.IO.File.Exists(toPath))
            {
                log.Debug("File " + toPath + " exist");
                FileAttributes attributes = System.IO.File.GetAttributes(toPath);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    log.Debug("File was readonly");
                    System.IO.File.SetAttributes(toPath, ~FileAttributes.ReadOnly);
                }
                try
                {
                    log.Debug("Deleting file");
                    System.IO.File.Delete(toPath);
                    log.Debug("File was deleted");
                    Thread.Sleep(2000);
                }
                catch (Exception exc)
                {
                    log.Debug("Error deleting existing file, continuing", exc);
                }
            }
            FileInfo fromFile = new FileInfo(fromPath);
            log.Debug("Moving file");
            fromFile.MoveTo(toPath);
            log.Debug("Done Moving file");
        }

        public void DeleteFile(string path)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
