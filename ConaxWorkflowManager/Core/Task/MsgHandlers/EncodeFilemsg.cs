using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class EncodeFilemsg
    {
        private static BrokeredMessage _brokeredMessage;
        private readonly ConaxWorkflowManagerConfig _systemConfig;
        private static MPPConfig _mppConfig;
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ContentData vodContent;

        public EncodeFilemsg(BrokeredMessage br, DateTime dt)
        {
            _brokeredMessage = br;
            var systemConfig =
                (ConaxWorkflowManagerConfig)
                    Config.GetConfig()
                        .SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            _systemConfig = systemConfig;
            var mppConfig =
                (MPPConfig) Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.MPP);
            _mppConfig = mppConfig;

            CreateContegoVODmsg cv = new CreateContegoVODmsg(br, dt);
            vodContent = cv.GetContentData();
            //string xmlFilePath = _brokeredMessage.Properties["FileName"].ToString();
            ////Asset asset  = vodContent.Assets.FirstOrDefault();
            //ElementalEncoderTask et = new ElementalEncoderTask(vodContent, xmlFilePath);

        }
    }
}
