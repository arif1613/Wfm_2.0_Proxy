using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration
{
    class Mpp5IdentityModel
    {
        // MPP user credentials.
        public string UserName { get; set; }
        public string Password { get; set; }

        public string HolderId { get; set; }
        public string ClientID { get; set; }
        public string PrivateKey { get; set; }
    }
}
