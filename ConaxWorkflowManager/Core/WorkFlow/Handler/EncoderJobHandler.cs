using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{

    public abstract class EncoderJobHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected bool trailerJob;

        protected String mezzanineName;

        protected String inputFolder;

        protected String encoderFolder;

        protected String outputFolder;

        protected String presetID;

        protected ContentData content;

        protected String PlayoutFileDirectory = "";

        protected int statusCheckInterval;

        protected String jobID;

        public bool TrailerJob
        {
            set { trailerJob = value; }
        }

        public ContentData Content
        {
            set { content = value; }
        }

        public String JobID
        {
            set { jobID = value; }
        }

        protected FileInfo copiedFile = null;

        protected FileInfo CopyFiles(string fullPathFrom, string fullPathTo)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fullPathTo)))
            {
                log.Debug("Directory " + Path.GetDirectoryName(fullPathTo) + " doesn't exist");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPathTo));
                }
                catch (Exception ex)
                {
                    log.Error("Folder couldn't be created", ex);
                }
            }

            log.Debug("moving file from " + fullPathFrom + " to " + fullPathTo);
            EncoderFileSystemHandler fileMover = new EncoderFileSystemHandler();
            copiedFile = fileMover.MoveFile(fullPathFrom, fullPathTo);
            return copiedFile;
        }
    }
}
