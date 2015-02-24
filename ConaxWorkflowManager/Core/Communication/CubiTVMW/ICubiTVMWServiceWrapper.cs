using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.CubiTVMW
{
    public interface ICubiTVMWServiceWrapper
    {


        void CreateEpgImports(List<ContentData> contents, Double keepCatchupAliveInHour);

        void CreateCatchUpContent(ContentData content, Double keepCatchupAliveInHour);

        /// <summary>
        /// This method registers a VOD Content in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="contentData">The object containing the information about the content to add.</param>
        /// <returns>Returns true if the content was added successfully.</returns>
        ContentData CreateContent(ContentData contentData, ulong serviceObjectID, ServiceConfig seviceConfig,
                                  bool createCategoryIfNotExists);


        String GetCubiepgidByExtneralId(String externalId);


        List<CubiEPG> GetEPGsReadyForPurge(DateTime searchFrom, DateTime searchTo);


        List<NPVRRecording> GetRecordingsDeletedSince(DateTime dateFrom);

        XmlDocument GetCatchUpContent(String externalId);

        ContentData CreateLiveChannel(ContentData content);

        String CreateNPVRChannel(ContentData content);


        /// <summary>
        /// This method registers a cover in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="cover">The image to create an cover from</param>
        /// <param name="file">The fileInfo of the image</param>
        /// <returns>Returns the ID in CubiTV if the cover was created successfully.</returns>
        int CreateCover(Image cover, FileInfo file);


        /// <summary>
        /// This method fetches a VOD Content in CubiTV Middleware Server.
        /// </summary>
        /// <param name="contentID">The ID of the content to fetch.</param>
        /// <returns>Returns information about the content.</returns>
        ContentData GetContent(ulong contentID);


        XmlDocument GetCatchupChannelByCatchupEvent(XmlDocument sourceDoc);


        ContentData UpdateCatchUpContent(ContentData contentData, XmlDocument sourceDoc, Double keepCatchupAliveInHour,
                                         Int32 coverID);


        /// <summary>
        /// This method updates a VOD Content in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="contentID">The id of the content to update.</param>
        /// <param name="contentData">The object containing the information about the content to update.</param>
        /// <returns>Returns true if the content was updated successfully.</returns>
        ContentData UpdateContent(ulong contentID, ContentData contentData, ulong serviceObjectID,
                                  ServiceConfig seviceConfig, bool createCategoryIfNotExists);


        /// <summary>
        /// This method deletes a VOD Content in CubiTV Middleware Server.
        /// </summary>
        /// <param name="contentID">The ID of the content to delete.</param>
        /// <returns>Returns true if the content was deleted successfully.</returns>
        bool DeleteContent(ulong contentID);


        bool DeleteEPG(ulong epgID);

        /// <summary>
        /// Since there is no way to delete catchupevents from externalId and we cant be sure an catchupevent have been
        /// synked with Mpp this method first tries to fetch the catchupevent from externalId and then fetches the Id
        /// and deletes with that id.
        /// </summary>
        /// <param name="externalId">The externalId to delete for</param>
        /// <returns>True if successfull</returns>
        bool DeleteCatchupEvent(String externalId);
        /// <summary>
        /// This method deletes a channel in CubiTV Middleware Server.
        /// </summary>
        /// <param name="channelID">The ID of the channel to delete.</param>
        /// <returns>Returns true if the channel was deleted successfully.</returns>
        bool DeleteChannel(ulong channelID);


        /// <summary>
        /// This method fetches a content Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The ID of the content price to fetch data about.</param>
        /// <returns>Information of the price if the call was successful.</returns>
        MultipleServicePrice GetContentPrice(ulong priceID);


        /// <summary>
        /// This method registers a content Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceData">The object containing the information about the price to add.</param>
        /// <param name="externalContentIDs">A list of one or more external ID to be added to the price</param>
        /// <returns>Returns true if the content was added successfully.</returns>
        MultipleServicePrice CreateContentPrice(MultipleServicePrice priceData, ContentData content);


        /// <summary>
        /// This method updates a Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The CubiTV priceID of the offer to update</param>
        /// <param name="priceData">The object containing the information to update the price with.</param>
        /// <returns>MultipleServicePrice containing data about the updated content price.</returns>
        MultipleServicePrice UpdateContentPrice(ulong priceID, MultipleServicePrice priceData, ContentData content);



        /// <summary>
        /// This method deletes a Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The ID of the content price to delete.</param>
        /// <returns>True if the content price was deleted successful.</returns>
        Boolean DeleteContentPrice(ulong priceID);


        /// <summary>
        /// This method fetches a subscription price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The ID of the subscription price to fetch data about.</param>
        /// <returns>Information of the price if the call was successful.</returns>
        XmlDocument GetSubscriptionPrice(ulong priceID);

        String GetProfileID(String deviceType);


        /// <summary>
        /// This method checks that all categories are present in cubiTV
        /// </summary>
        /// <param name="properties">All properties containing the categories to check</param>
        /// <returns>True if all catergories are found</returns>
        bool CheckAllCategories(IEnumerable<Property> properties, ServiceConfig seviceConfig);


        /// <summary>
        /// This method checks that all genres are present in cubiTV
        /// </summary>
        /// <param name="properties">All properties containing the genres to check</param>
        /// <returns>True if all genres are found</returns>
        bool CheckAllGenres(IEnumerable<Property> properties);


        Hashtable GetCategoryIDs(ServiceConfig seviceConfig, List<String> listOfCategories,
                                 bool createCategoryIfNotExists);




        string GetServiceId(string serviceType);


        String GetMPAARatingID(String MPAARating);


        String GetMexRatingID(String mexRating);


        String GetVChipRatingID(String vChipRating);


        int GetCategoryID(ServiceConfig seviceConfig, String categoryName, bool createCategoryIfNotExists);


        int GetCategoryID(ServiceConfig seviceConfig, String categoryName, Hashtable allCategories,
                          bool createCategoryIfNotExists);



        int GetNodeID(Hashtable table, String treeName, bool isTopNode, int belongsToNode);

        Dictionary<String, String> GetAllProfiles();
       

        Hashtable GetGenreIDs(List<String> listOfGenres);


        String GetGenreID(String genre);



        /// <summary>
        /// This method registers a subscription Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceData">The object containing the information about the price to add.</param>
        /// <param name="externalContentIDs">A list of one or more external ID to be added to the price</param>
        /// <returns>Returns true if the content was added successfully.</returns>
        MultipleServicePrice CreateSubscriptionPrice(MultipleServicePrice priceData, ContentData content);


        /// <summary>
        /// This method updates a Subscription Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The id of the Subscription Price to update</param>
        /// <param name="priceData">The object containing the information to update the price with.</param>
        /// <returns>MultipleServicePrice containing data about the updated Subsription Price.</returns>
        MultipleServicePrice UpdateSubscriptionPrice(ulong priceID, MultipleServicePrice priceData);


        MultipleServicePrice UpdateSubscriptionPrice(MultipleServicePrice priceData);


        MultipleServicePrice UpdateSubscriptionPrice(MultipleServicePrice priceData, ContentData contentData,
                                                     UpdatePackageOfferType type);


        /// <summary>
        /// This method deletes a Subscription Price in the CubiTV Middleware Server.
        /// </summary>
        /// <param name="priceID">The ID of the content price to delete.</param>
        /// <returns>True if the Subscription Price was deleted successful.</returns>
        Boolean DeleteSubscriptionPrice(ulong priceID);


        void AddContentToPackageOffer(MultipleServicePrice servicePrice, ContentData content);


        void HandleContentPrice(MultipleServicePrice servicePrice, ContentData content);


        List<NPVRRecording> GetNPVRRecording(String externalId);

        List<NPVRRecording> GetNPVRRecording(List<String> externalIds);

        List<NPVRRecording> GetNPVRRecording(List<String> externalIds, Int32 threadsToStart);


        void UpdateNPVRRecording(ContentData content, List<NPVRRecording> recordings, NPVRRecordStateInCubiware recordState);


        Hashtable GetListOfCubiEpgIds(List<string> externalIds);

        void UpdateNPVRRecording(Dictionary<ContentData, List<NPVRRecording>> recordingsPerContent);
   
    }
}
