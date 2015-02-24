using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using log4net;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.Archive
{
    public class SmoothThenHLSArchiveOrder : IArchiveOrder
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        BaseEncoderCatchupHandler SmoothHandler = null;
        BaseEncoderCatchupHandler HlsHandler = null;

        public SmoothThenHLSArchiveOrder(BaseEncoderCatchupHandler smoothHandler, BaseEncoderCatchupHandler hlsHandler)
        {
            SmoothHandler = smoothHandler;
            HlsHandler = hlsHandler;
        }

        public void ArchiveAssets(ContentData content, List<UInt64> serviceobjectId, DateTime startTime, DateTime endTime)
        {
            // archvie unique streams
            List<String> streamList = new List<String>();
            EPGChannel epgChannel = CatchupHelper.GetEPGChannel(content);
            List<System.Threading.Tasks.Task> archiveTPLTasks = new List<System.Threading.Tasks.Task>();
            foreach (KeyValuePair<UInt64, ServiceEPGConfig> kvp in epgChannel.ServiceEpgConfigs)
            {
                if (!serviceobjectId.Contains(kvp.Key)) // no user recorindg for this servcie, no need to arcvhie asset, continue to next.
                    continue;
                //if (!kvp.Value.EnableNPVR)
                //    continue;
                //if (!ConaxIntegrationHelper.IsPublishedToService(kvp.Value.ServiceObjectId, epgChannel))
                //    continue;
                foreach (SourceConfig sc in kvp.Value.SourceConfigs)
                {
                    if (streamList.Contains(sc.Stream))
                        continue; // archive altready started for this url
                    else
                        streamList.Add(sc.Stream);
                    
                    // check if asset already archvied or not.
                    //var asset = CommonUtil.GetAssetFromContentByISOAndDevice(content, kvp.Value.ServiceViewLanugageISO, sc.Device, AssetType.NPVR);
                    //var NPVRAssetArchiveStateCount = asset.Properties.Count(p => p.Type.Equals(CatchupContentProperties.NPVRAssetArchiveState) &&
                    //                                                             p.Value.Equals(NPVRAssetArchiveState.Archived.ToString("G")));
                    Property NPVRAssetArchiveStateProperty = ConaxIntegrationHelper.GetNPVRAssetArchiveState(content,kvp.Value.ServiceViewLanugageIso,sc.Device);
                    if (NPVRAssetArchiveStateProperty == null) {
                        log.Warn("content " + content.Name + " " + content.ID + " " + content.ExternalID + " missing asset for device " + sc.Device.ToString() + " for servcie " + kvp.Value.ServiceViewLanugageIso + ", this device might be recently added to the channel, skip archiving for this devcie.");
                        continue;
                    }

                    if (NPVRAssetArchiveStateProperty.Value.Equals(NPVRAssetArchiveState.Archived.ToString()))
                        continue; // already archived.
                    
                    String _streamName = sc.Stream;
                    UInt64 _serviceObjId = kvp.Key;
                    String _serviceISO = kvp.Value.ServiceViewLanugageIso;
                    DeviceType _device = sc.Device;
                    System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        ThreadContext.Properties["TaskName"] = "ArchiveAssetTask";
                        ThreadContext.Properties["ExternalId"] = "ExternalId=" + content.ExternalID + ";";
                        ThreadContext.Properties["ContentId"] = "Id=" + content.ID + ";";
                        AssetFormatType type = CommonUtil.GetAssetFormatTypeFromFileName(_streamName);
                        log.Debug("_streamName " + _streamName);
                        NPVRAssetLoger.WriteLog(content, "archive stream" + _streamName);
                        if (type == AssetFormatType.SmoothStreaming)
                            SmoothHandler.GenerateNPVR(content, _serviceObjId, _serviceISO, _device, startTime, endTime);
                        else if (type == AssetFormatType.HTTPLiveStreaming)
                            HlsHandler.GenerateNPVR(content, _serviceObjId, _serviceISO, _device, startTime, endTime);
                    }, TaskCreationOptions.LongRunning);
                    archiveTPLTasks.Add(task);
                }
            }

            while (archiveTPLTasks.Count > 0)
            {
                Int32 taskIndex = System.Threading.Tasks.Task.WaitAny(archiveTPLTasks.ToArray());
                try
                {
                    var res = archiveTPLTasks[taskIndex].Exception;
                    if (res != null)
                    {
                        res.Flatten();
                        log.Error("Failed to archive due to");
                        foreach (Exception ex in res.InnerExceptions)
                        {
                            log.Error(ex.Message, ex);
                        }
                    }
                }
                catch (AggregateException aex)
                {

                    aex = aex.Flatten();
                    log.Error("Failed to archive due to");
                    foreach (Exception ex in aex.InnerExceptions)
                    {
                        log.Error(ex.Message, ex);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Problem handle Task result  " + ex.Message, ex);
                }
                archiveTPLTasks.RemoveAt(taskIndex);
            }
        }
    }
}
