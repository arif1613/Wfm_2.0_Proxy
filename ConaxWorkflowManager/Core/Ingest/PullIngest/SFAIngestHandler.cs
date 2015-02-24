using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.SFAnytime;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest.SFA;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using log4net;
using System.Reflection;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.PullIngest
{
    public class SFAIngestHandler : IPullIngestHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private SFAnytimeServiceWrapper sfaWrapper = new SFAnytimeServiceWrapper();
        private SFAFtpHelper ftp = new SFAFtpHelper();
        public SFAFileHelper sfaFileHelper = new SFAFileHelper();

        public virtual IEnumerable<int> GetAvailableExternalIds()
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SFAnytime").SingleOrDefault();
            List<int> toreturn = new List<int>();
            try
            {
                foreach (var item in systemConfig.GetConfigParam("TestMedia").Split(','))
                {
                    toreturn.Add(int.Parse(item));
                }

                log.Debug("Using test data for media ids");
                return toreturn;
            }
            catch (Exception)
            {
                // no test data provided, use normal method
            }

            return sfaWrapper.GetMediaIds();
            
        }

        public string GetSystemName()
        {
            return "SFAnytime";
        }


        public bool IsValidExternalId(string MPPExternalIdString)
        {
            // valid: "SFA-1234"
            int i;
            if (MPPExternalIdString != null && MPPExternalIdString.StartsWith("SFA-") && int.TryParse(MPPExternalIdString.Substring(4), out i))
            {
                return true;
            }
            return false;
        }

        public int GetExternalIdFromMPPExternalId(string MPPExternalid)
        {
            return int.Parse(MPPExternalid.Substring(4));
        }


        public string CreateMPPExternalIdString(int externalId)
        {
            return "SFA-" + externalId.ToString();
        }


        public virtual void ProcessFiles(int externalId)
        {
            bool success = false;
            RootMediaDetails_1_3 rmd = sfaWrapper.GetMedia(externalId);

            if (rmd != null)
            {
                SFAMedia mediaObject = SFAMediaTranslator.Translate(rmd);
                if (mediaObject != null)
                {
                    if (sfaFileHelper.WriteXmlInDownloadDir(mediaObject.CableLabsXml, mediaObject.VideoFileName))
                    {
                        //if (ftp.TEST__DownloadFilesForMedia(mediaObject, sfMediaId))
                        if (ftp.DownloadFilesForMedia(mediaObject))
                        {
                            if (sfaFileHelper.GetCoverImage(mediaObject, externalId))
                            {
                                if (sfaFileHelper.MoveFilesToDecrypt(externalId))
                                {
                                    log.Debug("Got all files for media id " + externalId);
                                    success = true;
                                }
                            }
                        }
                    }
                }
            }

            if (!success)
            {
                log.Error("Failed getting files for media id " + externalId);
                sfaFileHelper.ClearDownloadFolder();
            }

        }


        public virtual IEnumerable<int> GetIdsToProcess(IEnumerable<int> externalIds, IEnumerable<ContentData> content)
        {
            return externalIds.Except(content.Select(x => GetExternalIdFromMPPExternalId(x.ExternalID)));
        }


        public virtual IEnumerable<int> GetIdsToDelete(IEnumerable<int> externalIds, IEnumerable<ContentData> content)
        {
            return content.Select(x => GetExternalIdFromMPPExternalId(x.ExternalID)).Except(externalIds);
        }
    }
}
