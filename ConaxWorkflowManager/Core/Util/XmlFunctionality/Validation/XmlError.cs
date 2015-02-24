using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Validation
{
    public class XmlError
    {
        public XmlError(String propertyName, XmlValidationErrors errorType, String errorMessage)
        {
            PropertyName = propertyName;

            ErrorType = errorType;

            ErrorMsg = errorMessage;
        }

        public XmlError()
        {
            
        }

        public String PropertyName { get; set; }

        public XmlValidationErrors ErrorType { get; set; }

        public String ErrorMsg { get; set; }
    }
}
