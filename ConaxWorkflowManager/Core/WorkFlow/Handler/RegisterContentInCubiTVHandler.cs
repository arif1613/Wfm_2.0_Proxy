using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using System.Collections;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WorkFlow.Handler
{
    public class RegisterContentInCubiTVHandler : ResponsibilityHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public override RequestResult OnProcess(RequestParameters parameters)
        {
            log.Info("OnProcess");


            ValidatorResult mappingResult = ArePropertiesMapped(parameters);
            
            if (!mappingResult.IsOK)
            {
                log.Error("Content failed at data mapping validation, " + mappingResult.Message);
                return new RequestResult(RequestResultState.Failed, mappingResult.Message);
            }
            
            ValidatorResult validationResult = IsContentDataValidInCubiTV(parameters);
            if (!validationResult.IsOK)
            {
                log.Error("Content failed data validation, " + validationResult.Message);
                return new RequestResult(RequestResultState.Failed, validationResult.Message);
            }

            
            ulong serviceObjectID = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0].ObjectID.Value;

            MultipleContentService contentService = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0];
            ICubiTVMWServiceWrapper wrapper = CubiTVMiddlewareManager.Instance(contentService.ObjectID.Value);
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            ContentData newContent = null;

            var workFlowConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            var serviceConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == serviceObjectID);
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
            String coverID = HandleContentCoverFile(content, serviceObjectID, wrapper);


            try
            {
                String existingCubiTVObjectID = ConaxIntegrationHelper.GetCubiTVContentID(contentService.ObjectID.Value, content);
              
                if (String.IsNullOrEmpty(existingCubiTVObjectID)) // if job is in retry state it is possible that content already is registered in cubiTV
                {
                    ConaxIntegrationHelper.SetCubiTVContentCoverID(serviceObjectID, content, coverID);
                    newContent = wrapper.CreateContent(content, serviceObjectID, serviceConfig, createCategoryIfNotExists);
                    content = mppWrapper.UpdateContent(content);
                }
            }
            catch (Exception e)
            {
                log.Error("Error when creating content in CubiTV", e);
                return new RequestResult(RequestResultState.Exception, e);
            }




            foreach (MultipleContentService service in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices)
            {
                foreach (MultipleServicePrice servicePrice in service.Prices)
                {
                    ConaxIntegrationHelper.SetCubiTVPriceCoverID(servicePrice, coverID);
                    if (servicePrice.IsRecurringPurchase.Value)
                    {

                        try
                        {
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
                        if (String.IsNullOrEmpty(existingPriceID))
                        {
                            try
                            {
                                wrapper.HandleContentPrice(servicePrice, content);
                            }
                            catch (Exception e)
                            {
                                log.Error("Something went wrong when creating contentPrice", e);
                                return new RequestResult(RequestResultState.Exception, e);
                            }
                        }
                    }

                }
            }
            ConaxIntegrationHelper.HandlePublishedTo(content, serviceObjectID);
            mppWrapper.UpdateContent(content, false);
            return new RequestResult(RequestResultState.Successful);
        }

        

        public override void OnChainFailed(RequestParameters parameters)
        {
            log.Debug("OnChainFailed");
            MultipleContentService contentService = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0];
            ICubiTVMWServiceWrapper wrapper = CubiTVMiddlewareManager.Instance(contentService.ObjectID.Value);
            String cubiTVContentID = ConaxIntegrationHelper.GetCubiTVContentID(contentService.ObjectID.Value, parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content);
            if (!String.IsNullOrEmpty(cubiTVContentID))
            {
                if (!wrapper.DeleteContent(ulong.Parse(cubiTVContentID)))
                {
                    log.Error("Couldnt delete content with CubiTVContentID = " + cubiTVContentID);
                }
                foreach (MultipleContentService service in parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices)
                {
                    foreach (MultipleServicePrice price in service.Prices)
                    {
                        if (!price.IsRecurringPurchase.Value) // only delete contentprices for now
                        {
                            String priceID = ConaxIntegrationHelper.GetCubiTVOfferID(price);
                            if (!String.IsNullOrEmpty(priceID))
                            {
                                if (!wrapper.DeleteContentPrice(ulong.Parse(priceID)))
                                {
                                    log.Error("Couldnt delete contentPrice with priceID = " + priceID);
                                }
                            }
                        }
                    }
                }
            }
            DeletePropertiesFromContent(parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content);

        }

        private void DeletePropertiesFromContent(ContentData contentData)
        {
            List<UpdatePropertiesForContentParameter> updates = new List<UpdatePropertiesForContentParameter>();
            UpdatePropertiesForContentParameter param = new UpdatePropertiesForContentParameter();
            param.Content = contentData;
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

        private String HandleContentCoverFile(ContentData content, ulong serviceObjectID, ICubiTVMWServiceWrapper wrapper)
        {
            try
            {
                String existingCoverID = ConaxIntegrationHelper.GetCubiTVContentCoverID(content, serviceObjectID);
                if (String.IsNullOrEmpty(existingCoverID)) // if job is in retry state it is possible that cover already is ingested.
                {
                    log.Debug("in HandleContentCoverFile");
                    Dictionary<String, List<Image>> images = new Dictionary<string, List<Image>>();
                    String keyForImageToUse = "";
                    foreach (LanguageInfo info in content.LanguageInfos)
                    {
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
                    }
                    Image coverImage = null;
                    FileInfo fileInfo = null;
                    if (images.Count > 0)
                    {
                        log.Debug("found " + images.Count.ToString() + " images");
                        var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
                        String sourceStorageFolder = systemConfig.GetConfigParam("SourceStorageDirectory");
                        coverImage = images[keyForImageToUse][0];
                        fileInfo = new FileInfo(Path.Combine(sourceStorageFolder, coverImage.URI));
                        log.Debug("full path to image = " + fileInfo.FullName);
                    }
                    else
                    {
                        throw new Exception("No images found for content with name = " + content.Name + " and id = " + content.ID);
                    }
                    int id = wrapper.CreateCover(coverImage, fileInfo);
                    log.Debug("Cover created, id= " + id.ToString());
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
                return "";
            }
            
            return "";
        }


        public ValidatorResult IsContentDataValidInCubiTV(RequestParameters parameters)
        {
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            String propertyName = "";
            List<String> l = new List<String>();
            String value = "";
            ulong serviceObjectID = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0].ObjectID.Value;
            var serviceConfig = Config.GetConfig().ServiceConfigs.FirstOrDefault(s => s.ServiceObjectId == serviceObjectID);
            if (!serviceConfig.ConfigParams.ContainsKey("VodServiceID") || String.IsNullOrEmpty(serviceConfig.GetConfigParam("VodServiceID")))
            {
                throw new Exception("Couldn't find VodServiceID in serviceConfig");
            }
            String vodServiceID = serviceConfig.GetConfigParam("VodServiceID");

            if (String.IsNullOrEmpty(GetTitle(content)) || GetTitle(content).Length >= 256)
            {
                return new ValidatorResult(false, "Datavalidation for CubiTV failed, Name is empty or to long, maximum length is 64");
            }
            try
            {
                //var ContentPropertyConfig = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "ContentProperty").SingleOrDefault();
                propertyName = VODnLiveContentProperties.Cast;
                var properties = content.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                l = new List<String>();
                foreach (Property p in properties)
                {
                    l.Add(p.Value);
                }
                value = String.Join(",", l.ToArray());
                if (value.Length >= 1025)
                {
                    return new ValidatorResult(false, "Datavalidation for CubiTV failed, Cast is to long, maximum length is 1024");
                }

                propertyName = VODnLiveContentProperties.Director;
                properties = content.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                l = new List<String>();
                foreach (Property p in properties)
                {
                    l.Add(p.Value);
                }
                value = String.Join(",", l.ToArray());
                if (value.Length >= 129)
                {
                    return new ValidatorResult(false, "Datavalidation for CubiTV failed, Director is to long, maximum length is 128");
                }

                propertyName = VODnLiveContentProperties.Producer;
                properties = content.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                l = new List<String>();
                foreach (Property p in properties)
                {
                    l.Add(p.Value);
                }
                value = String.Join(",", l.ToArray());
                if (value.Length >= 129)
                {
                    return new ValidatorResult(false, "Datavalidation for CubiTV failed, Producer is to long, maximum length is 128");
                }

                propertyName = VODnLiveContentProperties.ScreenPlay;
                properties = content.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                l = new List<String>();
                foreach (Property p in properties)
                {
                    l.Add(p.Value);
                }
                value = String.Join(",", l.ToArray());
                if (value.Length >= 129)
                {
                    return new ValidatorResult(false, "Datavalidation for CubiTV failed, Screenplay is to long, maximum length is 128");
                }

                ICubiTVMWServiceWrapper wrapper = CubiTVMiddlewareManager.Instance(parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0].ObjectID.Value);
                propertyName = VODnLiveContentProperties.Category;
                properties = content.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                if (properties.Count() != 0)
                {
                //    log.Debug("Checking that all categories exists in CubiTV");
                //    try
                //    {
                //        if (!wrapper.CheckAllCategories(properties, serviceConfig))
                //        {
                //            return new ValidatorResult(false, "Datavalidation for CubiTV failed, one or more categories was missing");
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        log.Error("Something went wrong when validating categories", ex);
                //        return new ValidatorResult(false, "Something went wrong when validating categories - " + ex.Message);
                //    }
                //    log.Debug("All categories exists in CubiTV");
                }
                else
                {
                    return new ValidatorResult(false, "Datavalidation for CubiTV failed, Category is empty, needs at least one category on content");
                }
                propertyName = VODnLiveContentProperties.Genre;
                properties = content.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                if (properties.Count() != 0)
                {
                    Property metadataMappingProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals("MetadataMappingConfigurationFileName"));
                    if (metadataMappingProperty == null || String.IsNullOrEmpty(metadataMappingProperty.Value))
                    {
                        log.Error("Datamappigvalidation for CubiTV failed, No metadatamapping property was found!");

                    }

                    log.Debug("Checking that all genre exists in CubiTV");
                    try
                    {
                        List<Property> genresToCheck = new List<Property>();
                        foreach (Property p in properties)
                        {
                            String s = MetadataMappingHelper.GetGenreForService(metadataMappingProperty.Value, serviceObjectID, p.Value);
                            if (!String.IsNullOrEmpty(s))
                            {
                                Property newProp = new Property() { Type = "Genre", Value = s };
                                genresToCheck.Add(newProp);
                            }
                            else
                                throw new Exception("No data mapping for genre " + p.Value + " was found");
                        }
                        if (!wrapper.CheckAllGenres(genresToCheck))
                        {
                            return new ValidatorResult(false, "Datavalidation for CubiTV failed, one or more genres was missing");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Something went wrong when validating genres", ex);
                        return new ValidatorResult(false, "Something went wrong when validating genres - " + ex.Message);
                    }
                    log.Debug("All Genres exists in CubiTV");
                }



                if (content.LanguageInfos.Count > 0)
                {
                    String description = content.LanguageInfos[0].ShortDescription;
                    if (!String.IsNullOrEmpty(description) && description.Length >= 256)
                    {
                        return new ValidatorResult(false, "Datavalidation for CubiTV failed, Short description is to long, maximum length is 255");
                    }

                }



            }
            catch (Exception ex)
            {
                log.Error("Datavalidation for CubiTV failed, Something went wrong checking data on content!", ex);
                return new ValidatorResult(false, "Datavalidation for CubiTV failed, Something went wrong checking data on content! - " + ex.Message);
            }
            log.Info("CubiTV validation finished, all data seems ok");
            return new ValidatorResult(true);
        }

        private string GetTitle(ContentData contentData)
        {
            LanguageInfo language = contentData.LanguageInfos[0];
            return language.Title;
        }

        public ValidatorResult ArePropertiesMapped(RequestParameters parameters)
        {
            ContentData content = parameters.CurrentWorkFlowProcess.WorkFlowParameters.Content;
            Property metadataMappingProperty = content.Properties.SingleOrDefault<Property>(p => p.Type.Equals("MetadataMappingConfigurationFileName"));
            if (metadataMappingProperty == null || String.IsNullOrEmpty(metadataMappingProperty.Value))
            {
                log.Error("Datamappigvalidation for CubiTV failed, No metadatamapping property was found!");
                return new ValidatorResult(false, "Datamappigvalidation for CubiTV failed, No metadatamapping property was found!");
            }
            ulong serviceID = parameters.CurrentWorkFlowProcess.WorkFlowParameters.MultipleContentServices[0].ObjectID.Value;
            String propertyName = "";
         
            try
            {
                propertyName = "Category";
                //var ContentPropertyConfig = Config.GetConfig().CustomConfigs.Where(c => c.CustomConfigurationName == "ContentProperty").SingleOrDefault();

                propertyName = VODnLiveContentProperties.Category;
                var properties = content.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                if (properties.Count() != 0)
                {
                    log.Debug("Checking that all categories has mapped values");
                    
                    foreach (Property p in properties)
                    {
                        if (MetadataMappingHelper.GetCategoryForService(metadataMappingProperty.Value,serviceID , p.Value) == null)
                        {
                            log.Error("No category mapping was found for value " + p.Value);
                            return new ValidatorResult(false, "No category mapping was found for value " + p.Value);
                        }
                    }
                }


                propertyName = VODnLiveContentProperties.Genre;
                properties = content.Properties.Where<Property>(p => p.Type.ToLower().Equals(propertyName.ToLower()));
                if (properties.Count() != 0)
                {
                    log.Debug("Checking that all genres has mapped values");

                    foreach (Property p in properties)
                    {
                        if (MetadataMappingHelper.GetGenreForService(metadataMappingProperty.Value, serviceID, p.Value) == null)
                        {
                            log.Error("No genre mapping was found for value " + p.Value);
                            return new ValidatorResult(false, "No genre mapping was found for value " + p.Value);
                        }
                    }
                }

                var validatorResult = CheckIfRatingPropertiesAreMapped(content, metadataMappingProperty.Value, serviceID);
                if (!validatorResult.IsOK)
                    return validatorResult;

            }
            catch (Exception ex)
            {
                log.Error("DataMapping validation for CubiTV failed, Something went wrong checking data mapping on content!", ex);
                return new ValidatorResult(false, "DataMapping validation for CubiTV failed, Something went wrong checking data mapping on content! - " + ex.Message);
            }
            log.Info("Metadata mapping validation finished, all data mapping seems ok");
            return new ValidatorResult(true);
        }

        private ValidatorResult CheckIfRatingPropertiesAreMapped(ContentData content, string metadataMappingXmlFileName, ulong serviceObjectId)
        {
            var movieRating = CommonUtil.GetRatingForContent(content, VODnLiveContentProperties.MovieRating);
            var tvRating = CommonUtil.GetRatingForContent(content, VODnLiveContentProperties.TVRating);
            
            if (movieRating == null && tvRating == null)
                return new ValidatorResult(false, "Content " + content.Name + "(Id: " + content.ID + ") does not have any ratings.");

            if (movieRating != null)
            {
                var movieRatingValidatorResult = CheckIfValueIsMappedInMetadataMappingFile(movieRating, metadataMappingXmlFileName, serviceObjectId,
                                                         VODnLiveContentProperties.MovieRating);
                 if (!movieRatingValidatorResult.IsOK)
                     return movieRatingValidatorResult;
            }

            if (tvRating != null)
            {
                var tvRatingValidatorResult = CheckIfValueIsMappedInMetadataMappingFile(tvRating, metadataMappingXmlFileName, serviceObjectId,
                                                         VODnLiveContentProperties.TVRating);
                if (!tvRatingValidatorResult.IsOK)
                    return tvRatingValidatorResult;
            }
               
            return new ValidatorResult(true);
        }

        private ValidatorResult CheckIfValueIsMappedInMetadataMappingFile(String value, string metadataMappingXmlFileName, ulong serviceObjectId, string propertyType)
        {
            var validationFailedMessage = "";
            
            log.Debug("Checking that " + propertyType + " has mapped value");
            if (MetadataMappingHelper.GetMappedValueForService(metadataMappingXmlFileName, serviceObjectId, value, propertyType) == null)
            {
                validationFailedMessage = "No " + propertyType + " mapping was found for value " + value;
                log.Error(validationFailedMessage);
                return new ValidatorResult(false, validationFailedMessage);
            }
            return new ValidatorResult(true, "");

        }
    }
}
