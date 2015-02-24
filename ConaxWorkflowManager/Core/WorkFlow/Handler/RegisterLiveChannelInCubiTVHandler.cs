using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.Reflection;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class RegisterLiveChannelInCubiTVHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        ICubiTVMWServiceWrapper wrapper = null;
        MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Debug("OnProcess");
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;

            foreach (MultipleContentService service in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices)
            {
                log.Debug("Hadling service with id " + service.ID.Value.ToString());
                wrapper = CubiTVMiddlewareManager.Instance(service.ObjectID.Value);

                ContentData newContent = null;
                log.Debug("Handling cover");
                String coverID =  HandleContentCoverFile(content, service);
                log.Debug("Cover handled, id= " + coverID);
                try
                {
                    String existingCubiTVObjectID = ConaxIntegrationHelper.GetCubiTVContentID(service.ObjectID.Value, content);
                    log.Debug("ExistingCubiTV = " + existingCubiTVObjectID);
                    if (String.IsNullOrEmpty(existingCubiTVObjectID)) // if job is in retry state it is possible that content already is registered in cubiTV
                    {
                        log.Debug("no channel existed, create");
                        newContent = wrapper.CreateLiveChannel(content);
                        log.Debug("Channel created, id = " + newContent.ID.Value.ToString());
                        content = mppWrapper.UpdateContent(content); // update Content in MPP
                    }
                }
                catch (Exception e)
                {
                    log.Error("Error when creating content in CubiTV", e);
                    return new RequestResult(RequestResultState.Exception, e);
                }

                foreach (MultipleServicePrice servicePrice in service.Prices)
                {
                    log.Debug("Going through prices");
                    log.Debug("Fetching coverID");
                    coverID = ConaxIntegrationHelper.GetCubiTVPriceCoverID(servicePrice);
                    log.Debug("CoverId= " + coverID);
                    if (String.IsNullOrWhiteSpace(coverID)) {
                        log.Debug("coverID doesn't exist, fetching");
                        coverID = ConaxIntegrationHelper.GetCubiTVContentCoverID(content, service.ObjectID.Value);
                        log.Debug("CoverID = " + coverID);
                        ConaxIntegrationHelper.SetCubiTVPriceCoverID(servicePrice, coverID);
                    }
                    if (servicePrice.IsRecurringPurchase.Value)
                    {

                        try
                        {
                            log.Debug("Adding channel to price");
                            wrapper.AddContentToPackageOffer(servicePrice, content);
                        }
                        catch (Exception e)
                        {
                            log.Error("Something went wrong when creating servicePrice", e);
                            return new RequestResult(RequestResultState.Exception, e);
                        }
                    }
                    else
                    {
                        String existingPriceID = ConaxIntegrationHelper.GetCubiTVOfferID(servicePrice);
                        log.Debug("Existing price = " + existingPriceID);
                        if (String.IsNullOrEmpty(existingPriceID))
                        {
                            try
                            {
                                log.Debug("handling contentPrice");
                                wrapper.HandleContentPrice(servicePrice, newContent);
                            }
                            catch (Exception e)
                            {
                                log.Error("Something went wrong when creating contentPrice", e);
                                return new RequestResult(RequestResultState.Exception, e);
                            }
                        }
                    }

                }

                ConaxIntegrationHelper.HandlePublishedTo(content, service.ObjectID.Value);
                mppWrapper.UpdateContent(content, false);
                
            }
            return new RequestResult(RequestResultState.Successful);
        }

        public override void OnChainFailed(RequestParameters parameters)
        {
            log.Debug("OnChainFailed");
            List<MultipleContentService> services = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices;
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            foreach(MultipleContentService service in services) {

                wrapper = CubiTVMiddlewareManager.Instance(service.ObjectID.Value);
                String cubiTVContentID = ConaxIntegrationHelper.GetCubiTVContentID(service.ObjectID.Value, content);
                if (!String.IsNullOrEmpty(cubiTVContentID))
                {
                    // delete catchup channel first if it exist
                    string cuID = ConaxIntegrationHelper.GetCubiTVCatchUpId(service.ObjectID.Value, content);
                    if (!string.IsNullOrEmpty(cuID))
                    {
                        if (!wrapper.DeleteChannel(ulong.Parse(cuID)))
                        {
                            log.Error("Couldnt delete catchup channel with GetCubiCatchUpId = " + cubiTVContentID);
                        }
                    }

                    if (!wrapper.DeleteChannel(ulong.Parse(cubiTVContentID)))
                    {
                        log.Error("Couldnt delete channel with CubiTVContentID = " + cubiTVContentID);
                    }
                
                    // Do nothing with the price for now...

                    //foreach (MultipleServicePrice price in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleServicePrices)
                    //{
                    //    if (price.IsRecurringPurchase.Value)
                    //    {
                    //        String priceID = ConaxIntegrationHelper.GetCubiTVOfferID(price);
                    //        if (!String.IsNullOrEmpty(priceID))
                    //        {
                    //            if (!wrapper.DeleteSubscriptionPrice(ulong.Parse(priceID)))
                    //            {
                    //                log.Error("Couldnt delete subscriptionPrice with priceID = " + priceID);
                    //            }
                    //        }
                    //    }
                    //}
                }
            }
            DeletePropertiesFromContent(content);
        }

        private void DeletePropertiesFromContent(ContentData contentData)
        {
            List<UpdatePropertiesForContentParameter> updates = new List<UpdatePropertiesForContentParameter>();
            UpdatePropertiesForContentParameter param = new UpdatePropertiesForContentParameter();
            param.Content = contentData;
            
            foreach (
                Property property in
                    contentData.Properties.Where(p => p.Type.Equals(VODnLiveContentProperties.CubiCatchUpId)))
            {
                param.Properties.Add(new KeyValuePair<string, Property>("DELETE", property));
            }
            foreach (
               Property property in
                   contentData.Properties.Where(p => p.Type.Equals("CubiNPVRId")))
            {
                param.Properties.Add(new KeyValuePair<string, Property>("DELETE", property));
            }
            foreach (
              Property property in
                  contentData.Properties.Where(p => p.Type.Equals(VODnLiveContentProperties.ServiceExtContentID)))
            {
                param.Properties.Add(new KeyValuePair<string, Property>("DELETE", property));
            }
            foreach (
               Property property in
                   contentData.Properties.Where(p => p.Type.Equals(SystemContentProperties.PublishedToService)))
            {
                param.Properties.Add(new KeyValuePair<string, Property>("DELETE", property));
            }

            mppWrapper.UpdateContentsPropertiesInChunks(updates);
        }

        private string HandleContentCoverFile(ContentData content, MultipleContentService service)
        {
            // same as for RegisterContentInCubiTVHandler? move to helper class in Util...?
            try
            {
                String existingCoverID = ConaxIntegrationHelper.GetCubiTVContentCoverID(content, service.ObjectID.Value); // TODO!, fix real serviceObjectID
                if (String.IsNullOrEmpty(existingCoverID)) // if job is in retry state it is possible that cover already is ingested.
                {
                    log.Debug("in HandleContentCoverFile");
                    Dictionary<String, List<Image>> images = new Dictionary<string, List<Image>>();
                    String keyForImageToUse = "";
                    var info = content.LanguageInfos.FirstOrDefault(l => l.ISO.Equals(service.ServiceViewMatchRules[0].ServiceViewLanugageISO, StringComparison.OrdinalIgnoreCase));
                    
                    foreach (Image image in info.Images)
                    {
                        if (!images.ContainsKey(image.ClientGUIName))
                        {
                            List<Image> i = new List<Image>();
                            i.Add(image);
                            images.Add(image.ClientGUIName, i);
                            keyForImageToUse = image.ClientGUIName;
                        }
                        else
                        {
                            images[image.ClientGUIName].Add(image);
                        }
                    }
                    
                    Image coverImage = null;
                    FileInfo fileInfo = null;
                    if (images.Count > 0)
                    {
                        log.Debug("found " + images.Count.ToString() + " images");
                        var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
                        String storageDir = systemConfig.SourceStorageDirectory;
                        coverImage = images[keyForImageToUse][0];
                        fileInfo = new FileInfo(Path.Combine(storageDir, coverImage.URI));
                        log.Debug("full path to image = " + fileInfo.FullName);
                    }
                    else
                    {
                        throw new Exception("No images found for content with name = " + content.Name + " and id = " + content.ID);
                    }
                    int id = wrapper.CreateCover(coverImage, fileInfo);
                    log.Debug("Cover created, id= " + id.ToString());
                    ConaxIntegrationHelper.SetCubiTVContentCoverID(service.ObjectID.Value, content, id.ToString());
                    return id.ToString();
                }
                else
                {
                    return existingCoverID;
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong handling images", ex);
                throw;
            }
        }
    }
}
