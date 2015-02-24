using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.ValidIngestTask.MsgHandlers
{
    public class CreateEpgMsg
    {
        private static DateTime _dt;
        private static BrokeredMessage _brokeredMessage;
        private static string _workdirectory;
        private static string FoldersettingsFileName;
        private static ConaxWorkflowManagerConfig _systemConfig;
        private static ContentData _ConaxVodContentData { get; set; }

        public CreateEpgMsg()
        {
            //_brokeredMessage = br;
            //_dt = dt;
            //_systemConfig =
            //    (ConaxWorkflowManagerConfig)
            //        Config.GetConfig()
            //            .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            //_workdirectory = _systemConfig.FileIngestWorkDirectory;
            //FoldersettingsFileName = _systemConfig.FolderSettingsFileName;
            var x = GetEpGitemsFromFeed();
            Console.WriteLine("EPG items got from epg feed");
            Console.WriteLine(x.FirstOrDefault().Key);
        }

        public Dictionary<UInt64, List<EpgContentInfo>> GetEpGitemsFromFeed()
        {
            var epgIngestTask=new EPGIngestTask();
            return epgIngestTask.GetEpgItemsFromFeeds();
        }


    }
}
