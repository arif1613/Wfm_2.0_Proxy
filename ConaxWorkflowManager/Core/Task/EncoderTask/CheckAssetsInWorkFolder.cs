using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task.EncoderTask
{
    public class CheckAssetsInWorkFolder
    {
        private static ContentData _contentData;
        private static BrokeredMessage _brokeredMessage;

        public CheckAssetsInWorkFolder(ContentData cd,BrokeredMessage br)
        {
            _brokeredMessage = br;
            _contentData = cd;
        }

        public bool checkAssetInWorkFolder()
        {
            var systemConfig =
                (ConaxWorkflowManagerConfig)
                    Config.GetConfig()
                        .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            var workfolder = systemConfig.FileIngestWorkDirectory;
            var uploadfolder = systemConfig.FileIngestUploadDirectory;
            var assetFileNameList = new List<string>();
            foreach (var v in _contentData.Assets)
            {
                string assetname = v.Name;
                string filename = Path.Combine(workfolder, assetname);
                if (File.Exists(filename))
                {
                    assetFileNameList.Add(filename);
                }
                else
                {
                    if (File.Exists(Path.Combine(uploadfolder, assetname)))
                    {
                        File.Move(Path.Combine(uploadfolder, assetname), filename);
                        Thread.Sleep(5000);
                        assetFileNameList.Add(filename);
                        File.Delete(Path.Combine(uploadfolder, assetname));
                    }

                }
            }

            if (_contentData.Assets.Count == assetFileNameList.Count)
            {
                return true;
            }
            return false;
        }
    }

}
