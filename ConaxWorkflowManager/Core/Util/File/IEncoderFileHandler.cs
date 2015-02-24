using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File
{
    public interface IEncoderFileHandler
    {

        /// <summary>
        /// This method should handle the move of the source file from the ingest folder to the encoder folder
        /// </summary>
        /// <param name="copyFromFullNameAndPath">The full path and name to the file to copy</param>
        /// <param name="copyToFullNameAndPath">The full path and name of the place to move the file to.</param>
        /// <returns>true if successfull</returns>
        FileInfo MoveFile(String copyFromFullNameAndPath, String copyToFullNameAndPath);

        /// <summary>
        /// This method handles the removing of the source file from the ingest folder.
        /// </summary>
        /// <param name="fullNameAndPatch">The full path and name of the file to remove.</param>
        /// <returns></returns>
        bool RemoveFileFromIngestFolder(String fullNameAndPatch);

        /// <summary>
        /// This methodod removes the source file from the encoder folder.
        /// </summary>
        /// <param name="fullNameAndPath">The full path and name of the file to remove.</param>
        /// <returns></returns>
        bool RemoveOriginalFileFromEncoderFolder(String fullNameAndPath);

    }
}
