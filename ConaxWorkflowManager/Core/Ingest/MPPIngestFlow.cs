using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest
{
    public class MPPIngestFlow
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Process(IngestItem ingestItem)
        {
            MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

            log.Debug("Action type:" + ingestItem.Type.ToString("G"));
            if (ingestItem.Type == IngestType.AddContent) 
            {
                List<MultipleServicePrice> newPrices = new List<MultipleServicePrice>(); // track new prices
                try {
                    foreach (KeyValuePair<MultipleContentService, List<MultipleServicePrice>> kvp in ingestItem.MultipleServicePrices)
                    {
                        foreach (MultipleServicePrice servicePrice in kvp.Value)
                        {
                            if (!servicePrice.ID.HasValue)
                            {  // no ID, create new servcie price. (content price)
                                mppWrapper.CreateServicePrice(kvp.Key.ObjectID.Value, servicePrice);
                                newPrices.Add(servicePrice);                        
                            }
                        }
                    }
                    mppWrapper = MPPIntegrationServiceManager.InstanceWithActiveEvent;
                    // create MPP Content 
                    log.Info("Adding content to mpp");
                    mppWrapper.AddContent(ingestItem.contentData);

                    mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
                    // set service price.
                    foreach (KeyValuePair<MultipleContentService, List<MultipleServicePrice>> kvp in ingestItem.MultipleServicePrices)
                    {
                        foreach (MultipleServicePrice servicePrice in kvp.Value)
                        {
                            mppWrapper.SetContentServicePrice(servicePrice, ingestItem.contentData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // delete new created stuff
                    foreach (MultipleServicePrice MSP in newPrices)
                        mppWrapper.DeleteServicePrice(MSP);

                    if (ingestItem.contentData.ID.HasValue)
                        mppWrapper.DeleteContent(ingestItem.contentData);

                    throw;
                }
            }
            if (ingestItem.Type == IngestType.UpdateContent)
            {
                throw new NotImplementedException("Updaet content not implemented");
            }
            if (ingestItem.Type == IngestType.DeleteContent)
            {                
                throw new NotImplementedException("Delete content not implemented");
            }
        }
    }
}
