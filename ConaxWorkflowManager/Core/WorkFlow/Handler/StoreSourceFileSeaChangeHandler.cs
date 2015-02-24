using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class StoreSourceFileSeaChangeHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {

            // save file to storeage.
            log.Info("OnProcess");
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            var seaChangeConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SeaChange").SingleOrDefault();
            bool useStorage = false;
            bool.TryParse(seaChangeConfig.GetConfigParam("UseSourceStorage"), out useStorage);
            String srcDir = seaChangeConfig.GetConfigParam("SourceStorageDirectory");
            String workDir = systemConfig.GetConfigParam("FileIngestWorkDirectory");

            // save video files
            List<String> filesToMove = new List<String>();
            foreach (Asset asset in content.Assets)
            {
                if (!filesToMove.Contains(asset.Name)) {
                    filesToMove.Add(asset.Name);
                }
                asset.Properties.Add(new Property("SourceFileName", asset.Name));
            }

            var ingestXMLFileNameProperty = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName", StringComparison.OrdinalIgnoreCase));
            if (ingestXMLFileNameProperty != null && !String.IsNullOrEmpty(ingestXMLFileNameProperty.Value))
            {
                filesToMove.Add(ingestXMLFileNameProperty.Value);
            }


            if (useStorage && filesToMove.Count > 0)
            {
                BaseFileIngestHelper FileIngestHelper = new BaseFileIngestHelper();
                log.Debug("Storage should be used for SeaChange, copying files");
                Boolean result = FileIngestHelper.CopyIngestFiles(filesToMove, workDir, srcDir);
                if (!result) {
                    log.Error("Failed to move files to source storage folder.");
                    return new RequestResult(RequestResultState.Failed);
                }
                else {
                    log.Debug("Update MPP assets for content " + content.ID.Value + " with SourceFileName");
                    content = mppWrapper.UpdateContent(content);
                    parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content = content;
                }
            }

            return new RequestResult(RequestResultState.Successful);
        }

        public override void OnChainFailed(RequestParameters parameters)
        {
            log.Debug("OnChainFailed");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            var seaChangeConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SeaChange").SingleOrDefault();
            bool useStorage = false;
            bool.TryParse(seaChangeConfig.GetConfigParam("UseSourceStorage"), out useStorage);
            String srcDir = seaChangeConfig.GetConfigParam("SourceStorageDirectory");
            if (useStorage)
            {
                BaseFileIngestHelper FileIngestHelper = new BaseFileIngestHelper();

                // remove video files
                foreach (Asset asset in content.Assets)
                {
                    var property = asset.Properties.FirstOrDefault(p => p.Type.Equals("SourceFileName", StringComparison.OrdinalIgnoreCase));
                    if (property != null)
                    {
                        try
                        {
                            FileIngestHelper.ReMoveFiles(new List<String> { property.Value }, srcDir);
                        }
                        catch (Exception ex) { }
                    }
                }
            }
        }
    }
}
