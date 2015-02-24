using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public class FileInformation
    {
        public FileStatus Status { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public DateTime lastAccess { get; set; }

        public override string ToString()
        {
            return "{status=" + this.Status + ", size=" + this.Size + ", path=" + this.Path + ", isDirectory=" + this.IsDirectory + "}";
        }
    }
}
