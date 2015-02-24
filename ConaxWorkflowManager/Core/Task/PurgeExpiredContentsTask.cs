using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class PurgeExpiredContentsTask : BaseTask
    {

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithActiveEvent;

        public override void DoExecute()
        {
            log.Debug("DoExecute for PurgeExpiredContent");
            try
            {
                List<ContentRightsOwner> CROs = mppWrapper.GetContentRightsOwners();
                
                List<ContentData> contents = new List<ContentData>();
                foreach (ContentRightsOwner cro in CROs)
                {
                    ContentSearchParameters searchParameters = new ContentSearchParameters();
                    searchParameters.ContentRightsOwner = cro.Name;
                    searchParameters.EventPeriodTo = DateTime.UtcNow;
                    searchParameters.Properties.Add("ContentType", ContentType.VOD.ToString("G"));
                    log.Debug("Fetching expired contents");
                    //List<ContentData> contentToPurge = mppWrapper.GetContent(searchParameters, true);
                    List<ContentData> contentToPurge = mppWrapper.GetContentFromProperties(searchParameters, true);
                    contents.AddRange(contentToPurge);
                }
                foreach (ContentData content in contents)
                {
                    try
                    {
                        log.Debug("Setting all publishinginfos on content " + content.Name + " to deleted");
                        foreach (PublishInfo pi in content.PublishInfos)
                            pi.PublishState = PublishState.Deleted;
                        //mppWrapper.UpdateContent(content);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error purging content with name " + content.Name + " continuing with next", ex);
                    }
                }
                mppWrapper.UpdateContentsInChunks(contents);
            }
            catch (Exception exc)
            {
                log.Error("Error purging contents", exc);
            }
        }
    }
}
