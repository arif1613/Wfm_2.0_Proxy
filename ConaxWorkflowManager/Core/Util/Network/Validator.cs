using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MPS.MPP.Auxiliary.ImportServer.Server.Util
{
    public class Validator
    {
        /// <summary>
        /// Returns free space for drive containing the specified folder, or returns -1 on failure.
        /// </summary>
        /// <param name="folderName">Must name a folder, and MUST end with a backslash.</param>
        /// <returns>Space free on the volume containing 'folderName' or -1 on error.</returns>
        public static Int64 CheckFreeSpace(string folderName)
        {
            Int64 free = 0, dummy1 = 0, dummy2 = 0;

            folderName = ModifyFolderName(folderName);

            if (GetDiskFreeSpaceEx(folderName, ref free, ref dummy1,
            ref dummy2))
                return free;
            else
                return -1;
        }


        [DllImport("Kernel32")]
        public static extern bool GetDiskFreeSpaceEx
        (
            string lpszPath, // Must name a folder, must end with '\'.
            ref long lpFreeBytesAvailable,
            ref long lpTotalNumberOfBytes,
            ref long lpTotalNumberOfFreeBytes
        );

        private static String ModifyFolderName(String folderName)
        {
            String fn = folderName;

            if (folderName.EndsWith(@"\") || folderName.EndsWith("/"))
                return fn;
            else
                return fn + "\\";
        }
    }
}
