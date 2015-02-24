using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler
{
    public interface IFileHandler
    {
        FileInformation[] ListDirectory(String path, bool includeDirectories);
        bool IsFileExclusive(String path);

        void CopyTo(String fromPath, String toPath);

        void MoveTo(String fromPath, String toPath);

        void DeleteFile(String path);
    }
}
