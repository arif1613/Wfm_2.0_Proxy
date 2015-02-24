using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.IO;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.CatchUp
{
    public class ElementalFileHandler : ICatchUpFileHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region ICatchUpFileHandler Members

        public void DeleteSSCatchUpFiles(List<SSManifest> ssManifests, String catchUpFSRoot)
        {
            // do nothing, since we are using chodeshop to handle SS, it should be deleted by codeshop for now.
        }

        public void DeleteHLSCatchUpFiles(List<HLSChunk> hlsChunks, String catchUpFSRoot)
        {

            List<String> segFolders = new List<String>();
            foreach (HLSChunk hlsChunk in hlsChunks)
            {
                try
                {
                    String segName = hlsChunk.URI.Replace("/", "\\");
                    if (segName.StartsWith("\\"))
                        segName = segName.Substring(1);

                    String pf = Path.GetDirectoryName(segName);
                    if (!segFolders.Contains(pf))
                        segFolders.Add(pf);

                    String segmentpath = Path.Combine(catchUpFSRoot, segName);
                    log.Debug("start purge " + segmentpath);

                    if (System.IO.File.Exists(segmentpath))
                        System.IO.File.Delete(segmentpath);
                }
                catch (Exception ex)
                {
                    log.Error("Failed to delete hls segment " + hlsChunk.URI, ex);
                }
            }

            foreach(String segFolder in segFolders) 
                DeleteSegFolder(segFolder, catchUpFSRoot);

        }

        private void DeleteSegFolder(String segFolder, String catchUpFSRoot)
        {
            String segmentpath = Path.Combine(catchUpFSRoot, segFolder);
            if (Directory.Exists(segmentpath))
            {
                if (Directory.GetFiles(segmentpath).Length > 0 ||
                    Directory.GetDirectories(segmentpath).Length > 0)
                    return;

                Directory.Delete(segmentpath);
            }

            String parentFolder = Path.GetDirectoryName(segFolder);
            if (parentFolder.Length > 1) {
                DeleteSegFolder(parentFolder, catchUpFSRoot);
            }
        }

        #endregion
    }
}
