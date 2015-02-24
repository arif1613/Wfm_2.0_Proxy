using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using System.Threading;
using MPS.MPP.Auxiliary.CompositeManifestGenerator.Generator.Util;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Network;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database.SQLite;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class UploadFilesToSeaChangeHandler : ResponsibilityHandler
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static SeaChangeServicesWrapper wrapper = new SeaChangeServicesWrapper();

        private IDBWrapper dbwrapper = DBManager.Instance;

        private MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            UNCPathHelper pathHelper = null;
            Impersonation impersonation = null;
            try
            {
                log.Debug("< --------------------------------------------------------------------------------------------------------------------------------->");
                var seaChangeConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0].ObjectID);
                int numberOfTries = 5;
                if (seaChangeConfig.ConfigParams.ContainsKey("NumberOfTriesOnFail") && !String.IsNullOrEmpty(seaChangeConfig.GetConfigParam("NumberOfTriesOnFail")))
                {
                    if (!int.TryParse(seaChangeConfig.GetConfigParam("NumberOfTriesOnFail"), out numberOfTries))
                        log.Error("Error setting NumberOfTriesOnFail, using default");
                }
                MultipleServicePrice price = GetContentPrice(parameters);
                
                ulong workFlowJobId = parameters.CurrentWorkFlowProcess.WorkFlowJobId;
                int tries = dbwrapper.GetNumberOfTries(workFlowJobId, this.GetType().Name);
                if (tries >= numberOfTries)
                {
                    log.Error("Publish failed, to many tries");
                    return new RequestResult(Util.Enums.RequestResultState.Failed, "Publish failed, to many tries");
                }

                // calculate space needed on server, ie source files + images + xml
                // check space left on server.
                String sleepTime = seaChangeConfig.GetConfigParam("SleepTimeInMinutes");
                ulong minutesToSleep = ulong.Parse(sleepTime);
                String spaceThreshhold = "";
                //bool inPercentage = false;
                if (seaChangeConfig.ConfigParams.ContainsKey("DiskSpaceInGBThreshhold") && !String.IsNullOrEmpty(seaChangeConfig.GetConfigParam("DiskSpaceInGBThreshhold")))
                {
                    spaceThreshhold = seaChangeConfig.GetConfigParam("DiskSpaceInGBThreshhold");
                }
                //else if (seaChangeConfig.ConfigParams.ContainsKey("DiskSpacePercentageThreshhold") && !String.IsNullOrEmpty(seaChangeConfig.GetConfigParam("DiskSpacePercentageThreshhold")))
                //{
                //    spaceThreshhold = seaChangeConfig.GetConfigParam("DiskSpacePercentageThreshhold");
                //    inPercentage = true;
                //}
                else
                {
                    throw new Exception("No threshhold specifield, needs to be set in either DiskSpaceInGBThreshhold or DiskSpacePercentageThreshhold");
                }

                String UNCPath = seaChangeConfig.GetConfigParam("UNCPath");
                log.Debug("Mapped drive = " + UNCPath);
                if (seaChangeConfig.ConfigParams.ContainsKey("UsePassword") && bool.Parse(seaChangeConfig.GetConfigParam("UsePassword")))
                {
                    log.Debug("Unlocking UNC path");
                    String userName = seaChangeConfig.GetConfigParam("UserName");
                    String passWord = seaChangeConfig.GetConfigParam("PassWord");
                    if (seaChangeConfig.ConfigParams.ContainsKey("UseImpersonation") && bool.Parse(seaChangeConfig.GetConfigParam("UseImpersonation")))
                    {
                        log.Debug("Using impersonation as login method");
                        String domainName = seaChangeConfig.GetConfigParam("DomainName");
                        log.Debug("Trying to impersonate with username = " + userName + ", password= " + passWord + " with domainName= " + domainName);
                        impersonation = new Impersonation(domainName, userName,  passWord);
                 
                        //if (impersonateUser.ImpersonateValidUser(userName, domainName, passWord))
                        //{
                        //    log.Debug("Impersonation success");
                        //}
                        //else
                        //{
                        //     log.Debug("Impersonation failed");
                        //}
                    }
                    else
                    {
                        pathHelper = new UNCPathHelper(userName, passWord);
                        pathHelper.UnlockUNCPath(UNCPath);
                        log.Debug("UNC path unlocked");
                    }
                }
                int spaceLeftOnServer = wrapper.GetSpaceLeftOnServer(UNCPath, false);
                log.Debug("SpaceLeftOnServer= " + spaceLeftOnServer.ToString());
                if (spaceLeftOnServer < int.Parse(spaceThreshhold))
                {
                    
                    log.Info("Diskspace less then limit, space left = " + wrapper.GetSpaceLeftOnServer(UNCPath, false).ToString() + ", threshHold= " + spaceThreshhold + " setting new NotUntil on WorkFlowJob");
                    DateTime newNotUntil = DateTime.UtcNow.AddMinutes(minutesToSleep);
                    dbwrapper.UpdateWorkFlowJobNotUntil(workFlowJobId, newNotUntil);
                    return new RequestResult(Util.Enums.RequestResultState.Retry, "Publish failed, not enough diskspace, moving job to " + newNotUntil.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                log.Info("Uploading files to SeaChange path");
                //if (wrapper.UploadFilesToServer(parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, UNCPath, price))
                //    log.Debug("Upload to seachange successful");
                //log.Debug("< --------------------------------------------------------------------------------------------------------------------------------->");
                try
                {
                    wrapper.UploadFilesToServer(parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, UNCPath,
                                                price);
                }
                catch (Exception exc)
                {
                    throw;
                }
                log.Info("Upload to seachange successful");
                log.Debug("< --------------------------------------------------------------------------------------------------------------------------------->");

            }
            catch (Exception exc)
            {
                log.Error("Error uploading files to SeaChange", exc);
                throw;
            }
            finally
            {
                if (impersonation != null)
                    impersonation.Dispose();
            }
            ConaxIntegrationHelper.HandlePublishedTo(parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0].ObjectID.Value);
            mppWrapper.UpdateContent(parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content, false);
            return new RequestResult(Util.Enums.RequestResultState.Successful);

        }

        private MultipleServicePrice GetContentPrice(RequestParameters parameters)
        {
            foreach (MultipleServicePrice servicePrice in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0].Prices)
            {
                if (!servicePrice.IsRecurringPurchase.Value)
                {
                    return servicePrice;
                }
            }
            return null;
        }

        public override void OnChainFailed(RequestParameters parameters)
        {

           
        }
    }
}
