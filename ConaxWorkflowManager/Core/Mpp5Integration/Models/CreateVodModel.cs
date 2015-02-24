using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models
{
    public class CreateVodModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, Dictionary<CultureInfo, string>> MetaData { get; set; }
        public string WamsAccountId { get; set; }
        public bool ActiveDirectoryRestricted { get; set; }
        public string Status { get; set; }
    }
}
