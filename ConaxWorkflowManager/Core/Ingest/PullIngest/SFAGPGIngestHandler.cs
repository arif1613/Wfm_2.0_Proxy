using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest
{
    public class SFAGPGIngestHandler : SFAIngestHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override IEnumerable<int> GetAvailableExternalIds()
        {
            List<int> ints = new List<int>();
            foreach (var item in sfaFileHelper.GetIdentifiersForEncryptedFiles())
            {
                ints.Add(int.Parse(item.Trim(new char[] { '_' })));
            }

            return ints.AsEnumerable();
        }


        public override void ProcessFiles(int externalId)
        {
            log.Debug("Decrypting gpg for media id " + externalId);
            sfaFileHelper.DecryptFile(externalId);

            log.Debug("Moving files for media id  " + externalId + " to upload");
            sfaFileHelper.MovePreparedFilesToUpload(sfaFileHelper.GetIdentifier(externalId));
        }

        public override IEnumerable<int> GetIdsToProcess(IEnumerable<int> externalIds, IEnumerable<Util.ValueObjects.ContentData> content)
        {
            return externalIds;
        }

        public override IEnumerable<int> GetIdsToDelete(IEnumerable<int> externalIds, IEnumerable<Util.ValueObjects.ContentData> content)
        {
            return new int[0];
        }
    }
}
