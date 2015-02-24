using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using log4net;
using System.Reflection;
using System.Threading;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler
{
    public class FileSystemHandler : IFileHandler
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

        public bool IsFileExclusive(String path)
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

        public void CopyTo(String fromPath, String toPath) {

            if (!Directory.Exists(Path.GetDirectoryName(toPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(toPath));

            if (System.IO.File.Exists(toPath))
            {
                log.Debug("File " + toPath + " exist");
                FileAttributes attributes = System.IO.File.GetAttributes(toPath);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    log.Debug("File was readonly");
                    System.IO.File.SetAttributes(toPath, ~FileAttributes.ReadOnly);
                }
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

            FileInfo fromFile = new FileInfo(fromPath);
            fromFile.CopyTo(toPath, true);
        }

        public void MoveTo(String fromPath, String toPath)
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
            log.Info("Done Moving file to " + toPath);
        }

        public void DeleteFile(String path) {

            if (System.IO.File.Exists(path))
            {
                FileAttributes attributes = System.IO.File.GetAttributes(path);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    System.IO.File.SetAttributes(path, ~FileAttributes.ReadOnly);
                }
            }

            FileInfo file = new FileInfo(path);
            file.Delete();
        }

        #endregion
    }
}
