using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Validation
{
    public interface IExternalXmlValidator
    {


        List<XmlError> ValidateXml(XmlDocument documentToValidate);

    }
}
