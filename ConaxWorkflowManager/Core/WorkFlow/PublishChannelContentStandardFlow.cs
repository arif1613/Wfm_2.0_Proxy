using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow
{
    public class PublishChannelContentStandardFlow : BaseFlow
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult Process(RequestParameters requestParameters)
        {
            log.Debug("In process");
            try
            {
                RequestResult result = HandleRequest(requestParameters);

                ContentData content = requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
                MultipleContentService service = new MultipleContentService();
                service.Name = "NO Service";  // in case somehow the servcie is broken
                service.ID = 0;
                if (requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices.Count != 0)
                    service = requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0];


                if (result.State == RequestResultState.Failed ||
                    result.State == RequestResultState.Exception)
                {
                    try
                    {
                        CommonUtil.SendFailedVODPublishNotification(content, service, result);
                    }
                    catch (Exception mailex)
                    {
                        log.Warn("Failed to send Notification.", mailex);
                    }
                }
                else if (result.State == RequestResultState.Successful)
                {
                    try
                    {
                        CommonUtil.SendSuccessfulVODPublishNotification(content, service);
                    }
                    catch (Exception mailex)
                    {
                        log.Warn("Failed to send Notification.", mailex);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                try
                {
                    ContentData content = requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
                    MultipleContentService service = new MultipleContentService();
                    service.Name = "NO Service";  // in case somehow the servcie is broken
                    service.ID = 0;
                    if (requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices.Count != 0)
                        service = requestParameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0];

                    CommonUtil.SendFailedVODPublishNotification(content, service, ex);
                }
                catch (Exception mailex)
                {
                    log.Warn("Failed to send Notification.", mailex);
                }
                throw;
            }
        }
    }
}
