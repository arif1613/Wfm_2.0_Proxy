using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.EPG
{
    /// <summary>
    /// This class is used to lock content for being created to different systems
    /// </summary>
    public class ContentLocker
    {
        private static Hashtable workingList = new Hashtable();

        /// <summary>
        /// This method adds the content to the lock list, ExternalID is used for the lock
        /// </summary>
        /// <param name="contentToLock">The content to add to the lock list</param>
        public static void LockContent(ContentData contentToLock)
        {
            lock (workingList.SyncRoot)
            {
                if (!workingList.ContainsKey(contentToLock.ExternalID))
                    workingList.Add(contentToLock.ExternalID, contentToLock.ExternalID);
            }
        }

        /// <summary>
        /// This method removes the content from the lock list, ExternalID is used for the lock
        /// </summary>
        /// <param name="contentToLock">The content to remove from the lock list</param>
        public static void UnLockContent(ContentData contentToUnLock)
        {
            lock (workingList.SyncRoot)
            {
                if (workingList.ContainsKey(contentToUnLock.ExternalID))
                    workingList.Remove(contentToUnLock.ExternalID);
            }
        }

        /// <summary>
        /// This method adds a list of content to the lock list, ExternalID is used for the lock
        /// </summary>
        /// <param name="contentToLock">The content to remove from the lock list</param>
        public static void LockContentList(List<ContentData> contentToLockList)
        {
            lock (workingList.SyncRoot)
            {
                foreach (ContentData contentToLock in contentToLockList)
                {
                    if (!workingList.ContainsKey(contentToLock.ExternalID))
                        workingList.Add(contentToLock.ExternalID, contentToLock.ExternalID);
                }
            }
        }

        /// <summary>
        /// This method removes a list of content from the lock list, ExternalID is used for the lock
        /// </summary>
        /// <param name="contentToLock">The content to remove from the lock list</param>
        public static void UnLockContentList(List<ContentData> contentToUnLockList)
        {
            lock (workingList.SyncRoot)
            {
                foreach (ContentData contentToUnLock in contentToUnLockList)
                {
                    if (workingList.ContainsKey(contentToUnLock.ExternalID))
                        workingList.Remove(contentToUnLock.ExternalID);
                }
            }
        }

        /// <summary>
        /// This method checks if a content exists in the lock list
        /// </summary>
        /// <param name="content">Content to check if it's locked</param>
        /// <returns>true if content exists in lock list, othervise false</returns>
        public static bool ContentIsLocked(ContentData content)
        {
            lock (workingList.SyncRoot)
            {
                return workingList.ContainsKey(content.ExternalID);
            }
        }

        /// <summary>
        /// This method checks if a content exists in the lock list
        /// </summary>
        /// <param name="externalID">ExternalID of Content to check if it's locked</param>
        /// <returns>true if content exists in lock list, othervise false</returns>
        public static bool ContentIsLocked(String externalID)
        {
            lock (workingList.SyncRoot)
            {
                return workingList.ContainsKey(externalID);
            }
        }

        /// <summary>
        /// This method checks if a content exists in the lock list
        /// </summary>
        /// <param name="content">Content to check if it's locked</param>
        /// <returns>true if content exists in lock list, othervise false</returns>
        public static void ClearLockedContentList()
        {
            lock (workingList.SyncRoot)
            {
                workingList.Clear();
            }
        }
    }
}
