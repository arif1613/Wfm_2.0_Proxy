using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.Security;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public abstract class ResponsibilityHandler
    {
        protected ResponsibilityHandler successor;

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected RequestParameters requestParameters;

        public ResponsibilityHandler() {}

        public void SetSuccessor(ResponsibilityHandler successor)
        {
            this.successor = successor;
        }

        public RequestResult HandleRequest(RequestParameters parameters)
        {
            try
            {
                requestParameters = parameters;
                WorkFlowProcess newWP = null;

                log.Debug(this.GetType().Name + ": HandleRequest start");
                var wp = parameters.HistoricalWorkFlowProcesses.Where(w => w.MethodName == this.GetType().Name
                                                              && w.State == Util.Enums.WorkFlowProcessState.Successful).SingleOrDefault();
               
                // set CurrentWorkFlowProcess, 
                if (wp == null)
                {
                    // TODO: how to handle existing Processing state? do we know it's still going?

                    // create new wp
                    newWP = new WorkFlowProcess();
                    newWP.MethodName = this.GetType().Name;
                    newWP.TimeStamp = DateTime.UtcNow;
                    newWP.State = Util.Enums.WorkFlowProcessState.Processing;

                    if (parameters.CurrentWorkFlowProcess != null) {
                        // this should be a handler in the chain
                        // copy workflowprocess data from last handler
                        newWP.WorkFlowJobId = parameters.CurrentWorkFlowProcess.WorkFlowJobId;
                        newWP.WorkFlowParameters = parameters.CurrentWorkFlowProcess.WorkFlowParameters;

                        // use this wp
                        parameters.CurrentWorkFlowProcess = newWP;
                    }
                    else {
                        // this should be set on the first handler to be processed in the chain.
                        // get last attempt wp if exist
                        var lastWP = parameters.HistoricalWorkFlowProcesses.Where(w => w.MethodName == this.GetType().Name
                                                                  && w.State != Util.Enums.WorkFlowProcessState.Successful).OrderByDescending(w => w.TimeStamp).FirstOrDefault();

                        if (lastWP != null && lastWP.WorkFlowParameters != null)
                        {   // use parameters from the last attempt
                            newWP.WorkFlowParameters = lastWP.WorkFlowParameters;
                            newWP.WorkFlowJobId = lastWP.WorkFlowJobId;
                            newWP.Message = lastWP.Message;
                        }
                        else 
                        { 
                            var lastAddedWP = parameters.HistoricalWorkFlowProcesses.Where(w => w.State != Util.Enums.WorkFlowProcessState.Successful).OrderByDescending(w => w.TimeStamp).FirstOrDefault();
                           // use parameters from the last added, it should be the latest one.
                           newWP.WorkFlowParameters = lastAddedWP.WorkFlowParameters;
                           newWP.WorkFlowJobId = lastAddedWP.WorkFlowJobId;
                           newWP.Message = lastAddedWP.Message;
                        }
                        // use this wp
                        parameters.CurrentWorkFlowProcess = newWP;
                    }
                }

                RequestResult result = new RequestResult(RequestResultState.Failed);
                if (wp == null)
                {
                    // Write Start state to SQL

                    // start process
                    result = OnProcess(parameters);
                    log.Debug(this.GetType().Name + ":     Processed: " + result.State.ToString("G") + " " + result.Message);


                    if (result.State == RequestResultState.Failed)
                    {
                        parameters.CurrentWorkFlowProcess.State = Util.Enums.WorkFlowProcessState.Failed;
                        parameters.CurrentWorkFlowProcess.Message = result.Message;
                        // Write failed state to MPP/SQL
                        UpdateWorkFlowProcess(parameters);
                    }
                    else if (result.State == RequestResultState.Exception) 
                    {
                        parameters.CurrentWorkFlowProcess.State = Util.Enums.WorkFlowProcessState.Error;
                        parameters.CurrentWorkFlowProcess.Message = result.Message;
                        // write Error state to MPP/SQL
                        UpdateWorkFlowProcess(parameters);
                    }
                    else if (result.State == RequestResultState.Successful)
                    {
                        OnSuccess(parameters);
                        // Write success state to SQL
                        parameters.CurrentWorkFlowProcess.State = Util.Enums.WorkFlowProcessState.Successful;
                    }
                }
                else
                {
                    // already finished.
                    OnAlreadySucceed(parameters);
                    RequestResult requestResult = new RequestResult();
                    requestResult.State = RequestResultState.Successful;
                    result = requestResult;
                    log.Debug(this.GetType().Name + ":     Already finished skip processing again.");
                }
                log.Debug(this.GetType().Name + ": HandleRequest end");

                if (successor != null && result.State == RequestResultState.Successful)
                {   // this handler successs to proccess, 
                    // moving to next handler for proccess.
                    result = successor.HandleRequest(parameters);
                }
                if (successor == null && result.State == RequestResultState.Successful) { 
                    // chain execution is done. no more in chain.
                    OnChainFinished(parameters);

                    // write finish state to MPP
                    UpdateWorkFlowStateInMPP(parameters);
                }
                if (result.State == RequestResultState.Failed ||
                    result.State == RequestResultState.Exception)
                {   // some where in the workflow failed, do somehting!
                    OnChainFailed(parameters);
                }
                if (result.State == RequestResultState.Revoke)
                {   // some where in the workflow revoked. revert stuff if needed.
                    OnChainRevoked(parameters);
                }
                if (result.State == RequestResultState.Retry)
                {
                    OnChainRetry(parameters);
                }

                return result;
            }
            catch (Exception e)
            {
                RequestResult requestResult = new RequestResult();
                requestResult.State = RequestResultState.Exception;
                requestResult.Message = e.Message;
                requestResult.Ex = e;

                parameters.CurrentWorkFlowProcess.State = Util.Enums.WorkFlowProcessState.Error;
                parameters.CurrentWorkFlowProcess.Message = e.Message;
                // write Error state to MPP/SQL
                UpdateWorkFlowProcess(parameters);

                log.Error(this.GetType().Name + ":     Exception from handler " + this.GetType().Name + ": " + e.Message, e);
                log.Debug(this.GetType().Name + ": HandleRequest end");

                // some where in the workflow failed, do somehting!
                try
                {
                    OnChainFailed(parameters);
                } catch(Exception ex) {
                    log.Warn("Exception on " + this.GetType().Name + " - OnChainFailed: " + ex.Message, ex);
                }
                return requestResult;
            }
        }

        private void OnChainRetry(RequestParameters parameters)
        {
            
        }

        public abstract RequestResult OnProcess(RequestParameters parameters);

        public virtual void OnChainFailed(RequestParameters parameters)
        {
            // roll back stuff when the chain breaks.
        }

        public virtual void OnChainFinished(RequestParameters parameters)
        {
        }

        public virtual void OnFailed(RequestParameters parameters)
        {
        }

        public virtual void OnException(RequestParameters parameters)
        {
        }

        public virtual void OnChainRevoked(RequestParameters parameters)
        {
        }

        public virtual void OnSuccess(RequestParameters parameters)
        {
        }

        public virtual void OnAlreadySucceed(RequestParameters parameters)
        {
        }

      private void UpdateWorkFlowProcess(RequestParameters parameters)
        {
            if (parameters.CurrentWorkFlowProcess.State == WorkFlowProcessState.Error)
                OnException(parameters);
            if (parameters.CurrentWorkFlowProcess.State == WorkFlowProcessState.Failed)
                OnFailed(parameters);

            // write state to MPP
            UpdateWorkFlowStateInMPP(parameters);
            // write state to SQL
        }

        protected Int32 NonSuccessfulAttemps()
        {
            Int32 tries = requestParameters.HistoricalWorkFlowProcesses.Count(w => w.MethodName == this.GetType().Name
                                                                  && w.Id != requestParameters.CurrentWorkFlowProcess.Id
                                                                  && w.State != Util.Enums.WorkFlowProcessState.Successful);
            return tries;
        }

        public void UpdateWorkFlowStateInMPP(RequestParameters parameters)
        {
            
            try
            {
                MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

                String workflowState = "";
                workflowState += "{ ";
                workflowState += "[TimeStamp]: [" + parameters.CurrentWorkFlowProcess.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "], ";
                workflowState += "[WorkFlow]: [" + parameters.Action.ToString("G") + "], ";
                workflowState += "[State]: [" + parameters.CurrentWorkFlowProcess.State.ToString("G") + "], ";
                workflowState += "[Handler]: [" + parameters.CurrentWorkFlowProcess.MethodName + "], ";
                workflowState += "[Message]: [" + parameters.CurrentWorkFlowProcess.Message + "] ";
                workflowState += " }";

                workflowState = SecurityElement.Escape(workflowState);

                if (parameters.Action == WorkFlowType.UpdateVODContent ||
                    parameters.Action == WorkFlowType.AddVODContent)
                {
                    CommonUtil.SetWorkFlowState(parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, workflowState);
                    mppWrapper.UpdateContent(parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, false);
                }

                if (parameters.Action == WorkFlowType.UpdateServicePrice)
                {
                    foreach(MultipleContentService service in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices) {
                        CommonUtil.SetWorkFlowState(service.Prices[0], workflowState);
                        mppWrapper.UpdateServicePrice(service.Prices[0]);
                    }
                }
            }
            catch (Exception ex) {
                log.Warn("Failed to update work flow state in MPP", ex);
            }
        }
    }
}
