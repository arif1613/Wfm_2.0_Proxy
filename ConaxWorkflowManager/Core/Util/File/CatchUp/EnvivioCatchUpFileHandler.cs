using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;


namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.CatchUp
{
    public class EnvivioCatchUpFileHandler : ICatchUpFileHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region ICatchUpFileHandler Members

        public void DeleteSSCatchUpFiles(List<SSManifest> ssManifests, String catchUpFSRoot)
        {
            foreach (SSManifest ssManifest in ssManifests)
            {
                try
                {
                    String manifestName = ssManifest.ManifestFileName.Replace("/", "\\");
                    if (manifestName.StartsWith("\\"))
                        manifestName = manifestName.Substring(1);

                    String manifestPath = Path.Combine(catchUpFSRoot, manifestName);
                    log.Debug("start purge " + manifestPath);

                    if (System.IO.File.Exists(manifestPath))
                    {
                        String segDir = Path.GetDirectoryName(manifestPath);
                        String catchupDir = Path.GetDirectoryName(segDir);

                        log.Debug("Delete dir " + segDir);
                        DirectoryInfo segdir = new DirectoryInfo(segDir);
                        segdir.Delete(true);

                        // check if catchup dir is empty for dlete
                        DirectoryInfo catchdir = new DirectoryInfo(catchupDir);
                        DirectoryInfo[] setdirs = catchdir.GetDirectories();
                        if (setdirs.Length == 0)
                        {
                            log.Debug("Catchp fodler " + catchupDir + " is empty, no segments left, ready for delete.");
                            catchdir.Delete(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Failed to delete " + ssManifest.ManifestFileName, ex);
                }
            }
        }

        public void DeleteHLSCatchUpFiles(List<HLSChunk> hlsChunks, String catchUpFSRoot)
        {

            foreach (HLSChunk hlsChunk in hlsChunks)
            {
                try
                {
                    String segName = hlsChunk.URI.Replace("/", "\\");
                    if (segName.StartsWith("\\"))
                        segName = segName.Substring(1);

                    String segmentpath = Path.Combine(catchUpFSRoot, segName);
                    log.Debug("start purge " + segmentpath);

                    if (System.IO.File.Exists(segmentpath))
                    {
                        FileAttributes attributes = System.IO.File.GetAttributes(segmentpath);
                        if ((attributes & FileAttributes.ReadOnly) != 0)
                        {
                            log.Debug("File " + segmentpath + " is readonly, removing readonly");
                            System.IO.File.SetAttributes(segmentpath, ~FileAttributes.ReadOnly);

                        }
                        System.IO.File.Delete(segmentpath);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Failed to delete hls segment " + hlsChunk.URI, ex);
                }
            }

        }

        #endregion
    }
}
