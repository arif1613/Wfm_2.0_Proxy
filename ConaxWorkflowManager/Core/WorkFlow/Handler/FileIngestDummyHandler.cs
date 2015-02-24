using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    /// <summary>
    /// This Handler is only for deleting content in MPP when Rollback for a file ingest workflow.
    /// </summary>
    public class FileIngestDummyHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {   // do nothing here,
            return new RequestResult(RequestResultState.Successful);
        }

        public override void OnChainFailed(RequestParameters parameters)
        {
            log.Debug("OnChainFailed");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;

            var contentTypeProperty = content.Properties.FirstOrDefault(p => p.Type.Equals("Contenttype", StringComparison.OrdinalIgnoreCase));
            if (contentTypeProperty == null)
                return;

            ContentType contentType = (ContentType)Enum.Parse(typeof(ContentType), contentTypeProperty.Value, true);

            // check type
            log.Debug("check contentType " + contentType.ToString() + " before remove from MPP.");
            switch (contentType)
            {
                case ContentType.CatchupTV:
                    // do nothing. let it go
                    break;
                case ContentType.Live:
                case ContentType.VOD:
                case ContentType.Channel:
                    // check if it's XML ingest
                    var ingestXMLFileNameProperty = content.Properties.FirstOrDefault(p => p.Type.Equals("IngestXMLFileName", StringComparison.OrdinalIgnoreCase));
                    if (ingestXMLFileNameProperty == null)
                    {
                        log.Debug("content " + content.ID + " " + content.Name + " doesn't have IngestXMLFileName property, it's not a xml file ingest, skip the rollback");
                        return;
                    }
                    break;
                case ContentType.NotSpecified:
                    log.Debug("content " + content.ID + " " + content.Name + " for this content type " + contentType.ToString() + " will be not removed, skip the rollback");
                    return;
                default:
                    return;
            }

            // start delete
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

            foreach (MultipleContentService service in services)
            {
                foreach (MultipleServicePrice MSP in service.Prices)
                {
                    if (MSP.IsRecurringPurchase.HasValue && !MSP.IsRecurringPurchase.Value)
                    {   // only delete content prices.
                        try
                        {
                            log.Debug("Start to delete price " + MSP.ID.Value + " " + MSP.Title);
                            mppWrapper.DeleteServicePrice(MSP);
                        }
                        catch (Exception ex)
                        {
                            log.Warn("Failed to delete price " + MSP.ID.Value + " " + MSP.Title);
                        }
                    }
                }
            }

            if (content.ID.HasValue)
            {
                try
                {
                    log.Debug("Start deleting content " + content.ID + " " + content.Name);
                    mppWrapper.DeleteContent(content);
                }
                catch (Exception ex)
                {
                    log.Warn("Failed to delete content " + content.Name + " " + content.ID.Value);
                }
            }
        }
    }
}
