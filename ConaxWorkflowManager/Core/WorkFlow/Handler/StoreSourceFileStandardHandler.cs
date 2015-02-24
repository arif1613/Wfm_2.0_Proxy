using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class StoreSourceFileStandardHandler : ResponsibilityHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            // save file to storeage.
            log.Info("OnProcess");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
            String srcDir = systemConfig.SourceStorageDirectory;
            String workDir = systemConfig.FileIngestWorkDirectory;
            BaseFileIngestHelper FileIngestHelper = new BaseFileIngestHelper();
            
            // save image
            List<String> filesToMove = new List<String>();
            foreach(LanguageInfo lang in content.LanguageInfos) {
                foreach(Image img in lang.Images) {
                    if (!filesToMove.Contains(img.URI))
                        filesToMove.Add(img.URI);
                }
            }

            if (filesToMove.Count > 0) {
                Boolean result = FileIngestHelper.CopyIngestFiles(filesToMove, workDir, srcDir);
                if (!result) {
                    log.Error("Failed to move fiels to source storage folder.");
                    return new RequestResult(RequestResultState.Failed);
                }
            }

            return new RequestResult(RequestResultState.Successful);
        }

        public override void OnChainFailed(RequestParameters parameters)
        {
            log.Debug("OnChainFailed");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
            String srcDir = systemConfig.SourceStorageDirectory;
            BaseFileIngestHelper FileIngestHelper = new BaseFileIngestHelper();

            // remove image
            foreach (LanguageInfo lang in content.LanguageInfos)
            {
                foreach (Image img in lang.Images)
                {
                    try
                    {
                        if (File.Exists(Path.Combine(srcDir, img.URI)))
                            FileIngestHelper.ReMoveFiles(new List<String> { img.URI }, srcDir);
                    }
                    catch (Exception ex) { }
                }
            }
        }
    }
}
