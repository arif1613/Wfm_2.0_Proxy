using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Harmonic;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class DeleteContentFromHarmonicOriginHandler : ResponsibilityHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            log.Debug("Initializing deletion from Harmonic Origin for content with name= " + content.Name + " and objectID= " + content.ObjectID.Value);

            IHarmonicOriginWrapper originWrapper = HarmonicOriginWrapperManager.Instance;

            try
            {
                log.Debug("Calling Harmonic Origin Api for Delete");
                if (originWrapper.DeleteAssetsFromOrigin(content))
                {
                    log.Debug("Call to Delete content was successful");
                }
                else
                {
                    log.Warn("Call to delete content failed");
                }
            }
            catch (Exception e)
            {
                log.Warn("Error when deleting from Harmonic Origin for content with name " + content.Name, e);
                //return false;
            }

            return new RequestResult(Util.Enums.RequestResultState.Successful);
        }
    }
}
