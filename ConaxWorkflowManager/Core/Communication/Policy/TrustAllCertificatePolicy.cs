using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Policy
{
    public class TrustAllCertificatePolicy : ICertificatePolicy
    {
        #region ICertificatePolicy Members

        public bool CheckValidationResult(ServicePoint srvPoint, System.Security.Cryptography.X509Certificates.X509Certificate certificate, WebRequest request, int certificateProblem)
        {
            return true;
        }

        #endregion
    }
}
