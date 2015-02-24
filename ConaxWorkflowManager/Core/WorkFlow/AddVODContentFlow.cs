using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using System.Diagnostics;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow
{

    public class AddVODContentFlow : BaseFlow
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /*
        public AddVODContentFlow(TaskConfig contextConfig)
            : base(contextConfig) {}
        */
        public override RequestResult Process(RequestParameters requestParameters)
        {
            log.Debug("In process");
            try
            {
                //RequestResult result = GCFMPPHandler.HandleRequest(requestParameters);
                RequestResult result = HandleRequest(requestParameters);
                if (result.State == RequestResultState.Failed ||
                    result.State == RequestResultState.Exception)
                {
                    try
                    {
                        RejectIngest(requestParameters);
                    }
                    catch (Exception moveEx) {
                        log.Warn("Failed to move files to reject folder.", moveEx);
                    }
                    try
                    {
                        if (result.State == RequestResultState.Exception)
                        {
                            CommonUtil.SendFailedVODIngestNotification(requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, result.Ex);
                        }
                        else // state = failed
                        {
                            CommonUtil.SendFailedVODIngestNotification(requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, result.Message);
                        }
                    }
                    catch (Exception mailex) {
                        log.Warn("Failed to send Notification.", mailex);
                    }
                }
                else if (result.State == RequestResultState.Successful) {
                    try
                    {
                        ProcessedIngest(requestParameters);
                    }
                    catch (Exception moveEx) {
                        log.Warn("Failed to move files to proccessed folder.", moveEx);
                    }
                    try
                    {
                        CommonUtil.SendSuccessfulVODIngestNotification(requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content);
                    }
                    catch (Exception mailex) {
                        log.Warn("Failed to send Notification.", mailex);
                    }
                }

                return result;
            } catch (Exception ex) {
                try
                {
                    RejectIngest(requestParameters);
                }
                catch (Exception moveEx) {
                    log.Warn("Failed to move files to reject folder.", moveEx);
                }
                try
                {
                    CommonUtil.SendFailedVODIngestNotification(requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, ex);
                }
                catch (Exception mailex) {
                    log.Warn("Failed to send Notification.", mailex);
                }
                throw;
            }
            // Send notification (mail)

        }

        private void ProcessedIngest(RequestParameters requestParameters) {

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String processedDir = systemConfig.GetConfigParam("FileIngestProcessedDirectory");
            MoveIngestFromWorkTo(requestParameters, processedDir);
        }

        private void RejectIngest(RequestParameters requestParameters)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String rejectDir = systemConfig.GetConfigParam("FileIngestRejectDirectory");
            MoveIngestFromWorkTo(requestParameters, rejectDir);
        }

        private void MoveIngestFromWorkTo(RequestParameters requestParameters, String destDir) {

            ContentData content = requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            var ingestXMLFileNameProperty = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName", StringComparison.OrdinalIgnoreCase));
            if (ingestXMLFileNameProperty == null)
            {
                log.Debug("This content doesn't have IngestXMLFileName property no import files to move.");
                return;
            }

            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
            String workDir = systemConfig.FileIngestWorkDirectory;
            workDir = Path.Combine(workDir, Path.GetDirectoryName(ingestXMLFileNameProperty.Value));
            String toDir = destDir;
            toDir = Path.Combine(toDir, Path.GetDirectoryName(ingestXMLFileNameProperty.Value));

            try
            {
                String ingestXmlPath = Path.Combine(workDir, Path.GetFileName(ingestXMLFileNameProperty.Value));
                IngestXMLType ingestXmlType = CommonUtil.GetIngestXMLType(ingestXmlPath);
                var ingestXMLConfig = Config.GetConfig().IngestXMLConfigs.SingleOrDefault(i => i.IngestXMLType.Equals(ingestXmlType.ToString(), StringComparison.OrdinalIgnoreCase));

                BaseIngestFileIngestHelper FileIngestHelper = Activator.CreateInstance(System.Type.GetType(ingestXMLConfig.FileIngestHelper)) as BaseIngestFileIngestHelper;
                FileIngestHelper.MoveIngestFiles(Path.GetFileName(ingestXMLFileNameProperty.Value), workDir, toDir);

                //cehck default img
                List<String> imgs = new List<String>();
                foreach(LanguageInfo lang in content.LanguageInfos) {
                    foreach(Image img in lang.Images) {
                        String imgfile = Path.Combine(workDir, Path.GetFileName(img.URI));
                        if (File.Exists(imgfile))
                            imgs.Add(Path.GetFileName(img.URI));
                    }
                }
                if (imgs.Count > 0)
                    FileIngestHelper.MoveIngestFiles(imgs, workDir, toDir);
            }
            catch (Exception ex)
            {
                log.Warn("Failed to move ingest files for " + Path.GetFileName(ingestXMLFileNameProperty.Value) + " from " + workDir + " to " + toDir);
            }
        }
    }
}
