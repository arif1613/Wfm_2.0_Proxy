using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class DeleteSourceFileStandardHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
            String srcDir = systemConfig.SourceStorageDirectory;
            BaseFileIngestHelper FileIngestHelper = new BaseFileIngestHelper();

            // remove image
            foreach (LanguageInfo lang in content.LanguageInfos) {
                foreach (Image img in lang.Images) {
                    try {
                        FileIngestHelper.ReMoveFiles(new List<String> { img.URI }, srcDir);
                    }
                    catch (Exception ex) { }
                }
            }

            return new RequestResult(RequestResultState.Successful);
        }
    }
}
