using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class UpdateContentInCubiTVHandler : ResponsibilityHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            
            log.Debug("OnProcess");
            MultipleContentService service = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0];
            ICubiTVMWServiceWrapper wrapper = CubiTVMiddlewareManager.Instance(service.ObjectID.Value);
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            var workFlowConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            var serviceConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == service.ObjectID.Value);
            if (!serviceConfig.ConfigParams.ContainsKey("VodServiceID") || String.IsNullOrEmpty(serviceConfig.GetConfigParam("VodServiceID")))
            {
                throw new Exception("Couldn't find VodServiceID in serviceConfig");
            }
            String vodServiceID = serviceConfig.GetConfigParam("VodServiceID");
            bool createCategoryIfNotExists = true;
            if (workFlowConfig.ConfigParams.ContainsKey("CreateCategoryIfNotExists") && !String.IsNullOrEmpty(workFlowConfig.GetConfigParam("CreateCategoryIfNotExists")))
            {
                bool.TryParse(workFlowConfig.GetConfigParam("CreateCategoryIfNotExists"), out createCategoryIfNotExists);
            }

            ContentData updatedContent = null;
            log.Debug("Updating content with name " + content.Name + " and ID = " + content.ID);
            try
            {
                String cubiTVContentID = ConaxIntegrationHelper.GetCubiTVContentID(service.ObjectID.Value, content);
                if (!String.IsNullOrEmpty(cubiTVContentID))
                {
                    updatedContent = wrapper.UpdateContent(ulong.Parse(cubiTVContentID), content, service.ObjectID.Value, serviceConfig, createCategoryIfNotExists);
                }
                else
                {
                    string message = "No CubiTV contentID was found for content with name " + content.Name + " and ID = " + content.ID;
                    log.Error(message);
                    return new RequestResult(RequestResultState.Failed, message);
                }
            }
            catch (Exception e)
            {
                log.Error("Error when updating content in CubiTV", e);
                return new RequestResult(RequestResultState.Exception, e);
            }
            //if (!mppWrapper.UpdateContent(content))
            //{
            //    log.Error("Error when updating content in MPP");
            //    return false;
            //}


            //IList<MultipleServicePrice> servicePrices = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleServicePrices;

            return new RequestResult(RequestResultState.Successful);
        }
    }
}
