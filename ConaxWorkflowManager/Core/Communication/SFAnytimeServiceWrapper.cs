using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.SFAnytime;
using System.Collections;
using System.ServiceModel;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication
{
    public class SFAnytimeServiceWrapper
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Opws3ServiceClient client = new Opws3ServiceClient();

        private static string _operatorCode;
        private static string _language;
        private static string _country;

        public SFAnytimeServiceWrapper()
        {
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "SFAnytime").SingleOrDefault();
            String endpoint = systemConfig.GetConfigParam("Endpoint");
            if (client.Endpoint.Address.Uri.AbsoluteUri != endpoint)
            {
                log.Debug("Setting endpoint for SF Anytime API to " + endpoint + ", previous endpoint was " + client.Endpoint.Address.Uri);
                client.Endpoint.Address = new EndpointAddress(new Uri(endpoint), client.Endpoint.Address.Identity, client.Endpoint.Address.Headers);
            }

            _operatorCode = systemConfig.GetConfigParam("OperatorCode");
            _language = systemConfig.GetConfigParam("Language");
            _country = systemConfig.GetConfigParam("Country");
        }

        //public List<Media> GetMediaList()
        //{
        //    MediaList ml = client.getMediaList(_operatorCode, _language, _country);

        //    return ml.media.ToList();
        //}

        public IEnumerable<int> GetMediaIds()
        {
            MediaList ml = client.getMediaList(_operatorCode, _language, _country);

            return ml.media.Select(x => x.id);
        }

        public RootMediaDetails_1_3 GetMedia(int id)
        {
            RootMediaDetails_1_3 rmd = null;
            try
            {
                rmd = client.getMedia_1_3(_operatorCode, _country, _language, id);
                if (rmd.errorCode != 0)
                {
                    log.Error("Media id " + id + ": " + rmd.errorMessage);
                    rmd = null;
                }
            }
            catch (Exception e)
            {
                log.Error("Failed communicating with SFA API", e);
            }

            return rmd;
        }
    }
}
