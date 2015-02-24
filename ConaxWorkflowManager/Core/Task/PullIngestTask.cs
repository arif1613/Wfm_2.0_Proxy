using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class PullIngestTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private MPPIntegrationServicesWrapper mppWrapper;

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");

            IPullIngestHandler handler = Activator.CreateInstance(System.Type.GetType(TaskConfig.GetConfigParam("PullIngestHandler"))) as IPullIngestHandler;
            log.Debug("Handler type: " + handler.GetType().Name);
            //var sfaSystemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == handler.GetSystemName()).SingleOrDefault();
            //mppWrapper = new MPPIntegrationServicesWrapper(sfaSystemConfig.GetConfigParam("AccountId"));
            mppWrapper = MPPIntegrationServiceManager.InstanceWithActiveEvent;

            IEnumerable<int> externalIds = handler.GetAvailableExternalIds();

            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "MPP").SingleOrDefault();

            ContentSearchParameters csp = new ContentSearchParameters();
            csp.ContentRightsOwner = systemConfig.GetConfigParam("DefaultContentRightsOwner");
            csp.Properties.Add("IngestSource", handler.GetSystemName());
            //List<ContentData> mppContent = mppWrapper.GetContent(csp, true);
            List<ContentData> mppContent = mppWrapper.GetContentFromProperties(csp, true);

            IEnumerable<ContentData> contentWithValidExternalIds = mppContent.Where(x => handler.IsValidExternalId(x.ExternalID));

            //foreach (var item in mppContent.Except(contentWithValidExternalIds))
            //{
            //    log.Debug("Content without valid external id (ContentId : Title): " + item.ID + " : " + item.Name);
            //}

            foreach (var item in handler.GetIdsToProcess(externalIds, contentWithValidExternalIds))
            {
                try
                {
                    log.Debug("Start processing media id " + item.ToString());
                    handler.ProcessFiles(item);
                }
                catch (Exception e)
                {
                    log.Error(e);
                }
            }

            foreach (var item in handler.GetIdsToDelete(externalIds, contentWithValidExternalIds))
            {
                log.Debug("trigger delete for media id " + item.ToString());
                DeleteMedia(item, handler);
            }

            log.Debug("DoExecute End (" + handler.GetType().Name + ")");
        }

        private void DeleteMedia(int externalId, IPullIngestHandler handler)
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "MPP").SingleOrDefault();
            string contentRightsOwner = systemConfig.GetConfigParam("DefaultContentRightsOwner");
            UInt64 servcieObejctID = 0; // find servcieobject id

            string mppExternalId = handler.CreateMPPExternalIdString(externalId);
            ContentData cd = mppWrapper.GetContentDataByExternalID(servcieObejctID, mppExternalId);

            if (cd == null)
            {
                log.Warn("Could not find content with external id " + mppExternalId + " in MPP. Will not delete.");
                return;
            }
            if (cd.ContentRightsOwner.Name != contentRightsOwner)
            {
                log.Warn("Found content with external id " + mppExternalId + " for other CRO in MPP. Will not delete.");
            }

            log.Debug("Setting publish state 'deleted' for content id " + cd.ID);
            cd.PublishInfos[0].PublishState = Util.Enums.PublishState.Deleted;
            try
            {
                mppWrapper.UpdateContent(cd);
            }
            catch (Exception e)
            {
                log.Warn("Could not set state 'deleted' for content id " + cd.ID + " external id is " + mppExternalId, e);
            }
        }
    }
}
