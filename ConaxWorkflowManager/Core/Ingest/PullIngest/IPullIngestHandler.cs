using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest
{
    public interface IPullIngestHandler
    {
        /// <summary>
        /// Get the external Id's of all content available in remote system
        /// </summary>
        /// <returns></returns>
        IEnumerable<int> GetAvailableExternalIds();

        /// <summary>
        /// Get name of external system. Used for setting value of property "IngestSource" 
        /// in ContentSearchParams when calling GetContentForExternalId
        /// and to use right MPP User when deleting content in MPP
        /// </summary>
        /// <returns></returns>
        string GetSystemName();

        /// <summary>
        /// Check that the external Id from MPP content is on correct format (ex: SFA-12345)
        /// </summary>
        /// <param name="idString"></param>
        /// <returns></returns>
        bool IsValidExternalId(string MPPExternalIdString);

        /// <summary>
        /// Add prefix to external Id
        /// </summary>
        /// <param name="externalId"></param>
        /// <returns></returns>
        string CreateMPPExternalIdString(int externalId);

        /// <summary>
        /// Download/decrypt/move/create files that should be added to MPP/Conax
        /// </summary>
        /// <param name="externalId"></param>
        void ProcessFiles(int externalId);

        /// <summary>
        /// Get ids of all media to add/work with
        /// </summary>
        /// <param name="externalIds"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        IEnumerable<int> GetIdsToProcess(IEnumerable<int> externalIds, IEnumerable<ContentData> content);

        /// <summary>
        /// Get ids of all media that should be removed from MPP/Conax
        /// </summary>
        /// <param name="externalIds"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        IEnumerable<int> GetIdsToDelete(IEnumerable<int> externalIds, IEnumerable<ContentData> content);
    }
}
