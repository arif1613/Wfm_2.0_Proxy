using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects
{
    public class Property
    {
        public Property()
        {}

        public Property(String type, String value) {
            Type = type;
            Value = value;
        }
        public String Type { get; set; }
        public String Value { get; set; }
    }
}
