using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.CatchUp
{
    public interface ICatchUpFileHandler
    {
        void DeleteSSCatchUpFiles(List<SSManifest> ssManifests, String catchUpFSRoot);

        void DeleteHLSCatchUpFiles(List<HLSChunk> hlsChunks, String catchUpFSRoot);
    }
}
