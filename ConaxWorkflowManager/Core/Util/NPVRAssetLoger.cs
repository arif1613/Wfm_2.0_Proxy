using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util
{
    public class NPVRAssetLoger
    {
        
        private static object syncRoot = new Object();

        public static void WriteLog(ContentData content, String msg)
        {
            WriteLog(content.ID + "_" + content.ExternalID, msg);
        }

        public static void WriteLog(String contentID,String msg)
        {
            String stateFilePath = Config.GetConaxWorkflowManagerConfig().ExtraNPVRAssetLog;
            if (String.IsNullOrWhiteSpace(stateFilePath))
                return;

            stateFilePath = Path.Combine(stateFilePath, DateTime.UtcNow.ToString("yyyy-MM-dd"));
            stateFilePath = Path.Combine(stateFilePath, contentID + ".log");
            msg = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss ") + msg + Environment.NewLine;
            lock (syncRoot)
            {
                try
                {
                    String folder = Path.GetDirectoryName(stateFilePath);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    System.IO.File.AppendAllText(stateFilePath, msg);
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}
