using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using log4net;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.EPG
{
    public class NPVRHelper
    {

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private XElement rightsManagementConfig;
        private static Hashtable rightOwnerPerChannel = new Hashtable();
        private static Hashtable rightOwnerTable = new Hashtable();
        private static Hashtable channelTable = new Hashtable();
        
        public NPVRHelper()
        {
            try
            {
                var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
                if (!systemConfig.ConfigParams.ContainsKey("NPVRRightsManagementFile") || String.IsNullOrEmpty(systemConfig.GetConfigParam("NPVRRightsManagementFile")))
                {
                    log.Error("No NPVRRightsManagementFile was configured, either value was empty or it needs to be added to the ConaxWorkflowManager system settings as NPVRRightsManagementFile");
                    throw new Exception("No NPVRRightsManagementFile was configured, either value was empty or it needs to be added to the ConaxWorkflowManager system settings as NPVRRightsManagementFile");
                }
                rightsManagementConfig = XElement.Load(systemConfig.GetConfigParam("NPVRRightsManagementFile"));
            }
            catch (Exception exc)
            {
                log.Error("Something went wrong initializing NPVRHelper", exc);
                throw;
            }
        }

        public bool NPVRIsEnabledForEvent(EPGChannel epgChannel, XElement eventNode, XElement EPGChannelConfigXML, String channelName)
        {
            bool ret = false;
            bool fetchedReply = false;
            String channelId = eventNode.Attribute("channel").Value;
            XElement channelNode = rightsManagementConfig.XPathSelectElement("Channels/Channel[@epgChannelId='" + channelId + "']");            

            if (eventNode.Attribute("enableNPVR") != null && !String.IsNullOrEmpty(eventNode.Attribute("enableNPVR").Value))
            {
                fetchedReply = bool.TryParse(eventNode.Attribute("enableNPVR").Value, out ret);
            }
            if (fetchedReply)
                return ret;

            if (eventNode.Attribute("rightsOwner") != null && !String.IsNullOrEmpty(eventNode.Attribute("rightsOwner").Value))
            {
                String rightsOwner = eventNode.Attribute("rightsOwner").Value;
                
                if (channelNode != null)
                {
                    String key = channelId+ ":" + rightsOwner;
                    if (rightOwnerPerChannel.ContainsKey(key))
                    {
                        ret = (bool)rightOwnerPerChannel[key];
                    }
                    else
                    {
                        XElement rightsOwnerElement = channelNode.XPathSelectElement("RightsOwners/RightsOwner[@name='" + rightsOwner + "']");
                        if (rightsOwnerElement != null && rightsOwnerElement.Attribute("enableNPVR") != null)
                        {
                            String enableNPVRFlag = rightsOwnerElement.Attribute("enableNPVR").Value;
                            if (bool.TryParse(enableNPVRFlag, out ret))
                            {
                                rightOwnerPerChannel.Add(key, ret);
                                fetchedReply = true;
                            }
                            else
                            {
                                log.Warn("Error parsing enableNPVRFlag for rightsOwner " + rightsOwner + " on channel with id + " + channelId);
                            }
                        }
                    }
                }
                else
                {
                    log.Warn("No channel with epgChannelId " + channelId + " was found in configuration");
                }
                if (!fetchedReply)
                {
                    if (rightOwnerTable.ContainsKey(rightsOwner))
                    {
                        ret = (bool)rightOwnerTable[rightsOwner];
                        fetchedReply = true;
                    }
                    else
                    {
                        XElement rightsOwnerNode = rightsManagementConfig.XPathSelectElement("rightsOwners/rightsOwner[@name='" + rightsOwner + "']");
                        if (rightsOwnerNode != null && rightsOwnerNode.Attribute("enableNPVR") != null)
                        {
                            String enableNPVRFlag = rightsOwnerNode.Attribute("enableNPVR").Value;
                            if (bool.TryParse(enableNPVRFlag, out ret))
                            {
                                rightOwnerTable.Add(rightsOwner, ret);
                                fetchedReply = true;
                            }
                            else
                            {
                                log.Warn("Error parsing enableNPVRFlag for rightsOwner " + rightsOwner + " on channel with id + " + channelId);
                            }
                        }
                    }
                }
            }
            if (fetchedReply)
                return ret;


            if (channelNode != null && channelNode.Attribute("enableNPVR") != null)
            {
                if (channelTable.ContainsKey(channelId))
                {
                    ret = (bool)channelTable[channelId];
                    fetchedReply = true;
                }
                else
                {
                    String enableNPVRFlag = channelNode.Attribute("enableNPVR").Value;
                    if (bool.TryParse(enableNPVRFlag, out ret))
                    {
                        channelTable.Add(channelId, ret);
                        fetchedReply = true;
                    }
                    else
                    {
                        log.Warn("Error parsing enableNPVRFlag for channel with epgChannelId + " + channelId);
                    }
                }
            }
            if (fetchedReply)
                return ret;
           
            //foreach (XElement channel in EPGChannelConfigXML.XPathSelectElements("Channels/Channel"))
            //{
            //    if (channel.Attribute("epgChannelId").Value.Equals(channelName, StringComparison.OrdinalIgnoreCase))
            //    {
            //        XAttribute enableNPVRAttribute = channel.Attribute("enableNPVR");
            //        if (enableNPVRAttribute != null)
            //            ret = Boolean.Parse(enableNPVRAttribute.Value);
            //        break;
            //    }
            //}
            ret = epgChannel.EnableNPVRInAnyService;

            return ret;
        }

        public static void SetSynked(ContentData content, bool isSynked)
        {
            Property property = content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.EpgIsSynked));
            if (property == null)
            {
                property = new Property(CatchupContentProperties.EpgIsSynked, isSynked.ToString());
                content.Properties.Add(property);
            }
            else
            {
                property.Value = isSynked.ToString();
            }
        }

        public static bool IsSynked(ContentData content)
        {
            Property property = content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.EpgIsSynked));
            if (property == null)
            {
                return false;
            }
            else
            {
                return bool.Parse(property.Value);
            }
        }

        public static int IncreaseEpgSynkRetries(ContentData content)
        {
            //MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
            Property property =
                content.Properties.SingleOrDefault(p => p.Type.Equals(CatchupContentProperties.NoOfEpgSynkRetries));
            int retries = 1;

            String value = property.Value;
            try
            {
                retries = int.Parse(value);
                retries++;
            }
            catch (Exception exc)
            {
                log.Warn("Couldnt parse noOfTries, value= " + value);
            }
            property.Value = retries.ToString();
            // mppWrapper.UpdateContentProperty(content.ID.Value, property);

            return retries;
        }
    }
}
