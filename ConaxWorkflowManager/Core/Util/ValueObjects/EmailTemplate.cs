using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class EmailTemplate
    {
        public EmailTemplateType Type { get; set; }
        public String From { get; set; }
        public String Subject { get; set; }
        public String Body { get; set; }
    }
}
