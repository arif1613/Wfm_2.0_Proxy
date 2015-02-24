using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.Cubi
{
    public class FakeCubiTVMWServiceWrapper : ICubiTVMWServiceWrapper
    {
        CubiTVTranslator translator = null;
        List<CubiEPG> cubiEPGForPurge = new List<CubiEPG>();
        
        public void Clean()
        {
            cubiEPGForPurge = new List<CubiEPG>();
        }

        public void AddcubiEPGForPurge(CubiEPG CubiEPG)
        {
            cubiEPGForPurge.Add(CubiEPG);
        }

        public FakeCubiTVMWServiceWrapper()
        {
            //String key = serviceConfig.GetConfigParam("UserHash");
            //String baseURL = serviceConfig.GetConfigParam("RestAPIBaseURL");

            //restAPI = new MiddleWareRestApiCaller(baseURL, key);
            translator = new CubiTVTranslator(this);
            //this.serviceConfig = serviceConfig;
        }

        public void CreateEpgImports(List<ConaxWorkflowManager.Core.Util.ValueObjects.ContentData> contents, double keepCatchupAliveInHour)
        {
            throw new NotImplementedException();
        }

        public void CreateCatchUpContent(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content, double keepCatchupAliveInHour)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.ContentData CreateContent(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData contentData, ulong serviceObjectID, ConaxWorkflowManager.Core.ServiceConfig seviceConfig, bool createCategoryIfNotExists)
        {
            throw new NotImplementedException();
        }

        public string GetCubiepgidByExtneralId(string externalId)
        {
            throw new NotImplementedException();
        }

        public List<CubiEPG> GetEPGsReadyForPurge(DateTime searchFrom, DateTime searchTo)
        {
            return cubiEPGForPurge;
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.NPVRRecording> GetRecordingsDeletedSince(DateTime dateFrom)
        {
            throw new NotImplementedException();
        }

        public System.Xml.XmlDocument GetCatchUpContent(string externalId)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.ContentData CreateLiveChannel(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public string CreateNPVRChannel(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public int CreateCover(ConaxWorkflowManager.Core.Util.ValueObjects.Image cover, System.IO.FileInfo file)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.ContentData GetContent(ulong contentID)
        {
            throw new NotImplementedException();
        }

        public System.Xml.XmlDocument GetCatchupChannelByCatchupEvent(System.Xml.XmlDocument sourceDoc)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.ContentData UpdateCatchUpContent(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData contentData, System.Xml.XmlDocument sourceDoc, double keepCatchupAliveInHour, int coverID)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.ContentData UpdateContent(ulong contentID, ConaxWorkflowManager.Core.Util.ValueObjects.ContentData contentData, ulong serviceObjectID, ConaxWorkflowManager.Core.ServiceConfig seviceConfig, bool createCategoryIfNotExists)
        {
            throw new NotImplementedException();
        }

        public bool DeleteContent(ulong contentID)
        {
            throw new NotImplementedException();
        }

        public bool DeleteEPG(ulong epgID)
        {
            throw new NotImplementedException();
        }

        public bool DeleteChannel(ulong channelID)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice GetContentPrice(ulong priceID)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice CreateContentPrice(ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice priceData, ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice UpdateContentPrice(ulong priceID, ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice priceData, ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public bool DeleteContentPrice(ulong priceID)
        {
            throw new NotImplementedException();
        }

        public System.Xml.XmlDocument GetSubscriptionPrice(ulong priceID)
        {
            throw new NotImplementedException();
        }

        public string GetProfileID(string deviceType)
        {
            throw new NotImplementedException();
        }

        public bool CheckAllCategories(IEnumerable<ConaxWorkflowManager.Core.Util.ValueObjects.Property> properties, ConaxWorkflowManager.Core.ServiceConfig seviceConfig)
        {
            throw new NotImplementedException();
        }

        public bool CheckAllGenres(IEnumerable<ConaxWorkflowManager.Core.Util.ValueObjects.Property> properties)
        {
            throw new NotImplementedException();
        }

        public System.Collections.Hashtable GetCategoryIDs(ConaxWorkflowManager.Core.ServiceConfig seviceConfig, List<string> listOfCategories, bool createCategoryIfNotExists)
        {
            throw new NotImplementedException();
        }

        public string GetServiceId(string serviceType)
        {
            throw new NotImplementedException();
        }

        public string GetMPAARatingID(string MPAARating)
        {
            throw new NotImplementedException();
        }

        public string GetMexRatingID(string mexRating)
        {
            throw new NotImplementedException();
        }

        public string GetVChipRatingID(string vChipRating)
        {
            throw new NotImplementedException();
        }

        public int GetCategoryID(ConaxWorkflowManager.Core.ServiceConfig seviceConfig, string categoryName, bool createCategoryIfNotExists)
        {
            throw new NotImplementedException();
        }

        public int GetCategoryID(ConaxWorkflowManager.Core.ServiceConfig seviceConfig, string categoryName, System.Collections.Hashtable allCategories, bool createCategoryIfNotExists)
        {
            throw new NotImplementedException();
        }

        public int GetNodeID(System.Collections.Hashtable table, string treeName, bool isTopNode, int belongsToNode)
        {
            throw new NotImplementedException();
        }

        public System.Collections.Hashtable GetGenreIDs(List<string> listOfGenres)
        {
            throw new NotImplementedException();
        }

        public string GetGenreID(string genre)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice CreateSubscriptionPrice(ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice priceData, ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice UpdateSubscriptionPrice(ulong priceID, ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice priceData)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice UpdateSubscriptionPrice(ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice priceData)
        {
            throw new NotImplementedException();
        }

        public ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice UpdateSubscriptionPrice(ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice priceData, ConaxWorkflowManager.Core.Util.ValueObjects.ContentData contentData, ConaxWorkflowManager.Core.Communication.UpdatePackageOfferType type)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSubscriptionPrice(ulong priceID)
        {
            throw new NotImplementedException();
        }

        public void AddContentToPackageOffer(ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice servicePrice, ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public void HandleContentPrice(ConaxWorkflowManager.Core.Util.ValueObjects.MultipleServicePrice servicePrice, ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content)
        {
            throw new NotImplementedException();
        }

        public List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.NPVRRecording> GetNPVRRecording(string externalId)
        {
            List<NPVRRecording> res = new List<NPVRRecording>();

            NPVRRecording rec = new NPVRRecording();
            rec.EPGExternalID = externalId;
            rec.Id = 1010;
            rec.EpgId = 2020;
            rec.Start = new DateTime(2013, 08, 18, 4, 0, 0);
            rec.End = new DateTime(2013, 08, 18, 20, 0, 0);
            rec.RecordState = NPVRRecordStateInCubiware.to_record;

            res.Add(rec);

            return res;
        }

        public List<NPVRRecording> GetNPVRRecording(List<String> externalIds) {
            return null;
        }

        public List<NPVRRecording> GetNPVRRecording(List<string> externalIds, int threadsToStart)
        {
            throw new NotImplementedException();
        }

        public void UpdateNPVRRecording(Dictionary<ContentData, List<NPVRRecording>> recordingsPerContent)
        {
        }

        public void UpdateNPVRRecording(ConaxWorkflowManager.Core.Util.ValueObjects.ContentData content, List<ConaxWorkflowManager.Core.Util.ValueObjects.Catchup.NPVRRecording> recordings, NPVRRecordStateInCubiware recordState)
        {
            Dictionary<String, List<NPVRRecording>> groupedRecordings = GenerateNPVRTask.GroupRecordingsPerGuardTimes(recordings);
            XmlDocument npvrXML = translator.TranslateContentDataToNPVRXml(content, groupedRecordings, recordState);
            
        }


        public Dictionary<string, string> GetAllProfiles()
        {
            throw new NotImplementedException();
        }

        #region ICubiTVMWServiceWrapper Members


        public bool DeleteCatchupEvent(string externalId)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ICubiTVMWServiceWrapper Members


        public System.Collections.Hashtable GetListOfCubiEpgIds(List<string> externalIds)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
