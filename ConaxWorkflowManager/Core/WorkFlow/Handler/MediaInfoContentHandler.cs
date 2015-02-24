using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.MediaInfo;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.IO;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class MediaInfoContentHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Info("OnProcess");
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String workDir = systemConfig.GetConfigParam("FileIngestWorkDirectory");
            //workDir = Path.Combine(workDir, parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content.ContentRightsOwner.Name.ToLower());
            try
            {
                foreach (Asset asset in parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content.Assets)
                {
                    try
                    {
                        log.Debug("Adding info for asset " + asset.Name);
                        String filePath = Path.Combine(workDir, asset.Name);
                        log.Debug("file location = " + filePath);
                        MediaFileInfo mediaFileInfo = MediaInfoHelper.GetMediaInfoForFile(filePath);
                        log.Debug("MediaInfo = height= " + mediaFileInfo.Height.ToString() + ", width = " + mediaFileInfo.Width.ToString() + ", no of languages= " + mediaFileInfo.AudioInfos.Count().ToString() + " no of subtitles = " + mediaFileInfo.SubtitleInfos.Count().ToString());
                        ConaxIntegrationHelper.SetResolution(asset, mediaFileInfo.Width, mediaFileInfo.Height);

                        ConaxIntegrationHelper.AddAudioTrackLanguages(asset, mediaFileInfo.AudioInfos);

                        ConaxIntegrationHelper.AddSubtitleLanguages(asset, mediaFileInfo.SubtitleInfos);

                        ConaxIntegrationHelper.AddDisplayAspectRatio(asset, mediaFileInfo.DisplayAspectRatio);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error when fetching mediainfo, continuing", ex);
                    }
                }
                MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
                mppWrapper.UpdateAssets(parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content);
            }
            catch (Exception exc)
            {
                log.Error("Error when fetching mediainfo, continuing", exc);
            }

            return new RequestResult(RequestResultState.Successful);
        }

        public override void OnChainFailed(RequestParameters parameters)
        {
            log.Debug("OnChainFailed");
            
        }

    }
}
